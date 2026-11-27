using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/minigame/setdata")]
public class SetMinigameData : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqSetArchiveMiniGameData req = await ReadData<ReqSetArchiveMiniGameData>();
        User user = GetUser();

        if (!string.IsNullOrEmpty(req.Json))
        {
            user.ArchiveMiniGameData[req.EventId] = req.Json;
            if (ArchiveMiniGameHelper.ResolveManager(req.EventId) is { } manager)
                ArchiveMiniGameHelper.ApplyState(user, manager, req.Json);
            JsonDb.Save();
        }

        await WriteDataAsync(new ResSetArchiveMiniGameData());
    }
}
