using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive;

/// <summary>
/// Archive stage reward rules. The archive spot table leaves
/// FirstClearRewardId = 0 on every row and puts the stage-clear reward in
/// ClearRewardId, so a first clear must pay ClearRewardId (plus
/// FirstClearRewardId when present). Repeats pay ClearRewardId only for stages
/// whose ArchiveEventDungeonStageTable row is repeatable.
/// </summary>
public static class ArchiveStageRewardHelper
{
    public static NetRewardData Grant(User user, int stageId, bool firstClear)
    {
        NetRewardData reward = new();
        if (!GameData.Instance.ArchiveEventDungeonSpotBattleRecords.TryGetValue(stageId, out var spot))
            return reward;

        bool repeatable = GameData.Instance.archiveEventDungeonStageRecords.TryGetValue(stageId, out var stageRow)
            && stageRow.IsRepeatClear;

        List<int> rewardIds = [];
        if (firstClear)
        {
            if (spot.ClearRewardId > 0) rewardIds.Add(spot.ClearRewardId);
            if (spot.FirstClearRewardId > 0) rewardIds.Add(spot.FirstClearRewardId);
        }
        else if (repeatable)
        {
            if (spot.ClearRewardId > 0) rewardIds.Add(spot.ClearRewardId);
        }

        rewardIds = [.. rewardIds.Distinct()];
        return rewardIds.Count == 0 ? reward : RewardUtils.RegisterRewardsForUserDou(user, rewardIds);
    }
}
