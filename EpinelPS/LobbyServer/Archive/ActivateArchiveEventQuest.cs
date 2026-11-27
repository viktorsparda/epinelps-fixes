using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/event-quest/activate")]
public class ActivateArchiveEventQuest : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqActivateArchiveEventQuest req = await ReadData<ReqActivateArchiveEventQuest>();
        User user = GetUser();

        if (ArchiveEventQuestHelper.TryGetEpisode(req.ArchiveRecordManagerId, out _, out _))
        {
            user.ActivatedArchiveEventQuestRecordId = req.ArchiveRecordManagerId;
            JsonDb.Save();
        }

        await WriteDataAsync(new ResActivateArchiveEventQuest());
    }
}
