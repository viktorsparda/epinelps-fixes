#!/usr/bin/env python3
"""Server-contract regression suite for the local EpinelPS build.

Runs a server binary in a throwaway sandbox on unprivileged ports, against a
*copy* of the live save data, and replays exact client protobuf requests for
the flows fixed so far. The live server, its db.json and ports 80/443 are never
touched, so this is safe to run while the real server is up.

    ./scripts/regression.py                      # test the installed binary
    ./scripts/regression.py --binary dist/server-x/EpinelPS   # test a new build
    ./scripts/regression.py -k dice -k island    # only matching cases
    ./scripts/regression.py --list               # list cases
    ./scripts/regression.py --keep               # keep sandbox + log after run

Exit code 0 = all passed. The server log for the run is in
.regression/sandbox/server.log (kept on failure).

Needs an existing install (EPINELPS_DATA, default ~/.local/share/EpinelPS) with
static data cached, and an admin account in its save: EPINELPS_ADMIN_EMAIL /
EPINELPS_ADMIN_PASSWORD (default me@local / password). The server must
support EPINELPS_HTTP_PORT / EPINELPS_HTTPS_PORT (this fork does).

Cases live in scripts/regression_cases.py. Each case gets a fresh account
(registered through the admin API inside the sandbox) plus the admin account,
so assertions can use exact values instead of depending on the live save.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import socket
import ssl
import subprocess
import sys
import time
import traceback
import urllib.error
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
WORKSPACE = os.path.dirname(HERE)
sys.path.insert(0, HERE)

from protoschema import Schema  # noqa: E402

DATA_DIR = os.environ.get("EPINELPS_DATA") or os.path.join(
    os.environ.get("XDG_DATA_HOME") or os.path.expanduser("~/.local/share"), "EpinelPS")
LIVE_DIR = os.path.join(DATA_DIR, "server")
# Server source with Protos/Generated: the workspace checkout (.src/EpinelPS)
# or, inside the epinelps-fixes repo, its EpinelPS/ project directory.
SRC_DIR = next((p for p in (os.path.join(WORKSPACE, ".src", "EpinelPS"),
                            os.path.join(WORKSPACE, "EpinelPS"))
                if os.path.isdir(os.path.join(p, "Protos"))),
               os.path.join(WORKSPACE, "EpinelPS"))
STATE_DIR = os.path.join(WORKSPACE, ".regression")
SANDBOX = os.path.join(STATE_DIR, "sandbox")

# An admin account that exists in the copied save (needed to register the
# throwaway test accounts). Override for other installs.
ADMIN_EMAIL = os.environ.get("EPINELPS_ADMIN_EMAIL", "me@local")
ADMIN_PASSWORD = os.environ.get("EPINELPS_ADMIN_PASSWORD", "password")

# Copied into the sandbox (written by the server or small); everything else
# in the live dir is symlinked read-mostly. db.json / epinelps.db MUST be copies
# because the server resolves them next to its own executable.
COPY_FILES = [
    "db.json", "epinelps.db", "gamecommon.json", "gameconfig.json",
    "gameversion.json", "appsettings.json", "appsettings.Development.json",
    "log4net.config", "site.pfx", "EpinelPS.staticwebassets.endpoints.json",
]
LINK_DIRS = ["cache", "wwwroot"]


class CaseFailure(AssertionError):
    pass


def _ssl_ctx():
    ctx = ssl.create_default_context()
    ctx.check_hostname = False
    ctx.verify_mode = ssl.CERT_NONE
    return ctx


def _free(port: int) -> bool:
    with socket.socket() as s:
        s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)  # ignore TIME_WAIT
        try:
            s.bind(("127.0.0.1", port))
            return True
        except OSError:
            return False


class Server:
    """A sandboxed server process."""

    def __init__(self, binary: str, http_port: int, https_port: int):
        self.binary = os.path.abspath(binary)
        self.http_port = http_port
        self.https_port = https_port
        self.base = f"https://127.0.0.1:{https_port}"
        self.proc: subprocess.Popen | None = None
        self.log_path = os.path.join(SANDBOX, "server.log")

    def prepare(self):
        if os.path.isdir(SANDBOX):
            shutil.rmtree(SANDBOX)
        os.makedirs(os.path.join(SANDBOX, "logs"))
        dst = os.path.join(SANDBOX, "EpinelPS")
        # Must be a real copy: the server writes db.json next to the resolved
        # executable path, and a symlink would resolve to the live directory.
        subprocess.run(["cp", "--reflink=auto", self.binary, dst], check=True)
        os.chmod(dst, 0o755)
        for name in COPY_FILES:
            src = os.path.join(LIVE_DIR, name)
            if os.path.exists(src):
                shutil.copy2(src, os.path.join(SANDBOX, name))
        for name in LINK_DIRS:
            os.symlink(os.path.join(LIVE_DIR, name), os.path.join(SANDBOX, name))
        assert os.path.realpath(dst).startswith(os.path.realpath(SANDBOX))

    def start(self, timeout: float = 120):
        for p in (self.http_port, self.https_port):
            if not _free(p):
                raise SystemExit(f"port {p} is in use; pick others with --https-port/--http-port")
        env = dict(os.environ,
                   EPINELPS_HTTP_PORT=str(self.http_port),
                   EPINELPS_HTTPS_PORT=str(self.https_port))
        log = open(self.log_path, "wb")
        # stdin stays an open pipe: on EOF the server's console loop spins.
        self.proc = subprocess.Popen(
            [os.path.join(SANDBOX, "EpinelPS"), "--headless"],
            cwd=SANDBOX, env=env, stdin=subprocess.PIPE,
            stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
        deadline = time.time() + timeout
        while time.time() < deadline:
            if self.proc.poll() is not None:
                raise SystemExit(f"sandbox server exited ({self.proc.returncode}); see {self.log_path}")
            try:
                urllib.request.urlopen(self.base + "/", context=_ssl_ctx(), timeout=2)
                return
            except urllib.error.HTTPError:
                return  # any HTTP answer means Kestrel is up
            except Exception:
                time.sleep(0.5)
        raise SystemExit(f"sandbox server did not answer within {timeout}s; see {self.log_path}")

    def stop(self):
        if not self.proc or self.proc.poll() is not None:
            return
        self.proc.terminate()
        try:
            self.proc.wait(10)
        except subprocess.TimeoutExpired:
            self.proc.kill()
            self.proc.wait()


class Http:
    def __init__(self, base: str):
        self.base = base

    def json(self, path: str, body: dict, token: str | None = None) -> dict:
        req = urllib.request.Request(self.base + path, data=json.dumps(body).encode(), method="POST")
        req.add_header("Content-Type", "application/json")
        if token:
            req.add_header("Authorization", "Bearer " + token)
        with urllib.request.urlopen(req, context=_ssl_ctx(), timeout=30) as r:
            return json.loads(r.read() or b"{}")


class Admin:
    def __init__(self, http: Http):
        self.http = http
        r = http.json("/adminapi/login", {"Username": ADMIN_EMAIL, "Password": ADMIN_PASSWORD})
        if not r.get("token") and not r.get("Token"):
            raise SystemExit(f"admin login failed in sandbox: {r}")
        self.token = r.get("token") or r.get("Token")

    def cmd(self, name: str, uid: int | str = "", *args) -> dict:
        r = self.http.json("/adminapi/RunCmd",
                           {"cmdName": name, "p1": str(uid), "p2": " ".join(map(str, args))},
                           self.token)
        if not r.get("ok"):
            raise CaseFailure(f"admin cmd {name} {uid} {args}: {r['error']}")
        return r

    def register(self, email: str, password: str):
        r = self.http.json("/adminapi/RegisterAccount?seq=1",
                           {"Email": email, "Password": password}, self.token)
        if not r.get("ok"):
            raise SystemExit(f"RegisterAccount failed: {r}")


class Client:
    """A logged-in game account talking protobuf to the lobby."""

    def __init__(self, http: Http, schema: Schema, email: str, password: str):
        self.http = http
        self.schema = schema
        self.email = email
        md5 = hashlib.md5(password.encode()).hexdigest()
        r = http.json("/account/login?seq=1", {"account": email, "password": md5})
        if r.get("ret") != 0:
            raise SystemExit(f"login failed for {email}: {r}")
        self.token = r["token"]
        self.uid = int(r["uid"])

    def raw(self, path: str, body: bytes) -> tuple[int, bytes]:
        url = self.http.base + "/v1/" + path.lstrip("/")
        req = urllib.request.Request(url, data=body, method="POST")
        req.add_header("Authorization", "Bearer " + self.token)
        req.add_header("Content-Type", "application/octet-stream+protobuf")
        try:
            with urllib.request.urlopen(req, context=_ssl_ctx(), timeout=30) as r:
                return r.status, r.read()
        except urllib.error.HTTPError as e:
            return e.code, e.read()

    def call(self, path: str, req: dict | None = None, *, res: str | None = None,
             req_type: str | None = None) -> dict:
        """POST a named-field request; return the decoded response dict.
        Types come from the handler source unless overridden."""
        route = self.schema.routes.get(path) or {}
        req_type = req_type or route.get("req")
        res_type = res or route.get("res")
        if req and not req_type:
            raise CaseFailure(f"no request type known for {path}")
        body = self.schema.encode(req_type, req) if req else b""
        status, raw = self.raw(path, body)
        if status != 200:
            raise CaseFailure(f"{path}: HTTP {status} {raw[:200]!r}")
        if not res_type:
            return {"_raw": raw.hex()}
        try:
            out = self.schema.decode(res_type, raw)
        except Exception as e:
            raise CaseFailure(f"{path}: cannot decode as {res_type}: {e}; raw={raw.hex()[:200]}")
        out["_raw"] = raw.hex()
        return out


class Context:
    def __init__(self, server: Server, schema: Schema, run_id: str):
        self.http = Http(server.base)
        self.schema = schema
        self.admin = Admin(self.http)
        self._run_id = run_id
        self._n = 0
        self._admin_client: Client | None = None

    def fresh(self, tutorials: bool = True) -> Client:
        """Register and log in a brand-new account inside the sandbox."""
        self._n += 1
        email = f"regress-{self._run_id}-{self._n}@local"
        self.admin.register(email, "regress")
        c = Client(self.http, self.schema, email, "regress")
        if tutorials:
            self.admin.cmd("finish-all-tutorials", c.uid)
        return c

    def admin_client(self) -> Client:
        if not self._admin_client:
            self._admin_client = Client(self.http, self.schema, ADMIN_EMAIL, ADMIN_PASSWORD)
        return self._admin_client


def check(cond, msg: str):
    if not cond:
        raise CaseFailure(msg)


def eq(actual, expected, what: str):
    if actual != expected:
        raise CaseFailure(f"{what}: expected {expected!r}, got {actual!r}")


def main() -> int:
    import regression_cases

    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--binary", default=os.path.join(LIVE_DIR, "EpinelPS"),
                    help="server binary to test (default: installed live binary)")
    ap.add_argument("--src", default=SRC_DIR, help="source tree for the proto schema")
    ap.add_argument("--https-port", type=int, default=8443)
    ap.add_argument("--http-port", type=int, default=8880)
    ap.add_argument("-k", action="append", default=[], help="run cases whose name contains this")
    ap.add_argument("--list", action="store_true")
    ap.add_argument("--keep", action="store_true", help="keep the sandbox after a passing run")
    ap.add_argument("-v", "--verbose", action="store_true", help="print tracebacks")
    args = ap.parse_args()

    cases = [c for c in regression_cases.CASES
             if not args.k or any(k in c.__name__ for k in args.k)]
    if args.list:
        for c in regression_cases.CASES:
            print(f"{c.__name__:40} {(c.__doc__ or '').strip().splitlines()[0] if c.__doc__ else ''}")
        return 0
    if not cases:
        print("no cases matched")
        return 2

    schema = Schema.load(args.src, STATE_DIR)
    sha = hashlib.sha256(open(args.binary, "rb").read()).hexdigest()
    print(f"binary {args.binary}\nsha256 {sha[:16]}…  sandbox {SANDBOX}  port {args.https_port}")

    server = Server(args.binary, args.http_port, args.https_port)
    server.prepare()
    t0 = time.time()
    failures = 0
    try:
        server.start()
        print(f"server up in {time.time() - t0:.1f}s\n")
        ctx = Context(server, schema, time.strftime("%H%M%S"))
        for case in cases:
            t = time.time()
            try:
                note = case(ctx)
                print(f"PASS  {case.__name__:40} {time.time() - t:5.2f}s  {note or ''}")
            except Exception as e:  # noqa: BLE001 - report every case
                failures += 1
                print(f"FAIL  {case.__name__:40} {time.time() - t:5.2f}s  {e}")
                if args.verbose:
                    traceback.print_exc()
    finally:
        server.stop()
    try:
        log = open(server.log_path, errors="replace").read().splitlines()
        exc = [line for line in log if "Exception" in line or "Unhandled" in line]
        if exc:
            print(f"\nserver log: {len(exc)} exception line(s); first: {exc[0][:160]}")
    except OSError:
        pass
    print(f"\n{len(cases) - failures}/{len(cases)} passed")
    if failures or args.keep:
        print(f"sandbox kept: {SANDBOX} (server log: {server.log_path})")
    else:
        shutil.rmtree(SANDBOX, ignore_errors=True)
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
