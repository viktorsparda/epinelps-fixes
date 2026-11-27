using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event;

[GameRequest("/event/boxgacha/execute")]
public class ExecuteEventBoxGacha : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqExecuteEventBoxGacha req = await ReadData<ReqExecuteEventBoxGacha>();
        User user = GetUser();

        // The client always calls NKUserData.UpdateFrom(response.Reward), so every
        // path must return a non-null Reward (including error/empty paths).
        static ResExecuteEventBoxGacha EmptyResponse() => new() { Reward = new NetRewardData() };

        // 1. Find EventBoxGachaRecord by EventId
        EventBoxGachaRecord? boxGacha = GameData.Instance.EventBoxGachaTable.Values.FirstOrDefault(x => x.EventId == req.EventId);
        if (boxGacha == null)
        {
            Logging.WriteLine($"[EventBoxGacha] Unknown event id: {req.EventId}", LogType.Warning);
            await WriteDataAsync(EmptyResponse());
            return;
        }

        if (!user.EventBoxGachaData.TryGetValue(req.EventId, out UserEventBoxGachaData? gachaData))
        {
            gachaData = new UserEventBoxGachaData();
            user.EventBoxGachaData[req.EventId] = gachaData;
        }

        // 2. Check undrawn rewards and box completion before deducting tickets
        HashSet<int> drawnOrders = [.. gachaData.RewardOrders];
        // A rerun box cannot award a costume the user already owns: its slot is
        // treated as claimed, so the box has one reward (and one draw) fewer.
        HashSet<int> ownedCostumeOrders = EventBoxGachaHelper.GetOwnedCostumeOrders(user, boxGacha.GachaRewardGroup);
        List<EventBoxGachaRewardRecord> allRewards = [.. GameData.Instance.EventBoxGachaRewardTable.Values
            .Where(r => r.Group == boxGacha.GachaRewardGroup)
            .OrderBy(r => r.Order)];

        if (allRewards.Count == 0)
        {
            Logging.WriteLine($"[EventBoxGacha] Event {req.EventId} has no configured rewards in GachaRewardGroup {boxGacha.GachaRewardGroup}", LogType.Warning);
            await WriteDataAsync(EmptyResponse());
            return;
        }
        List<EventBoxGachaRewardRecord> availableRewards = [.. allRewards.Where(r => !drawnOrders.Contains(r.Order) && !ownedCostumeOrders.Contains(r.Order))];
        if (availableRewards.Count == 0)
        {
            List<int> resolvedOrders = EventBoxGachaHelper.GetResolvedOrders(gachaData, ownedCostumeOrders);
            Logging.WriteLine($"[EventBoxGacha] Event {req.EventId} box is already completed for user {user.ID} ({resolvedOrders.Count}/{allRewards.Count} rewards claimed)", LogType.Warning);
            ResExecuteEventBoxGacha completed = new() { Reward = new NetRewardData() };
            completed.RewardOrders.AddRange(resolvedOrders);
            await WriteDataAsync(completed);
            return;
        }

        // 3. Server is authoritative for the roll count
        if (req.CurrentCount > 0 && req.CurrentCount != gachaData.GachaCount)
        {
            Logging.WriteLine($"[EventBoxGacha] Desync warning for user {user.ID} in event {req.EventId}: client reported count {req.CurrentCount}, server has {gachaData.GachaCount}", LogType.Warning);
        }

        int currentCount = gachaData.GachaCount + 1;

        // 4. Resolve price from EventBoxGachaPriceTable
        List<EventBoxGachaPriceRecord> priceRecords = [.. GameData.Instance.EventBoxGachaPriceTable.Values
            .Where(p => p.Group == boxGacha.PriceGroup)
            .OrderBy(p => p.Count)];

        if (priceRecords.Count == 0)
        {
            Logging.WriteLine($"[EventBoxGacha] Event {req.EventId} has no price records in PriceGroup {boxGacha.PriceGroup}", LogType.Warning);
            await WriteDataAsync(EmptyResponse());
            return;
        }

        EventBoxGachaPriceRecord? priceRec = priceRecords.FirstOrDefault(p => p.Count == currentCount);
        if (priceRec == null)
        {
            priceRec = priceRecords.Last();
            Logging.WriteLine($"[EventBoxGacha] Roll count {currentCount} exceeded configured prices. Falling back to max price (Count {priceRec.Count}, Cost {priceRec.ItemCount}).", LogType.Warning);
        }

        // 5. Check and deduct tickets
        int ticketId = priceRec.ItemId > 0 ? priceRec.ItemId : boxGacha.EventItemId;
        List<DbItemData> ticketStacks = [.. user.Items.Where(i => i.ItemType == ticketId)];
        int totalTickets = ticketStacks.Sum(i => i.Count);

        if (ticketStacks.Count > 1)
        {
            DbItemData primary = ticketStacks[0];
            primary.Count = totalTickets;
            for (int i = 1; i < ticketStacks.Count; i++)
            {
                user.Items.Remove(ticketStacks[i]);
            }
            ticketStacks = [primary];
        }

        DbItemData? ticketItem = ticketStacks.FirstOrDefault();

        if (priceRec.ItemType == RewardType.Item && priceRec.ItemCount > 0)
        {
            if (totalTickets < priceRec.ItemCount || ticketItem == null)
            {
                Logging.WriteLine($"[EventBoxGacha] User {user.ID} has insufficient tickets for event {req.EventId}. Needed: {priceRec.ItemCount}, Has: {totalTickets}", LogType.Warning);
                await WriteDataAsync(EmptyResponse());
                return;
            }

            ticketItem.Count -= priceRec.ItemCount;
            totalTickets = ticketItem.Count;
            if (ticketItem.Count <= 0)
            {
                user.Items.Remove(ticketItem);
            }
        }
        else
        {
            Logging.WriteLine($"[EventBoxGacha] Roll {currentCount} for user {user.ID} in event {req.EventId} is FREE (Cost: 0).");
        }

        // 6. Resolve probabilities for this roll
        Dictionary<int, int> probRecords = GameData.Instance.EventBoxGachaProbTable.Values
            .Where(p => p.Group == boxGacha.ProbGroup && p.Count == currentCount)
            .ToDictionary(p => p.Order, p => p.Rate);

        if (probRecords.Count == 0)
        {
            int fallbackCount = GameData.Instance.EventBoxGachaProbTable.Values
                .Where(p => p.Group == boxGacha.ProbGroup)
                .Select(p => p.Count)
                .DefaultIfEmpty(0)
                .Max();

            if (fallbackCount > 0)
            {
                probRecords = GameData.Instance.EventBoxGachaProbTable.Values
                    .Where(p => p.Group == boxGacha.ProbGroup && p.Count == fallbackCount)
                    .ToDictionary(p => p.Order, p => p.Rate);
            }
        }

        int totalWeight = 0;
        foreach (EventBoxGachaRewardRecord reward in availableRewards)
        {
            totalWeight += probRecords.TryGetValue(reward.Order, out int rate) ? rate : 100;
        }

        EventBoxGachaRewardRecord winningReward;
        if (totalWeight <= 0)
        {
            winningReward = availableRewards[Rng.Next(0, availableRewards.Count)];
        }
        else
        {
            int roll = Rng.Next(0, totalWeight);
            int cumulative = 0;
            winningReward = availableRewards.Last();
            foreach (EventBoxGachaRewardRecord reward in availableRewards)
            {
                int rate = probRecords.TryGetValue(reward.Order, out int r) ? r : 100;
                cumulative += rate;
                if (roll < cumulative)
                {
                    winningReward = reward;
                    break;
                }
            }
        }

        // 7. Grant the reward
        NetRewardData rewardData = new();
        RewardUtils.AddSingleObject(user, ref rewardData, winningReward.ItemId, winningReward.ItemType, winningReward.ItemCount);

        // 8. Persist state
        gachaData.RewardOrders.Add(winningReward.Order);
        gachaData.GachaCount = currentCount;
        JsonDb.Save();

        // 9. Build response
        ResExecuteEventBoxGacha response = new()
        {
            Reward = rewardData,
            Ticket = new NetUserItemData
            {
                Tid = ticketId,
                Count = totalTickets,
                Isn = ticketItem?.Isn ?? 0,
                Corporation = ticketItem?.Corp ?? 0,
            },
        };
        response.RewardOrders.AddRange(EventBoxGachaHelper.GetResolvedOrders(gachaData, ownedCostumeOrders));

        Logging.WriteLine($"[EventBoxGacha] Roll {currentCount} completed for user {user.ID}. Won Order: {winningReward.Order} (Item {winningReward.ItemId} x{winningReward.ItemCount}). Remaining tickets: {totalTickets}");

        await WriteDataAsync(response);
    }
}
