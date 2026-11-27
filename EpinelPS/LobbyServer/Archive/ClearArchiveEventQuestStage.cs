using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/event-quest/stage/clear")]
public class ClearArchiveEventQuestStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqClearArchiveEventQuestStage req = await ReadData<ReqClearArchiveEventQuestStage>();
        User user = GetUser();

        if (req.BattleResult == 1 &&
            ArchiveEventQuestHelper.TryGetEpisode(req.ArchiveRecordManagerId, out _, out var manager))
        {
            bool belongs = ArchiveEventQuestHelper.GetStages(manager.Id)
                .Any(s => s.Id == req.EventQuestStageId);
            if (belongs && !user.ClearedArchiveEventQuestStageIds.Contains(req.EventQuestStageId))
            {
                user.ClearedArchiveEventQuestStageIds.Add(req.EventQuestStageId);
                JsonDb.Save();
            }
        }

        await WriteDataAsync(new ResClearArchiveEventQuestStage());
    }
}
