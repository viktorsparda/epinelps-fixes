using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/minigame/towerdefense/enter")]
public class EnterArchiveTowerDefense : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        _ = await ReadData<ReqEnterArchiveTowerDefense>();
        await WriteDataAsync(new ResEnterArchiveTowerDefense());
    }
}

[GameRequest("/archive/minigame/towerdefense/finish")]
public class FinishArchiveTowerDefense : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        _ = await ReadData<ReqFinishArchiveTowerDefense>();
        await WriteDataAsync(new ResFinishArchiveTowerDefense());
    }
}

[GameRequest("/archive/minigame/towerdefense/gettutorialids")]
public class GetArchiveTowerDefenseTutorialIds : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetArchiveTowerDefenseTutorialIds req = await ReadData<ReqGetArchiveTowerDefenseTutorialIds>();
        User user = GetUser();
        var state = ArchiveMiniGameStateStore.Load<TowerDefenseState>(user, $"towerdefense:{req.EventTowerDefenseArchiveManagerId}");
        ResGetArchiveTowerDefenseTutorialIds response = new();
        response.TutorialIds.AddRange(state.TutorialIds);
        await WriteDataAsync(response);
    }

    public class TowerDefenseState
    {
        public List<int> TutorialIds { get; set; } = [];
    }
}

[GameRequest("/archive/minigame/towerdefense/settutorialids")]
public class SetArchiveTowerDefenseTutorialIds : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqSetArchiveTowerDefenseTutorialIds req = await ReadData<ReqSetArchiveTowerDefenseTutorialIds>();
        User user = GetUser();
        var state = ArchiveMiniGameStateStore.Load<GetArchiveTowerDefenseTutorialIds.TowerDefenseState>(
            user, $"towerdefense:{req.EventTowerDefenseArchiveManagerId}");
        state.TutorialIds = [.. req.TutorialIds];
        ArchiveMiniGameStateStore.Save(user, $"towerdefense:{req.EventTowerDefenseArchiveManagerId}", state);
        JsonDb.Save();
        await WriteDataAsync(new ResSetArchiveTowerDefenseTutorialIds());
    }
}
