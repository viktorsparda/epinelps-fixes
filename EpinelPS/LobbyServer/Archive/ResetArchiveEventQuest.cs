using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/event-quest/reset")]
public class ResetArchiveEventQuest : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqResetArchiveEventQuest req = await ReadData<ReqResetArchiveEventQuest>();
        User user = GetUser();

        if (ArchiveEventQuestHelper.TryGetEpisode(req.ArchiveRecordManagerId, out _, out var manager))
        {
            HashSet<int> questIds = [.. ArchiveEventQuestHelper.GetQuests(manager.Id).Select(q => q.Id)];
            HashSet<int> stageIds = [.. ArchiveEventQuestHelper.GetStages(manager.Id).Select(s => s.Id)];
            user.ClearedArchiveEventQuestIds.RemoveAll(questIds.Contains);
            user.ClearedArchiveEventQuestStageIds.RemoveAll(stageIds.Contains);
            JsonDb.Save();
        }

        await WriteDataAsync(new ResResetArchiveEventQuest());
    }
}
