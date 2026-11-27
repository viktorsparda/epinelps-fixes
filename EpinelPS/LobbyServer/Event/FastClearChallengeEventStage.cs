using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event;

[GameRequest("/event/challengestage/fastclear")]
public class FastClearChallengeEventStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqFastClearChallengeEventStage req = await ReadData<ReqFastClearChallengeEventStage>();
        User user = GetUser();

        if (!user.EventInfo.TryGetValue(req.EventId, out EventData? eventData))
        {
            eventData = new EventData();
            user.EventInfo[req.EventId] = eventData;
        }
        bool firstClear = !eventData.ClearedStages.Contains(req.StageId);
        if (firstClear) eventData.ClearedStages.Add(req.StageId);
        eventData.LastStage = req.StageId;

        ResFastClearChallengeEventStage response = new()
        {
            Reward = new NetRewardData(),
        };

        if (GameData.Instance.EventDungeonSpotBattleTable.TryGetValue(req.StageId, out EventDungeonSpotBattleRecord? spot))
        {
            int rewardId = firstClear ? spot.FirstClearRewardId : spot.ClearRewardId;
            if (rewardId > 0)
            {
                response.Reward = RewardUtils.RegisterRewardsForUser(user, rewardId);
            }

            if (spot.TicketItemId > 0 && spot.TicketItemCount > 0)
            {
                DbItemData? ticket = user.Items.FirstOrDefault(i => i.ItemType == spot.TicketItemId);
                if (ticket != null && ticket.Count >= spot.TicketItemCount)
                {
                    ticket.Count -= spot.TicketItemCount;
                    response.RemainTicket = ticket.Count;
                    if (ticket.Count <= 0) user.Items.Remove(ticket);
                }
            }
        }

        JsonDb.Save();
        await WriteDataAsync(response);
    }
}
