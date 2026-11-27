using EpinelPS.Data;
using EpinelPS.Models;

namespace EpinelPS.LobbyServer.Event;

internal static class EventBoxGachaHelper
{
    /// <summary>
    /// Reward orders that count as already claimed because the costume they award
    /// is already in the commander's wardrobe.
    /// A rerun box gacha cannot award a costume the user already owns: the
    /// official game removes that slot, so the box has one reward (and one draw)
    /// fewer. These orders are reported alongside the drawn ones so the client
    /// renders the costume as obtained and stops offering it.
    /// </summary>
    public static HashSet<int> GetOwnedCostumeOrders(User user, int rewardGroup)
    {
        HashSet<int> orders = [];
        foreach (EventBoxGachaRewardRecord reward in GameData.Instance.EventBoxGachaRewardTable.Values)
        {
            if (reward.Group != rewardGroup) continue;
            if (reward.ItemType != RewardType.CharacterCostume) continue;
            if (user.CostumeList.Contains(reward.ItemId)) orders.Add(reward.Order);
        }
        return orders;
    }

    /// <summary>
    /// Persisted draw order followed by the owned-costume orders that were not
    /// drawn (drawn orders are already in the persisted list).
    /// </summary>
    public static List<int> GetResolvedOrders(UserEventBoxGachaData? gachaData, HashSet<int> ownedCostumeOrders)
    {
        List<int> orders = gachaData == null ? [] : [.. gachaData.RewardOrders];
        foreach (int order in ownedCostumeOrders)
        {
            if (!orders.Contains(order)) orders.Add(order);
        }
        return orders;
    }
}
