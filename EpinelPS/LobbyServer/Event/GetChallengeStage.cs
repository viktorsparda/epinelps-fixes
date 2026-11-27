using EpinelPS.Data;
using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Event;

[GameRequest("/event/challengestage/get")]
public class GetChallengeStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqChallengeEventStageData req = await ReadData<ReqChallengeEventStageData>();
        User user = GetUser();

        if (!user.EventInfo.TryGetValue(req.EventId, out EventData? eventData))
        {
            eventData = new EventData { LastStage = 0 };
            user.EventInfo[req.EventId] = eventData;
        }

        int ticketId = ChallengeEventHelper.GetTicketItemId(req.EventId);
        DbItemData? ticket = ticketId > 0
            ? user.Items.FirstOrDefault(i => i.ItemType == ticketId)
            : null;

        // First access to a challenge event grants the starting 3 tickets once.
        if (ticketId > 0 && ticket == null && !eventData.ChallengeTicketsGranted)
        {
            ticket = new DbItemData
            {
                ItemType = ticketId,
                Csn = 0,
                Count = 3,
                Level = 0,
                Exp = 0,
                Position = 0,
                Corp = 0,
                Isn = user.GenerateUniqueItemId(),
            };
            user.Items.Add(ticket);
            eventData.ChallengeTicketsGranted = true;
            JsonDb.Save();
        }

        ResChallengeEventStageData response = new()
        {
            RemainTicket = ticket?.Count ?? 0,
            TeamData = new NetUserTeamData
            {
                Type = (int)TeamType.StoryEvent
            },
        };
        if (user.UserTeams.TryGetValue((int)TeamType.StoryEvent, out NetUserTeamData? teamData))
        {
            response.TeamData = teamData;
        }
        response.LastClearedEventStageList.Add(new NetLastClearedEventStageData()
        {
            DifficultyId = eventData.Diff,
            StageId = eventData.LastStage
        });

        JsonDb.Save();
        await WriteDataAsync(response);
    }
}
