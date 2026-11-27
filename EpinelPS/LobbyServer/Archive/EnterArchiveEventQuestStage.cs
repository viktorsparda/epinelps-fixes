using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/event-quest/stage/enter")]
public class EnterArchiveEventQuestStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqEnterArchiveEventQuestStage req = await ReadData<ReqEnterArchiveEventQuestStage>();

        // Entering is a client-side battle load; the server only needs to accept it.
        await WriteDataAsync(new ResEnterArchiveEventQuestStage());
    }
}
