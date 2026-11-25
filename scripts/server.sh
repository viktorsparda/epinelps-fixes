#!/usr/bin/env bash
# Start, stop or check the installed EpinelPS server.
#
#   ./scripts/server.sh status
#   ./scripts/server.sh start
#   ./scripts/server.sh stop
#   ./scripts/server.sh restart
#
# Data directory: $EPINELPS_DATA, else $XDG_DATA_HOME/EpinelPS, else
# ~/.local/share/EpinelPS.
#
# `start` never redirects stdin from /dev/null. The console command loop treats
# EOF as an empty line and re-prints its prompt forever, which pins a CPU core
# and writes hundreds of KB/s to server.log. A read-write FIFO is used instead,
# so Console.ReadLine() blocks.
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
DATA="${EPINELPS_DATA:-${XDG_DATA_HOME:-$HOME/.local/share}/EpinelPS}"
SERVER_DIR="$DATA/server"
SERVER="$SERVER_DIR/EpinelPS"
PIDFILE="$SERVER_DIR/server.pid"
LOGFILE="$SERVER_DIR/server.log"
STDIN_FIFO="$SERVER_DIR/.server-stdin.fifo"

die() { echo "server: $*" >&2; exit 1; }

server_up() { curl -skf -o /dev/null --max-time 2 https://127.0.0.1/ 2>/dev/null; }

# 0 = pid, 1 = not running, 2 = pidfile exists but process is outside this PID namespace
server_pid() {
    local pid
    pid="$(cat "$PIDFILE" 2>/dev/null)" || return 1
    [[ -n "$pid" ]] || return 1
    if [[ ! -e "/proc/$pid" ]]; then
        server_up && return 2
        return 1
    fi
    kill -0 "$pid" 2>/dev/null || return 1
    if [[ -r "/proc/$pid/cmdline" ]]; then
        tr '\0' ' ' < "/proc/$pid/cmdline" | grep -q 'EpinelPS' || return 1
    fi
    echo "$pid"
}

can_bind_low_ports() {
    [[ "$(id -u)" = 0 ]] && return 0
    local start
    start="$(cat /proc/sys/net/ipv4/ip_unprivileged_port_start 2>/dev/null || echo 1024)"
    [[ "$start" -le 80 ]] && return 0
    local getcap
    getcap="$(command -v getcap || echo /usr/sbin/getcap)"
    [[ -x "$getcap" ]] && "$getcap" "$SERVER" 2>/dev/null | grep -q cap_net_bind_service
}

stop_via_api() {
    local user="${EPINELPS_ADMIN_USER:-}" pass="${EPINELPS_ADMIN_PASS:-}" token
    [[ -n "$user" && -n "$pass" ]] || return 1
    token="$(curl -sk --max-time 5 https://127.0.0.1/adminapi/login \
        -H 'Content-Type: application/json' \
        -d "{\"Username\":\"$user\",\"Password\":\"$pass\"}" \
        | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')"
    [[ -n "$token" ]] || return 1
    curl -sk --max-time 5 https://127.0.0.1/adminapi/RunCmd \
        -H "Authorization: Bearer $token" -H 'Content-Type: application/json' \
        -d '{"cmdName":"exit"}' >/dev/null 2>&1 || return 1
    for _ in $(seq 1 50); do
        server_up || return 0
        sleep 0.2
    done
    return 1
}

do_status() {
    local pid rc
    if pid="$(server_pid)"; then rc=0; else rc=$?; fi
    case "$rc" in
        0) echo "running (pid $pid)" ;;
        2) echo "running (pid $(cat "$PIDFILE"), outside this PID namespace)" ;;
        *) echo "stopped"; return 1 ;;
    esac
    if [[ -f "$LOGFILE" ]]; then
        echo "log:   $LOGFILE ($(( $(stat -c %s "$LOGFILE") / 1024 / 1024 )) MB)"
    fi
}

do_start() {
    local pid rc
    if pid="$(server_pid)"; then rc=0; else rc=$?; fi
    if [[ "$rc" = 0 || "$rc" = 2 ]]; then
        echo "server already running"
        return 0
    fi
    [[ -x "$SERVER" ]] || die "missing server binary: $SERVER (run ./scripts/install.sh)"
    [[ -f "$SERVER_DIR/gamecommon.json" ]] || die "$SERVER_DIR/gamecommon.json is missing (see vendor/server-data/README.md)"
    can_bind_low_ports || die "the server needs cap_net_bind_service for ports 80/443; grant it once with: sudo setcap cap_net_bind_service=+ep '$SERVER'"

    if [[ -f "$LOGFILE" ]]; then
        if [[ "$(stat -c %s "$LOGFILE" 2>/dev/null || echo 0)" -gt 20000000 ]]; then
            rm -f "$LOGFILE" "$LOGFILE.old"
        else
            mv -f "$LOGFILE" "$LOGFILE.old"
        fi
    fi

    cd "$SERVER_DIR"
    if [[ ! -p "$STDIN_FIFO" ]]; then
        rm -f "$STDIN_FIFO"
        mkfifo -m 600 "$STDIN_FIFO" || die "cannot create $STDIN_FIFO"
    fi
    setsid nohup sh -c 'exec 3<>"$0" || exit 1; exec ./EpinelPS --headless <&3' "$STDIN_FIFO" \
        >"$LOGFILE" 2>&1 &
    echo $! > "$PIDFILE"
    sleep 1
    if ! pid="$(server_pid)"; then
        tail -n 20 "$LOGFILE" >&2
        die "server exited right away (see $LOGFILE)"
    fi
    echo "server started (pid $pid), log: $LOGFILE"
}

do_stop() {
    local pid rc
    if pid="$(server_pid)"; then rc=0; else rc=$?; fi
    if [[ "$rc" = 2 ]]; then
        if stop_via_api; then
            rm -f "$PIDFILE"
            echo "server stopped (via admin API)"
            return 0
        fi
        die "server runs outside this PID namespace; set EPINELPS_ADMIN_USER/EPINELPS_ADMIN_PASS for the admin API or stop it manually"
    fi
    if [[ "$rc" != 0 ]]; then
        echo "server is not running"
        rm -f "$PIDFILE"
        return 0
    fi
    kill "$pid"
    for _ in $(seq 1 50); do
        kill -0 "$pid" 2>/dev/null || break
        sleep 0.2
    done
    kill -0 "$pid" 2>/dev/null && kill -9 "$pid"
    rm -f "$PIDFILE"
    echo "server stopped"
}

case "${1:-status}" in
    status)  do_status ;;
    start)   do_start ;;
    stop)    do_stop ;;
    restart) do_stop; do_start ;;
    *) echo "usage: $0 [status|start|stop|restart]" >&2; exit 2 ;;
esac
