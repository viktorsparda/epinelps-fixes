using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/event-quest/acquire")]
public class AcquireArchiveEventQuestReward : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqAcquireArchiveEventQuestReward req = await ReadData<ReqAcquireArchiveEventQuestReward>();
        User user = GetUser();
        ResAcquireArchiveEventQuestReward response = new()
        {
            Reward = new NetRewardData()
        };

        if (ArchiveEventQuestHelper.TryGetEpisode(req.ArchiveRecordManagerId, out var record, out var manager))
        {
            List<EventQuestRecord> quests = ArchiveEventQuestHelper.GetQuests(manager.Id);
            List<EventQuestStageRecord> stages = ArchiveEventQuestHelper.GetStages(manager.Id);
            List<int> rewardIds = [];

            foreach (EventQuestRecord quest in quests)
            {
                if (!user.ClearedArchiveEventQuestIds.Contains(quest.Id) ||
                    user.ClaimedArchiveEventQuestRewardIds.Contains(quest.Id))
                    continue;
                if (quest.RewardId > 0) rewardIds.Add(quest.RewardId);
                user.ClaimedArchiveEventQuestRewardIds.Add(quest.Id);
            }

            foreach (EventQuestStageRecord stage in stages)
            {
                if (!user.ClearedArchiveEventQuestStageIds.Contains(stage.Id) ||
                    user.ClaimedArchiveEventQuestStageRewardIds.Contains(stage.Id))
                    continue;
                if (stage.RewardId > 0) rewardIds.Add(stage.RewardId);
                user.ClaimedArchiveEventQuestStageRewardIds.Add(stage.Id);
            }

            HashSet<int> cleared = [.. user.ClearedArchiveEventQuestIds];
            bool allQuestsCleared = quests.Count > 0 && quests.All(q => cleared.Contains(q.Id));
            if (allQuestsCleared && record.EventQuestClearRewardId > 0 &&
                !user.ClaimedArchiveEventQuestRecordRewardIds.Contains(record.Id))
            {
                rewardIds.Add(record.EventQuestClearRewardId);
                user.ClaimedArchiveEventQuestRecordRewardIds.Add(record.Id);
            }

            if (rewardIds.Count > 0)
                response.Reward = RewardUtils.RegisterRewardsForUserDou(user, rewardIds);

            JsonDb.Save();
        }

        await WriteDataAsync(response);
    }
}
