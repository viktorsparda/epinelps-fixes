namespace EpinelPS.Models;

/// <summary>
/// Persisted state for an archive field map. Archive fields use the same wire
/// types as event fields, but have a separate save slot per map id.
/// </summary>
public class ArchiveFieldInfo
{
    public string Json { get; set; } = "";
    public List<int> UserArchiveProgressIdList { get; set; } = [];
    public List<NetFieldStageData> CompletedStages { get; set; } = [];
    public List<NetFieldObject> CompletedObjects { get; set; } = [];
    public List<NetNonResettableFieldObject> NonResettableFieldObjects { get; set; } = [];
    public List<NetCampaignTeamPosition> TeamPositions { get; set; } = [];
    public List<int> AcquiredFieldPasswordIdList { get; set; } = [];
    public List<int> UnlockedFieldPasswordDoorIdList { get; set; } = [];
    public List<int> ViewedEventFieldNoticePopupTableIds { get; set; } = [];
}
