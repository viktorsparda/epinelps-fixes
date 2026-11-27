using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/event-quest/deactivate")]
public class DeactivateArchiveEventQuest : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqDeactivateArchiveEventQuest req = await ReadData<ReqDeactivateArchiveEventQuest>();
        User user = GetUser();

        if (user.ActivatedArchiveEventQuestRecordId == req.ArchiveRecordManagerId)
        {
            user.ActivatedArchiveEventQuestRecordId = 0;
            JsonDb.Save();
        }

        await WriteDataAsync(new ResDeactivateArchiveEventQuest());
    }
}
