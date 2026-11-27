using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/event-quest/clear")]
public class ClearArchiveEventQuest : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqClearArchiveEventQuest req = await ReadData<ReqClearArchiveEventQuest>();
        User user = GetUser();

        if (ArchiveEventQuestHelper.TryGetEpisode(req.ArchiveRecordManagerId, out _, out var manager))
        {
            bool belongs = ArchiveEventQuestHelper.GetQuests(manager.Id)
                .Any(q => q.Id == req.ArchiveEventQuestId);
            if (belongs && !user.ClearedArchiveEventQuestIds.Contains(req.ArchiveEventQuestId))
            {
                user.ClearedArchiveEventQuestIds.Add(req.ArchiveEventQuestId);
                JsonDb.Save();
            }
        }

        await WriteDataAsync(new ResClearArchiveEventQuest());
    }
}
