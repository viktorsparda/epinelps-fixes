using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;
namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/storydungeon/clearstage")]
public class ClearArchiveStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqClearArchiveStage req = await ReadData<ReqClearArchiveStage>(); // has fields EventId, StageId, BattleResult
        int evid = req.EventId;
        int stgid = req.StageId;
        int result = req.BattleResult;
        User user = GetUser();

        // Check if the EventInfo exists for the given EventId
        if (!user.EventInfo.TryGetValue(evid, out EventData? eventData))
        {
            throw new Exception($"Event with ID {evid} not found.");
        }

        // Update the EventData if BattleResult is 1
        bool wasCleared = eventData.ClearedStages.Contains(stgid);
        if (result == 1 && !wasCleared)
        {
            eventData.ClearedStages.Add(stgid);
            // Update the LastStage in EventData
            eventData.LastStage = stgid;
        }

        ResClearArchiveStage response = new();
        // Archive stages have optional first-clear/repeat rewards. Most old
        // events (e.g. Cherry Blossom) have 0 here, but always return a
        // non-null Reward because the client calls UpdateFrom(response.Reward).
        if (result == 1)
            response.Reward = ArchiveStageRewardHelper.Grant(user, stgid, !wasCleared);
        response.Reward ??= new NetRewardData();

        JsonDb.Save();
        await WriteDataAsync(response);
    }
}
