#!/usr/bin/env python3
"""Named protobuf encode/decode for EpinelPS lobby messages, without protoc.

The schema is scraped from the server's generated C# (`Protos/Generated/
allmsgs.cs`): every `XFieldNumber = N` constant is paired with the property
declaration that follows it. The client protocol uses no sint/fixed types, so
the C# property type is enough to pick the wire encoding.

Route -> request/response types are scraped from the handler sources
(`[GameRequest("/path")]`, `ReadData<ReqX>()`, `new ResY`).

Both results are cached as JSON next to the caller's choice of cache dir and
rebuilt when the sources change.

    schema = Schema.load(src_dir, cache_dir)
    body = schema.encode("ReqChallengeEventStageData", {"EventId": 60123})
    msg = schema.decode("ResChallengeEventStageData", raw)
"""
from __future__ import annotations

import json
import os
import re
import struct

SCALARS = {
    "int": "int32", "long": "int64", "uint": "uint32", "ulong": "uint64",
    "bool": "bool", "string": "string", "double": "double", "float": "float",
    "pb::ByteString": "bytes",
}
WELL_KNOWN = {
    "Google.Protobuf.WellKnownTypes.Timestamp": {
        "Seconds": {"num": 1, "type": "int64", "label": "single"},
        "Nanos": {"num": 2, "type": "int32", "label": "single"},
    },
    "Google.Protobuf.WellKnownTypes.Duration": {
        "Seconds": {"num": 1, "type": "int64", "label": "single"},
        "Nanos": {"num": 2, "type": "int32", "label": "single"},
    },
}
CACHE_VERSION = 4

_CLASS = re.compile(r"^(\s*)public sealed partial class (\w+) : pb::IMessage<")
_TYPES = re.compile(r"^(\s*)public static partial class Types \{")
_ENUM = re.compile(r"^(\s*)public enum (\w+) \{")
_ENUM_VAL = re.compile(r"^\s*\[pbr::OriginalName\(\"[^\"]*\"\)\] (\w+) = (-?\d+),")
_FIELDNUM = re.compile(r"^\s*public const int (\w+)FieldNumber = (\d+);")
_PROP = re.compile(r"^\s*public (.+?) (\w+) \{\s*$")
_ROUTE = re.compile(r'\[GameRequest\("([^"]+)"\)\]')
_READ = re.compile(r"ReadData<(\w+)>")
_RES = re.compile(r"\b(Res[A-Z]\w*)\s+\w+\s*=\s*new\b|new (Res[A-Z]\w*)\b")


def _norm(t: str) -> str:
    t = t.strip().removeprefix("global::").removeprefix("EpinelPS.")
    return t.replace(".Types.", ".")


def _field_type(decl: str):
    decl = decl.strip()
    m = re.fullmatch(r"pbc::RepeatedField<(.+)>", decl)
    if m:
        return "repeated", _scalar_or_ref(m.group(1)), None
    m = re.fullmatch(r"pbc::MapField<(.+?), (.+)>", decl)
    if m:
        return "map", _scalar_or_ref(m.group(2)), _scalar_or_ref(m.group(1))
    return "single", _scalar_or_ref(decl), None


def _scalar_or_ref(t: str) -> str:
    t = t.strip()
    return SCALARS.get(t, None) or _norm(t)


def parse_messages(allmsgs: str) -> dict:
    messages: dict[str, dict] = {}
    enums: dict[str, dict] = {}
    stack: list[tuple[int, str]] = []  # (indent, name) of enclosing messages
    pending = None
    cur_enum = None
    with open(allmsgs, encoding="utf-8") as f:
        for line in f:
            m = _CLASS.match(line)
            if m:
                indent = len(m.group(1))
                while stack and stack[-1][0] >= indent:
                    stack.pop()
                stack.append((indent, m.group(2)))
                full = ".".join(s[1] for s in stack)
                messages[full] = {}
                cur_enum = None
                continue
            m = _ENUM.match(line)
            if m:
                indent = len(m.group(1))
                while stack and stack[-1][0] >= indent:
                    stack.pop()
                full = ".".join([s[1] for s in stack] + [m.group(2)])
                enums[full] = {}
                cur_enum = full
                continue
            if cur_enum:
                m = _ENUM_VAL.match(line)
                if m:
                    enums[cur_enum][m.group(1)] = int(m.group(2))
                    continue
                if line.strip() == "}":
                    cur_enum = None
                continue
            if _TYPES.match(line):
                continue
            m = _FIELDNUM.match(line)
            if m and stack:
                pending = (m.group(1), int(m.group(2)))
                continue
            if pending:
                m = _PROP.match(line)
                if m and m.group(2) == pending[0]:
                    label, typ, key = _field_type(m.group(1))
                    full = ".".join(s[1] for s in stack)
                    fld = {"num": pending[1], "type": typ, "label": label}
                    if key:
                        fld["key"] = key
                    messages[full][pending[0]] = fld
                    pending = None
    for name, fields in WELL_KNOWN.items():
        messages[name] = fields
    return {"messages": messages, "enums": enums}


def parse_routes(lobby_dir: str) -> dict:
    routes = {}
    for root, _, files in os.walk(lobby_dir):
        for fn in files:
            if not fn.endswith(".cs"):
                continue
            text = open(os.path.join(root, fn), encoding="utf-8").read()
            matches = list(_ROUTE.finditer(text))
            if not matches:
                continue
            file_req = _READ.search(text)
            file_res = _RES.search(text)
            # Several handler classes can share a file; scan each attribute's
            # own segment first, then fall back to the file-wide match.
            # Stacked attributes on one class share the segment after the last.
            for idx, m in enumerate(matches):
                start = m.end()
                j = idx + 1
                while j < len(matches) and not text[start:matches[j].start()].strip(" \t\r\n]["):
                    start = matches[j].end()
                    j += 1
                end = matches[j].start() if j < len(matches) else len(text)
                seg = text[start:end]
                req = _READ.search(seg) or file_req
                res = _RES.search(seg) or file_res
                routes[m.group(1)] = {
                    "req": req.group(1) if req else None,
                    "res": (res.group(1) or res.group(2)) if res else None,
                    "file": os.path.relpath(os.path.join(root, fn), lobby_dir),
                }
    return routes


def _varint(n: int) -> bytes:
    n &= (1 << 64) - 1
    out = bytearray()
    while True:
        b = n & 0x7F
        n >>= 7
        if n:
            out.append(b | 0x80)
        else:
            out.append(b)
            return bytes(out)


def _read_varint(buf: bytes, i: int):
    shift = val = 0
    while True:
        b = buf[i]
        i += 1
        val |= (b & 0x7F) << shift
        if not b & 0x80:
            return val, i
        shift += 7


def _signed(v: int, bits: int) -> int:
    v &= (1 << 64) - 1
    if v >= 1 << 63:
        v -= 1 << 64
    if bits == 32:
        v = ((v + (1 << 31)) % (1 << 32)) - (1 << 31)
    return v


VARINT_TYPES = {"int32", "int64", "uint32", "uint64", "bool"}


class Schema:
    def __init__(self, data: dict):
        self.messages = data["messages"]
        self.enums = data["enums"]
        self.routes = data["routes"]

    @classmethod
    def load(cls, src_dir: str, cache_dir: str) -> "Schema":
        allmsgs = os.path.join(src_dir, "Protos", "Generated", "allmsgs.cs")
        lobby = os.path.join(src_dir, "LobbyServer")
        stamp = [CACHE_VERSION, os.path.getmtime(allmsgs),
                 max(os.path.getmtime(os.path.join(r, f))
                     for r, _, fs in os.walk(lobby) for f in fs if f.endswith(".cs"))]
        cache = os.path.join(cache_dir, "proto-schema.json")
        try:
            with open(cache) as f:
                data = json.load(f)
            if data.get("stamp") == stamp:
                return cls(data)
        except (OSError, ValueError):
            pass
        data = parse_messages(allmsgs)
        data["routes"] = parse_routes(lobby)
        data["stamp"] = stamp
        os.makedirs(cache_dir, exist_ok=True)
        with open(cache, "w") as f:
            json.dump(data, f)
        return cls(data)

    # ---- kind helpers -------------------------------------------------
    def _kind(self, typ: str) -> str:
        if typ in VARINT_TYPES:
            return "varint"
        if typ in ("string", "bytes"):
            return "len"
        if typ == "double":
            return "f64"
        if typ == "float":
            return "f32"
        if typ in self.enums:
            return "enum"
        if typ in self.messages:
            return "message"
        raise KeyError(f"unknown proto type {typ!r}")

    def fields(self, msg: str) -> dict:
        if msg not in self.messages:
            raise KeyError(f"unknown message {msg!r}")
        return self.messages[msg]

    # ---- encode -------------------------------------------------------
    def encode(self, msg: str, value: dict) -> bytes:
        fields = self.fields(msg)
        out = bytearray()
        for name, v in value.items():
            if name not in fields:
                raise KeyError(f"{msg} has no field {name!r}; fields: {sorted(fields)}")
            f = fields[name]
            if f["label"] == "repeated":
                for item in v:
                    out += self._encode_one(f["num"], f["type"], item)
            elif f["label"] == "map":
                items = v.items() if isinstance(v, dict) else v
                for k, item in items:
                    entry = (self._encode_one(1, f["key"], k)
                             + self._encode_one(2, f["type"], item))
                    out += _varint(f["num"] << 3 | 2) + _varint(len(entry)) + entry
            else:
                out += self._encode_one(f["num"], f["type"], v)
        return bytes(out)

    def _encode_one(self, num: int, typ: str, v) -> bytes:
        kind = self._kind(typ)
        if kind == "enum":
            if isinstance(v, str):
                v = self.enums[typ][v]
            kind = "varint"
        if kind == "varint":
            return _varint(num << 3) + _varint(int(v))
        if kind == "f64":
            return _varint(num << 3 | 1) + struct.pack("<d", v)
        if kind == "f32":
            return _varint(num << 3 | 5) + struct.pack("<f", v)
        if kind == "message":
            payload = self.encode(typ, v)
        elif typ == "bytes":
            payload = bytes.fromhex(v) if isinstance(v, str) else bytes(v)
        else:
            payload = str(v).encode()
        return _varint(num << 3 | 2) + _varint(len(payload)) + payload

    # ---- decode -------------------------------------------------------
    def decode(self, msg: str, buf: bytes) -> dict:
        """Decode to a dict of field name -> value. Absent scalars are left out
        (use .get(name, 0)); repeated fields are lists, maps are dicts."""
        fields = self.fields(msg)
        by_num = {f["num"]: (n, f) for n, f in fields.items()}
        out: dict = {}
        i = 0
        while i < len(buf):
            key, i = _read_varint(buf, i)
            num, wire = key >> 3, key & 7
            if wire == 0:
                raw, i = _read_varint(buf, i)
            elif wire == 1:
                raw, i = buf[i:i + 8], i + 8
            elif wire == 5:
                raw, i = buf[i:i + 4], i + 4
            elif wire == 2:
                ln, i = _read_varint(buf, i)
                raw, i = buf[i:i + ln], i + ln
            else:
                raise ValueError(f"{msg}: unsupported wire type {wire}")
            if num not in by_num:
                out.setdefault("_unknown", []).append((num, wire))
                continue
            name, f = by_num[num]
            if f["label"] == "map":
                entry = self._decode_map_entry(f, raw)
                out.setdefault(name, {}).update([entry])
            elif f["label"] == "repeated":
                lst = out.setdefault(name, [])
                if wire == 2 and self._kind(f["type"]) in ("varint", "enum", "f32", "f64"):
                    lst.extend(self._unpack(f["type"], raw))
                else:
                    lst.append(self._decode_one(f["type"], wire, raw))
            else:
                out[name] = self._decode_one(f["type"], wire, raw)
        return out

    def _decode_map_entry(self, f, raw):
        k = v = None
        i = 0
        while i < len(raw):
            key, i = _read_varint(raw, i)
            num, wire = key >> 3, key & 7
            if wire == 0:
                val, i = _read_varint(raw, i)
            else:
                ln, i = _read_varint(raw, i)
                val, i = raw[i:i + ln], i + ln
            typ = f["key"] if num == 1 else f["type"]
            dec = self._decode_one(typ, wire, val)
            if num == 1:
                k = dec
            else:
                v = dec
        if v is None:
            v = {} if self._kind(f["type"]) == "message" else 0
        return k, v

    def _unpack(self, typ, raw):
        kind = self._kind(typ)
        out, i = [], 0
        while i < len(raw):
            if kind in ("varint", "enum"):
                v, i = _read_varint(raw, i)
                out.append(self._decode_one(typ, 0, v))
            elif kind == "f64":
                out.append(struct.unpack("<d", raw[i:i + 8])[0])
                i += 8
            else:
                out.append(struct.unpack("<f", raw[i:i + 4])[0])
                i += 4
        return out

    def _decode_one(self, typ, wire, raw):
        kind = self._kind(typ)
        if kind == "varint":
            if typ == "bool":
                return bool(raw)
            if typ == "int32":
                return _signed(raw, 32)
            if typ == "int64":
                return _signed(raw, 64)
            return raw
        if kind == "enum":
            names = {v: k for k, v in self.enums[typ].items()}
            return names.get(_signed(raw, 32), _signed(raw, 32))
        if kind == "f64":
            return struct.unpack("<d", raw)[0]
        if kind == "f32":
            return struct.unpack("<f", raw)[0]
        if kind == "message":
            return self.decode(typ, raw)
        if typ == "bytes":
            return raw.hex()
        return raw.decode("utf-8", "replace")
