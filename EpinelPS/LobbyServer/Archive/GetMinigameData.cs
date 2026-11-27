using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/minigame/getdata")]
public class GetMinigameData : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetArchiveMiniGameData req = await ReadData<ReqGetArchiveMiniGameData>();
        User user = GetUser();

        ResGetArchiveMiniGameData response = new()
        {
            Json = "{}"
        };

        if (ArchiveMiniGameHelper.ResolveManager(req.EventId) is { } manager)
        {
            // NKS archive minigames need a schema-complete state document; the
            // client's NksArchiveState.Load dereferences every mission list.
            // Rebuild from the NKS save each time so schema/progress changes
            // cannot leave a stale document behind.
            response.Json = ArchiveMiniGameHelper.BuildState(user, manager);
            user.ArchiveMiniGameData[req.EventId] = response.Json;
            user.ArchiveNksManagerId = manager.Id;
            JsonDb.Save();
        }
        else if (user.ArchiveMiniGameData.TryGetValue(req.EventId, out string? stored) &&
                 IsUsable(stored))
        {
            response.Json = stored;
        }

        await WriteDataAsync(response);
    }

    private static bool IsUsable(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}") return false;
        try
        {
            return Newtonsoft.Json.Linq.JObject.Parse(json)
                .ContainsKey("NksArchiveMissionSerializable");
        }
        catch
        {
            return false;
        }
    }
}
