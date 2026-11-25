#!/usr/bin/env python3
"""Generate a .reg file that trusts an EpinelPS CA in a Wine prefix.

Wine's wininet validates the TLS chain, so the private CA that signed
EpinelPS's site.pfx must be in the prefix's Root store. Wine does not persist
certificates added at runtime through cert32, so this writes the serialized
certificate blob directly, the same shape Windows uses:

    DWORD 3, DWORD 1, DWORD 20, SHA-1(cert), DWORD 32, DWORD 1,
    DWORD len(DER), DER

Usage:
    # CA certificate that signed EpinelPS/site.pfx (empty password)
    openssl pkcs12 -in EpinelPS/site.pfx -nokeys -cacerts -passin pass: -out ca.pem

    python3 tools/trust-epinelps-ca.py ca.pem > epinelps-ca.reg

    WINEPREFIX=/path/to/prefix wineserver -k
    WINEPREFIX=/path/to/prefix wine reg import epinelps-ca.reg

The thumbprint is printed on stderr. Accepts PEM or DER input; PEM is
converted with the openssl command line tool.
"""

import hashlib
import struct
import subprocess
import sys


def read_der(path: str) -> bytes:
    with open(path, "rb") as fh:
        data = fh.read()
    if b"-----BEGIN" in data:
        return subprocess.check_output(
            ["openssl", "x509", "-in", path, "-outform", "der"]
        )
    return data


def wrap_hex(blob: bytes) -> list[str]:
    full = ",".join(f"{byte:02x}" for byte in blob)
    width = 72  # even, so pairs are not split across lines
    chunks = [full[i:i + width] for i in range(0, len(full), width)]
    lines = []
    for index, chunk in enumerate(chunks):
        line = ('"Blob"=hex:' + chunk) if index == 0 else ("  " + chunk)
        if index < len(chunks) - 1:
            line += "\\"
        lines.append(line)
    return lines


def main() -> int:
    if len(sys.argv) != 2:
        print(f"usage: {sys.argv[0]} <ca.pem|ca.der>", file=sys.stderr)
        return 2

    der = read_der(sys.argv[1])
    if not der:
        print("error: empty certificate", file=sys.stderr)
        return 1

    sha1 = hashlib.sha1(der).digest()
    thumbprint = sha1.hex().upper()
    blob = (
        struct.pack("<III", 3, 1, 20)
        + sha1
        + struct.pack("<III", 32, 1, len(der))
        + der
    )

    print("Windows Registry Editor Version 5.00")
    print()
    print(
        "[HKEY_LOCAL_MACHINE\\Software\\Microsoft\\SystemCertificates\\Root"
        f"\\Certificates\\{thumbprint}]"
    )
    for line in wrap_hex(blob):
        print(line)

    print(f"thumbprint: {thumbprint}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    sys.exit(main())
