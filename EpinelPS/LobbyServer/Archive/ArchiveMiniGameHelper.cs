using EpinelPS.Data;
using EpinelPS.Database;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EpinelPS.LobbyServer.Archive;

/// <summary>
/// Builds/merges the archive minigame JSON used by /archive/minigame/{get,set}data.
/// The only archive minigame the current client drives through this generic
/// endpoint is NKS: it loads NksArchiveSerializable (mission lists + out-game
/// state) and calls /arcade/nksv2/* for the actual run progress.
/// </summary>
public static class ArchiveMiniGameHelper
{
    public static string DefaultNksOutGameState(EventNKSCommonSettingsRecord? manager = null)
    {
        int characterId = 0;
        int stageId = 0;
        if (manager != null)
        {
            List<EventNKSCharacterRecord> chars = GameData.Instance.EventNKSCharacterTable.Values
                .Where(c => c.CommonSettingsID == manager.Id)
                .OrderBy(c => c.Id)
                .ToList();
            characterId = (chars.FirstOrDefault(c => c.ReqStgClear == 0) ?? chars.FirstOrDefault())?.Id ?? 0;

            List<EventNKSStageRecord> stages = GameData.Instance.EventNKSStageTable.Values
                .Where(s => s.CommonSettingsID == manager.Id)
                .OrderBy(s => s.Id)
                .ToList();
            stageId = (stages.FirstOrDefault(s => s.StageOpenCondition == 0) ?? stages.FirstOrDefault())?.Id ?? 0;
        }

        JArray boughtCharacters = [];
        if (characterId > 0) boughtCharacters.Add(characterId);

        return new JObject
        {
            ["AllowReenter"] = false,
            ["SelectedCharacterId"] = characterId,
            ["SelectedStageId"] = stageId,
            ["BurstDoubleTouchSetting"] = 0,
            ["BurstButtonPositionSetting"] = 0,
            ["Currency"] = 0,
            ["BoughtCharacters"] = boughtCharacters,
            ["GlobalStats"] = new JArray(),
            ["LockedGlobalStatSlots"] = new JArray(),
            ["BoughtSkills"] = new JArray(),
            ["ObtainedItems"] = new JArray(),
            ["ClearedStages"] = new JArray(),
            ["Collections"] = new JArray(),
            ["PlayedDialogs"] = new JArray(),
            ["CurrentlyWorkingTimeLineTableIds"] = new JArray(),
            ["SaveData"] = JValue.CreateNull()
        }.ToString(Formatting.None);
    }

    /// <summary>
    /// Resolve the NKS settings row the archive UI uses for an event. The
    /// archive row shares GroupId with the current engine row; the highest
    /// VersionType in that group is the engine the client actually launched
    /// (e.g. event 80803 -> group 200 -> settings id 3).
    /// </summary>
    public static EventNKSCommonSettingsRecord? ResolveManager(int eventId)
    {
        List<EventNKSCommonSettingsRecord> rows = GameData.Instance.EventNKSCommonSettingsTable.Values
            .Where(r => r.EventId == eventId)
            .ToList();
        if (rows.Count == 0) return null;

        // The archive UI/controller uses the archive settings row (e.g. 80803
        // -> manager 2, stage 101, missions 1101xxx). The newer engine row 3
        // is only what the client sends as nKsId for the v2 API.
        return rows.FirstOrDefault(r => r.IsArchiveEvent) ??
               rows.OrderBy(r => r.VersionType).First();
    }

    public static Nksv2Data EnsureNksData(User user, EventNKSCommonSettingsRecord manager)
    {
        if (user.Nksv2Datas.TryGetValue(manager.Id, out Nksv2Data? data))
            return data;

        data = new Nksv2Data();
        foreach (EventNKSMissionRecord mission in GameData.Instance.EventNKSMissionTable.Values
                     .Where(m => m.CommonSettingsID == manager.Id)
                     .OrderBy(m => m.Id))
        {
            data.MissionProgressData[mission.Id] = new MiniGameNKSV2MissionProgress
            {
                NKsMissionId = mission.Id,
                CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
                Progress = 0,
                Seq = (long)User.GenerateMsn()
            };
        }
        user.Nksv2Datas[manager.Id] = data;
        return data;
    }

    /// <summary>Build a schema-complete NksArchiveSerializable JSON document.</summary>
    public static string BuildState(User user, EventNKSCommonSettingsRecord manager)
    {
        Nksv2Data data = EnsureNksData(user, manager);
        List<EventNKSMissionRecord> missions = GameData.Instance.EventNKSMissionTable.Values
            .Where(m => m.CommonSettingsID == manager.Id)
            .OrderBy(m => m.Id)
            .ToList();

        JArray ids = [];
        JArray sequences = [];
        JArray progresses = [];
        JArray received = [];

        foreach (EventNKSMissionRecord mission in missions)
        {
            ids.Add(mission.Id);
            if (data.MissionProgressData.TryGetValue(mission.Id, out MiniGameNKSV2MissionProgress? progress))
            {
                sequences.Add(progress.Seq);
                progresses.Add(progress.Progress);
                received.Add(progress.ReceivedAt != null);
            }
            else
            {
                sequences.Add(0);
                progresses.Add(0);
                received.Add(false);
            }
        }

        JObject defaults = JObject.Parse(DefaultNksOutGameState(manager));
        JObject outGame;
        try
        {
            JToken parsed = string.IsNullOrWhiteSpace(data.ProgressJson)
                ? JValue.CreateNull()
                : JToken.Parse(data.ProgressJson);
            outGame = parsed as JObject ?? (JObject)defaults.DeepClone();
        }
        catch
        {
            outGame = (JObject)defaults.DeepClone();
        }

        // Repair old/stored documents that point at another NKS manager (the
        // first broken build used manager 2's stage 101 / missions 1101xxx) or
        // at "NotSet" 0. The lobby and stage select look these up in the
        // static tables, so they must belong to this manager.
        HashSet<int> validCharacters = [.. GameData.Instance.EventNKSCharacterTable.Values
            .Where(c => c.CommonSettingsID == manager.Id).Select(c => c.Id)];
        HashSet<int> validStages = [.. GameData.Instance.EventNKSStageTable.Values
            .Where(s => s.CommonSettingsID == manager.Id).Select(s => s.Id)];
        int defaultCharacterId = defaults.Value<int>("SelectedCharacterId");
        int defaultStageId = defaults.Value<int>("SelectedStageId");

        int selectedCharacterId = outGame.Value<int?>("SelectedCharacterId") ?? 0;
        if (!validCharacters.Contains(selectedCharacterId))
            selectedCharacterId = defaultCharacterId;
        int selectedStageId = outGame.Value<int?>("SelectedStageId") ?? 0;
        if (!validStages.Contains(selectedStageId))
            selectedStageId = defaultStageId;
        outGame["SelectedCharacterId"] = selectedCharacterId;
        outGame["SelectedStageId"] = selectedStageId;

        JArray boughtCharacters = outGame["BoughtCharacters"] as JArray ?? [];
        List<int> keptCharacters = [.. boughtCharacters.Values<int>().Where(validCharacters.Contains).Distinct()];
        if (selectedCharacterId > 0 && !keptCharacters.Contains(selectedCharacterId))
            keptCharacters.Insert(0, selectedCharacterId);
        outGame["BoughtCharacters"] = new JArray(keptCharacters);

        if (outGame["ClearedStages"] is JArray clearedStages)
        {
            List<int> keptStages = [.. clearedStages.Values<int>().Where(validStages.Contains).Distinct()];
            outGame["ClearedStages"] = new JArray(keptStages);
        }

        data.ProgressJson = outGame.ToString(Formatting.None);

        var root = new JObject
        {
            ["NksArchiveMissionSerializable"] = new JObject
            {
                ["EventMissionTableIds"] = ids,
                ["EventMissionSequences"] = sequences,
                ["EventMissionProgresses"] = progresses,
                ["EventMissionIsReceived"] = received
            },
            ["NksMiniOutGameStateSerializable"] = outGame
        };
        return root.ToString(Formatting.None);
    }

    /// <summary>Merge a saved archive document back into the NKS v2 save.</summary>
    public static void ApplyState(User user, EventNKSCommonSettingsRecord manager, string json)
    {
        JObject root;
        try { root = JObject.Parse(json); }
        catch { return; }

        Nksv2Data data = EnsureNksData(user, manager);

        if (root["NksMiniOutGameStateSerializable"] is JObject outGame)
            data.ProgressJson = outGame.ToString(Formatting.None);

        if (root["NksArchiveMissionSerializable"] is not JObject mission)
            return;

        List<long>? ids = mission["EventMissionTableIds"]?.ToObject<List<long>>();
        List<long>? sequences = mission["EventMissionSequences"]?.ToObject<List<long>>();
        List<long>? progresses = mission["EventMissionProgresses"]?.ToObject<List<long>>();
        List<bool>? received = mission["EventMissionIsReceived"]?.ToObject<List<bool>>();
        if (ids == null || progresses == null) return;

        for (int i = 0; i < ids.Count; i++)
        {
            int id = (int)ids[i];
            if (!data.MissionProgressData.TryGetValue(id, out MiniGameNKSV2MissionProgress? progress))
                continue;
            if (progresses.Count > i)
                progress.Progress = (int)progresses[i];
            if (sequences != null && sequences.Count > i && sequences[i] > 0)
                progress.Seq = sequences[i];
            if (received != null && received.Count > i && received[i] && progress.ReceivedAt == null)
                progress.ReceivedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        }
    }
}
