# Server runtime data

`gamecommon.json` is required at server startup (`GameData`/`Program` read it
from the binary's directory). It is not part of the upstream source tree and is
not produced by `dotnet publish`, so it is vendored here from the official
EpinelPS release so that a build from this repository can run.

It is a small configuration blob (the `SmallNumber` value used when reading
static data), not a game asset. `scripts/install.sh` copies it into
`$EPINELPS_DATA/server/` if that file does not exist yet; an existing
installation's copy is never overwritten.

If you already have an EpinelPS installation, you can use its
`server/gamecommon.json` instead.
