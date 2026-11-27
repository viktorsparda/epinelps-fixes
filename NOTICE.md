# NOTICE

This repository is a modified version of **EpinelPS**:

- Original project: <https://github.com/EpinelPS/EpinelPS>
- Base commit: `11fce367effe04c14e029cc67419f03c5a9afbdf` (2026-11-20)
- License: GNU Affero General Public License v3.0 (see `LICENSE`)

Modifications were made on 2026-11-24 and 2026-11-25. They are described in
`README.md` and in the Git history after the `Import EpinelPS 11fce36` commit.

The modified files are:

- `EpinelPS/Program.cs`
- `EpinelPS/Data/GameData.cs`
- `EpinelPS/Models/UserModel.cs`
- `EpinelPS/Commands/Services/CliLoop.cs`
- `EpinelPS/LobbyServer/Archive/GetMinigameData.cs`
- `EpinelPS/LobbyServer/Character/DoLimitBreak.cs`
- `EpinelPS/LobbyServer/Character/GetCharacterCostume.cs`
- `EpinelPS/LobbyServer/Character/UpgradeCore.cs`
- `EpinelPS/LobbyServer/Event/ExecuteEventBoxGacha.cs`
- `EpinelPS/LobbyServer/Event/GetEventBoxGacha.cs`
- `EpinelPS/LobbyServer/Event/EventBoxGachaHelper.cs` (new 2026-11-25)
- `EpinelPS/LobbyServer/Event/Shop/EventShopHelper.cs`
- and the new files added under `EpinelPS/LobbyServer/` and `EpinelPS/Models/`
  in commit 2 of the history.

The changes were developed with AI assistance; see the AI disclosure in
`README.md`. This fork is not affiliated with or endorsed by the EpinelPS
developers.

## Bundled third-party components

- `launcher/` is an unmodified copy of the EpinelPSLauncher source from
  <https://github.com/EpinelPS/EpinelPSLauncher> at commit
  `d219c64347328de627764dcfd2f82f31199a7569`, licensed AGPL-3.0 (its `LICENSE`
  is included in that directory). See `launcher/BUNDLED.md`.
- `vendor/launcher-windows/` contains the four Windows-only helper binaries
  built from that same launcher commit, with `SHA256SUMS`. See its `README.md`.
- `vendor/server-data/gamecommon.json` is the runtime configuration file from
  the official EpinelPS distribution; it is required at server startup and is
  not part of the upstream source tree.

The server source modifications and the helper scripts/tools in `scripts/`,
`tools/` and `docs/` are part of this fork.
