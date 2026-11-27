using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Event;

[GameRequest("/event/challengestage/enter")]
public class EnterChallengeEventStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqEnterChallengeEventStage req = await ReadData<ReqEnterChallengeEventStage>();
        User user = GetUser();

        // Ensure the event state exists so a later clear can be recorded.
        if (!user.EventInfo.ContainsKey(req.EventId))
        {
            user.EventInfo[req.EventId] = new EventData();
            JsonDb.Save();
        }

        await WriteDataAsync(new ResEnterChallengeEventStage());
    }
}
