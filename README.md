# EpinelPS — personal fork with fixes

Unofficial fork of [EpinelPS](https://github.com/EpinelPS/EpinelPS), based on
commit [`11fce36`](https://github.com/EpinelPS/EpinelPS/commit/11fce367effe04c14e029cc67419f03c5a9afbdf)
("Update contributing rules", 2026-11-20), plus the changes listed below.

The original project README is kept as
[README.upstream.md](README.upstream.md). This fork is not affiliated with the
EpinelPS developers.

## AI disclosure

The changes in this fork were developed with AI assistance. They were tested by
running the server against the 153.6.10 PC client, but there is no test suite.

Upstream EpinelPS (`CONTRIBUTING.md`) prohibits AI-generated contributions, so
this fork is **not** intended for, and must not be submitted as, an upstream
pull request.

Review the code before relying on it. The persistence and reward paths (the new
`db.json` fields, ticket deduction and box-gacha rolls) are where a mistake has
real consequences.

## What is in this fork

### Bug fixes

- **Console EOF busy-loop** (`Commands/Services/CliLoop.cs`): `ReadLine()`
  returning `null` now stops the loop. Before, a headless server started with
  `/dev/null` stdin re-printed the prompt forever, pinning a core and writing
  roughly 640 KB/s to `server.log`.
- **Max limit break / core upgrade** (`Character/DoLimitBreak.cs`,
  `Character/UpgradeCore.cs`): if the character is maxed or has no next grade,
  the handler now returns the current character. The old empty response reset
  the client character to grade 0 and left the upgrade button enabled. No spare
  body is consumed when nothing happens.
- **Costume ownership** (`Character/GetCharacterCostume.cs`): returns the
  user's owned costumes (`user.CostumeList`) instead of every costume in static
  data. Returning everything made removed costumes appear owned and broke the
  costume-gacha "already owned" state.
- **Newbie package** (`Shop/InApp/GetNewbiePackage.cs`): returns a typed
  response with a `null` `State`. A partial non-null state caused a client
  `NullReferenceException` in `NewbiePackageShopManager.Update`, aborting the
  lobby UI.
- **Event shop lookup** (`Event/Shop/EventShopHelper.cs`): resolves story-event
  shops through `EventStoryTable.ContentsShopId`, and only accepts a shortcut
  that points at an existing entry in `ContentsShopTable`.
- **Archive minigame data** (`Archive/GetMinigameData.cs`): returns `"{}"`
  instead of an empty string.
- **Asset routes** (`Program.cs`): `/prdenv`, `/PC` and `/media` accept `HEAD`
  in addition to `GET`.

### Handlers that were missing or stubs

- **Archive fields**: `/archive/field/enter`, `/archive/field/save`,
  `/archive/field/password-door/list` persist per-map field state (raw JSON,
  completed stages and objects, team positions, progress groups, acquired
  passwords and unlocked doors).
- **Archive DateSim**: `/archive/minigame/DateSim/GetData` and `/FinishDialog`
  persist the client progress JSON and completed dialog ids, with JSON and size
  validation on finish.
- **Event box gacha**: `/event/boxgacha/get` and `/event/boxgacha/execute` load
  the price, probability and reward tables. The server is authoritative for the
  roll count, deducts tickets (merging duplicate stacks) and persists drawn
  reward orders per user.
- **Misc**: `/character/toggle/list` (typed empty lists),
  `/system/battlelog/disallowed-team-types` (empty disallowed list) and
  `/auth/logout` (typed response, logs `EndLogInfo`).

### Persistence

`Models/UserModel.cs` gains three per-user collections, all defaulting to empty
so existing `db.json` saves still load:

- `ArchiveFieldInfo` — archive field state per map id.
- `ArchiveDateSimProgressById` — DateSim progress per minigame id.
- `EventBoxGachaData` — box-gacha draw count and reward orders per event id.

New model classes: `ArchiveFieldInfo`, `ArchiveDateSimProgress`,
`UserEventBoxGachaData`.

### Wine cutscene playback (docs and helper tools)

Story cutscenes need environment fixes outside the server source. See
[`docs/WINE-CUTSCENES.md`](docs/WINE-CUTSCENES.md) for the four pieces: Wine
11.22, the `Urlmon` Media Foundation scheme handler, trusting the local CA
plus routing `cloud.nikke-kr.com` to the server, and the media cache.

Helpers in this repo:

- `tools/wine-shims/cloudredirect.c` — `LD_PRELOAD` resolver shim that maps
  `cloud.nikke-kr.com` to the local server for one process tree.
- `tools/wine-shims/offline-net.c` — optional shim that blocks non-loopback
  connections to test offline playback.
- `tools/wine-shims/scheme-handlers.reg` — the `http:`/`https:` handler
  registration.
- `tools/trust-epinelps-ca.py` — writes the serialized CA blob into a prefix's
  registry (Wine does not persist runtime-installed certs).
- `tools/mfprobe/mfprobe.c` — Winelib probe for
  `MFCreateSourceReaderFromURL`, for verifying a setup.

These are setup/diagnostic tools, not part of the server build.

## Commit layout

1. `Import EpinelPS 11fce36` — exact upstream tree.
2. `Add archive, DateSim, box gacha and missing lobby handlers` — the changes
   above.
3. `Stop the console command loop on EOF` — the CLI fix, independently
   revertable.
4. `Local-only: redirect ch01_end_ja media to loopback HTTP` — **do not use
   this outside a Wine prefix that needs it.** It hardcodes a redirect to
   `http://127.0.0.1/media/ch01_end_ja.mp4` for one cutscene. Drop this commit
   for a clean build.
5. `Document fork provenance, changes and build steps` — README and NOTICE.
6. `Document Wine cutscene setup and add helper tools` — the cutscene guide and
   the shims/cert helper/probe.

## Build

Requires the .NET 10 SDK.

```bash
dotnet publish EpinelPS -c Release -r linux-x64 --self-contained true -o out
```

On Linux the server binds ports 80 and 443 unless the code is changed. Give the
binary the capability or run it as root:

```bash
sudo setcap cap_net_bind_service=+ep out/EpinelPS
```

`gameconfig.json` and `gameversion.json` must be next to the binary (the
publish output includes them). On first start the server downloads
`StaticData.pack` from the URL in `gameconfig.json`, which requires internet.

## Notes

- The server is built for game version `153.6.10` (`TargetVersion` in
  `gameconfig.json`). A different client version needs a matching config and
  static data.
- Tested with Wine 11.22 on Linux, but nothing here is Linux-specific beyond the
  local-only commit.
- Back up `db.json` and `epinelps.db` before switching a real save to this
  build.
- Some archive DateSim and event-shop code was already present in the working
  tree when this fork was assembled (an earlier local
  `event-fixes-11fce36.patch`) and is included as-is.

## License

GNU Affero General Public License v3.0 — see [LICENSE](LICENSE). The original
copyright and license notices are retained; see [NOTICE.md](NOTICE.md) for the
modification notice.
