using EpinelPS.Data;

namespace EpinelPS.LobbyServer.Archive;

/// <summary>
/// Shared lookups for the archived "event quest" episodes (Archive RecordType 1).
/// A record's RecordMainArchiveEventId matches ArchiveEventQuestManagerTable.EventId.
/// </summary>
public static class ArchiveEventQuestHelper
{
    public static bool TryGetEpisode(int recordId, out ArchiveRecordManagerRecord record, out ArchiveEventQuestManagerRecord_Raw manager)
    {
        record = null!;
        manager = null!;
        if (!GameData.Instance.archiveRecordManagerTable.TryGetValue(recordId, out record!))
            return false;
        if (record.RecordType != ArchiveRecordType.EventQuest)
            return false;

        int archiveEventId = record.RecordMainArchiveEventId;
        manager = GameData.Instance.archiveEventQuestManagerTable.Values
            .FirstOrDefault(m => m.EventId == archiveEventId)!;
        return manager != null;
    }

    public static List<EventQuestRecord> GetQuests(int managerId)
    {
        return GameData.Instance.eventQuestTable.Values
            .Where(q => q.EventQuestManagerId == managerId)
            .OrderBy(q => q.Id)
            .ToList();
    }

    public static List<EventQuestStageRecord> GetStages(int managerId)
    {
        return GameData.Instance.eventQuestStageTable.Values
            .Where(s => s.ArchiveEventQuestManagerId == managerId)
            .OrderBy(s => s.Id)
            .ToList();
    }
}
