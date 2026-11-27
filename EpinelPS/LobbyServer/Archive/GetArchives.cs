using EpinelPS.Data;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/get")]
public class GetArchives : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        _ = await ReadData<ReqGetArchiveRecord>();

        ResGetArchiveRecord response = new();
        List<ArchiveRecordManagerRecord> records = [.. GameData.Instance.archiveRecordManagerTable.Values];
        List<int> allIds = [.. records.Select(record => record.Id)];

        // This is the archive catalog, not the user's unlock state.
        response.ArchiveRecordManagerList.AddRange(allIds);

        User user = GetUser();
        if (GameConfig.Root.ArchiveUnlockAll == true)
        {
            response.UnlockedArchiveRecordList.AddRange(allIds);
        }
        else
        {
            response.UnlockedArchiveRecordList.AddRange(user.UnlockedArchiveRecordIds);
        }

        List<ArchiveRecordManagerRecord> eventQuestRecords = [.. records
            .Where(record => record.RecordType == ArchiveRecordType.EventQuest)];

        response.ArchiveEventQuest = new();
        if (GameConfig.Root.ArchiveUnlockAll == true)
        {
            response.ArchiveEventQuest.UnlockedArchiveRecordManagerEventQuestIdList
                .AddRange(eventQuestRecords.Select(record => record.Id));
        }
        else
        {
            response.ArchiveEventQuest.UnlockedArchiveRecordManagerEventQuestIdList
                .AddRange(user.UnlockedArchiveEventQuestIds);
        }

        response.ArchiveEventQuest.ActivatedArchiveRecordManagerId = user.ActivatedArchiveEventQuestRecordId;

        // The client builds each episode's quest/stage panes from this mapping:
        // full id lists plus the cleared ("cumulative") and not-yet-cleared
        // ("current") subsets, and a per-stage state.
        foreach (ArchiveRecordManagerRecord record in eventQuestRecords)
        {
            if (!ArchiveEventQuestHelper.TryGetEpisode(record.Id, out _, out var manager))
                continue;

            List<EventQuestRecord> quests = ArchiveEventQuestHelper.GetQuests(manager.Id);
            List<EventQuestStageRecord> stages = ArchiveEventQuestHelper.GetStages(manager.Id);

            var mapping = new ResGetArchiveRecord.Types.ArchiveEventQuestData.Types.EventQuestMappingData
            {
                ArchiveRecordManagerId = record.Id
            };
            mapping.EventQuestIdList.AddRange(quests.Select(q => q.Id));

            foreach (EventQuestStageRecord stage in stages)
            {
                bool cleared = user.ClearedArchiveEventQuestStageIds.Contains(stage.Id);
                var stageData = new ResGetArchiveRecord.Types.ArchiveEventQuestData.Types.EventQuestMappingData.Types.StageData
                {
                    StageId = stage.Id,
                    State = cleared
                        ? ResGetArchiveRecord.Types.ArchiveEventQuestData.Types.EventQuestMappingData.Types.StageData.Types.StageState.Cleared
                        : ResGetArchiveRecord.Types.ArchiveEventQuestData.Types.EventQuestMappingData.Types.StageData.Types.StageState.Entered
                };
                mapping.EventQuestStageList.Add(stageData);
                if (cleared)
                {
                    mapping.CurrentArchiveEventQuestStageList.Add(stageData);
                    mapping.CumulativeArchiveEventQuestStageList.Add(stageData);
                }
            }

            foreach (EventQuestRecord quest in quests)
            {
                if (!user.ClearedArchiveEventQuestIds.Contains(quest.Id))
                    continue;
                // Current = cleared since the last reset; Cumulative = ever
                // cleared. Both are *cleared* sets — the client uses them to
                // find the next quest, so never put uncleared ids here.
                mapping.CurrentArchiveEventQuestIdList.Add(quest.Id);
                mapping.CumulativeArchiveEventQuestIdList.Add(quest.Id);
            }

            response.ArchiveEventQuest.EventQuestMappingList.Add(mapping);
        }

        await WriteDataAsync(response);
    }
}
