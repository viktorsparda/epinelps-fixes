using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;
namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/storydungeon/fastclearstage")]
public class FastClearArchiveStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqFastClearArchiveStage req = await ReadData<ReqFastClearArchiveStage>();
        int evid = req.EventId;
        int stgid = req.StageId;

        User user = GetUser();

        // Check if the EventInfo exists for the given EventId
        if (!user.EventInfo.TryGetValue(evid, out EventData? eventData))
        {
            throw new Exception($"Event with ID {evid} not found.");
        }
        bool wasCleared = eventData.ClearedStages.Contains(stgid);
        if (!wasCleared)
        {
            eventData.ClearedStages.Add(stgid);
            // Update the LastStage in EventData
            eventData.LastStage = stgid;
        }

        ResFastClearArchiveStage response = new();
        response.Reward = ArchiveStageRewardHelper.Grant(user, stgid, !wasCleared);
        response.Reward ??= new NetRewardData();

        JsonDb.Save();
        await WriteDataAsync(response);
    }
}
