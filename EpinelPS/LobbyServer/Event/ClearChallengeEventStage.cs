using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event;

[GameRequest("/event/challengestage/clear")]
public class ClearChallengeEventStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqClearChallengeEventStage req = await ReadData<ReqClearChallengeEventStage>();
        User user = GetUser();

        if (!user.EventInfo.TryGetValue(req.EventId, out EventData? eventData))
        {
            eventData = new EventData();
            user.EventInfo[req.EventId] = eventData;
        }

        bool firstClear = req.BattleResult == 1 && !eventData.ClearedStages.Contains(req.StageId);
        if (req.BattleResult == 1)
        {
            if (firstClear) eventData.ClearedStages.Add(req.StageId);
            eventData.LastStage = req.StageId;
        }

        ResClearChallengeEventStage response = new()
        {
            Reward = new NetRewardData(),
            FirstClearReward = new NetRewardData(),
        };

        if (req.BattleResult == 1 &&
            GameData.Instance.EventDungeonSpotBattleTable.TryGetValue(req.StageId, out EventDungeonSpotBattleRecord? spot))
        {
            int rewardId = firstClear ? spot.FirstClearRewardId : spot.ClearRewardId;
            if (rewardId > 0)
            {
                NetRewardData reward = RewardUtils.RegisterRewardsForUser(user, rewardId);
                if (firstClear) response.FirstClearReward = reward;
                else response.Reward = reward;
            }

            // Challenge tickets are ordinary items (e.g. 7110123). Consume one
            // when present.
            int remainTicket = 0;
            if (spot.TicketItemId > 0 && spot.TicketItemCount > 0)
            {
                DbItemData? ticket = user.Items.FirstOrDefault(i => i.ItemType == spot.TicketItemId);
                if (ticket != null && ticket.Count >= spot.TicketItemCount)
                {
                    ticket.Count -= spot.TicketItemCount;
                    remainTicket = ticket.Count;
                    if (ticket.Count <= 0) user.Items.Remove(ticket);
                }
            }
            response.RemainTicket = remainTicket;
        }
        else
        {
            response.RemainTicket = 0;
        }

        JsonDb.Save();
        await WriteDataAsync(response);
    }
}
