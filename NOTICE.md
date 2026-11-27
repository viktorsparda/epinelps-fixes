# NOTICE

This repository is a modified version of **EpinelPS**:

- Original project: <https://github.com/EpinelPS/EpinelPS>
- Base commit: `11fce367effe04c14e029cc67419f03c5a9afbdf` (2026-11-20)
- License: GNU Affero General Public License v3.0 (see `LICENSE`)

Modifications were made between 2026-11-24 and 2026-11-26. They are described
in `README.md` and in the Git history after the `Import EpinelPS 11fce36` commit.

The modified upstream files are:

- `EpinelPS/Commands/Services/CliLoop.cs`
- `EpinelPS/Data/GameData.cs`
- `EpinelPS/LobbyServer/Archive/ArchiveClearStage.cs`
- `EpinelPS/LobbyServer/Archive/FastClearArchiveStage.cs`
- `EpinelPS/LobbyServer/Archive/GetArchives.cs`
- `EpinelPS/LobbyServer/Archive/GetMinigameData.cs`
- `EpinelPS/LobbyServer/Character/DoLimitBreak.cs`
- `EpinelPS/LobbyServer/Character/GetCharacterCostume.cs`
- `EpinelPS/LobbyServer/Character/UpgradeCore.cs`
- `EpinelPS/LobbyServer/Event/ExecuteEventBoxGacha.cs`
- `EpinelPS/LobbyServer/Event/GetChallengeStage.cs`
- `EpinelPS/LobbyServer/Event/GetEventBoxGacha.cs`
- `EpinelPS/LobbyServer/Event/Shop/EventShopHelper.cs`
- `EpinelPS/LobbyServer/Gacha/ExecGacha.cs`
- `EpinelPS/LobbyServer/Gacha/ExecutePityGacha.cs`
- `EpinelPS/LobbyServer/Intercept/AnomalousFastClear.cs`
- `EpinelPS/LobbyServer/Intercept/AnomalousFinish.cs`
- `EpinelPS/LobbyServer/Inventory/UsePiece.cs`
- `EpinelPS/LobbyServer/LobbyHandler.cs`
- `EpinelPS/LobbyServer/LobbyUser/Mail/ReadMail.cs`
- `EpinelPS/LobbyServer/Minigame/nksv2/Get.cs`
- `EpinelPS/Models/Admin/RunCmdModel.cs`
- `EpinelPS/Models/DbModels.cs`
- `EpinelPS/Models/UserModel.cs`
- `EpinelPS/Program.cs`
- `EpinelPS/Utils/AdminCommands.cs`
- `EpinelPS/Utils/RewardUtils.cs`

The removed upstream files (stub handlers replaced by
`EpinelPS/LobbyServer/Minigame/IslandAdventure/LegacyIslandAdventure.cs`) are:

- `EpinelPS/LobbyServer/Minigame/IslandAdventure/EventCurrency.cs`
- `EpinelPS/LobbyServer/Minigame/IslandAdventure/EventInventory.cs`
- `EpinelPS/LobbyServer/Minigame/IslandAdventure/FishSpotCount.cs`
- `EpinelPS/LobbyServer/Minigame/IslandAdventure/GetFishingStepUpRewardStatus.cs`
- `EpinelPS/LobbyServer/Minigame/IslandAdventure/GetPhotoStepUpRewardStatus.cs`
- `EpinelPS/LobbyServer/Minigame/IslandAdventure/ListMission.cs`
- `EpinelPS/LobbyServer/Minigame/IslandAdventure/PhotoData.cs`

All other files under `EpinelPS/` that are not in the import commit are new
files added by this fork.

The changes were developed with AI assistance; see the AI disclosure in
`README.md`. This fork is not affiliated with or endorsed by the EpinelPS
developers.

## Bundled third-party components

- `launcher/` is an unmodified copy of the EpinelPSLauncher source from
  <https://github.com/EpinelPS/EpinelPSLauncher> at commit
  `d219c64347328de627764dcfd2f82f31199a7569`, licensed AGPL-3.0 (its `LICENSE`
  is included in that directory). See `launcher/BUNDLED.md`. The launcher
  binary built by `scripts/build-launcher.sh` is a **modified** version: it
  applies `patches/launcher-private-hosts.patch` (changes
  `EpinelPSLauncher/Utils/PathUtil.cs` and
  `EpinelPSLauncher/Utils/ServerSwitcher.cs`, 2026-11-25) to a temporary copy
  so that, on Linux, the launcher uses a private hosts file instead of
  `/etc/hosts`.
- `vendor/launcher-windows/` contains the four Windows-only helper binaries
  built from that same launcher commit, with `SHA256SUMS`. See its `README.md`.
- `vendor/server-data/gamecommon.json` is the runtime configuration file from
  the official EpinelPS distribution; it is required at server startup and is
  not part of the upstream source tree.

The server source modifications and the helper scripts/tools in `scripts/`,
`tools/` and `docs/` are part of this fork.
