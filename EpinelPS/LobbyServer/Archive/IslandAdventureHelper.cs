using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;

namespace EpinelPS.LobbyServer.Archive;

/// <summary>
/// Server-side state and rules for the Island Adventure (Coral Island /
/// "Beauty Full Shot") minigame. The game world, fishing timing and camera
/// mechanics run client-side; this helper owns the authoritative inventory,
/// currency, collections, shop and mission bookkeeping the lobby UI reads.
/// </summary>
public class IslandAdventureState
{
    public bool StarterGranted { get; set; }
    public DateTime CreatedAt { get; set; }
    public long Currency { get; set; }
    public int EquippedRodId { get; set; }
    public int EquippedBaitId { get; set; }
    public Dictionary<int, int> Items { get; set; } = [];
    public Dictionary<int, int> ShopPurchaseCounts { get; set; } = [];
    public Dictionary<int, IslandAdventureFishState> Fish { get; set; } = [];
    public Dictionary<int, IslandAdventureSpotState> Spots { get; set; } = [];
    public List<IslandAdventurePhotoState> Photos { get; set; } = [];
    public Dictionary<int, int> MissionProgress { get; set; } = [];
    public HashSet<int> ReceivedMissions { get; set; } = [];
    public Dictionary<int, DateTime> MissionReceivedAt { get; set; } = [];
    public HashSet<int> ClaimedFishSteps { get; set; } = [];
    public HashSet<int> ClaimedPhotoSteps { get; set; } = [];
    public int FishingCount { get; set; }
    public int FishingAttemptCount { get; set; }
    public int DailyFishingCount { get; set; }
    public int DailyFishingAttemptCount { get; set; }
    public int DailyPhotoCount { get; set; }
    public int DailyBaitUseCount { get; set; }
    public long DailyGoldEarned { get; set; }
    public int LastDailyResetDay { get; set; }
    public int MaxFishSize { get; set; }
    public long FishSizeTotal { get; set; }
    public long GoldEarned { get; set; }
    public int BaitUseCount { get; set; }
    public long DailyPoint { get; set; }
}

public class IslandAdventureFishState
{
    public int FishId { get; set; }
    public int MaxSize { get; set; }
    public int FishingCount { get; set; }
}

public class IslandAdventureSpotState
{
    public int SpotId { get; set; }
    public int CatchCount { get; set; }
    public DateTime? LimitReachTime { get; set; }
}

public class IslandAdventurePhotoState
{
    public int PhotoId { get; set; }
    public DateTime TakenTime { get; set; }
    public int Magnification { get; set; }
    public int TargetObject { get; set; }
    public int Flash { get; set; }
}

public static class IslandAdventureHelper
{
    public const int StarterRodId = 101;
    public const int StarterBaitId = 201;
    public const int StarterBaitCount = 30;

    public static IslandAdventureManagerRecord? Manager(int managerId)
    {
        return GameData.Instance.IslandAdventureManagerTable.TryGetValue(managerId, out var manager)
            ? manager
            : null;
    }

    public static IslandAdventureManagerRecord? ManagerForEvent(int eventId, bool archive)
    {
        return GameData.Instance.IslandAdventureManagerTable.Values
            .Where(m => m.EventId == eventId && m.IsArchiveEvent == archive)
            .OrderBy(m => m.Id)
            .FirstOrDefault();
    }

    public static string Key(int managerId) => $"islandadventure:{managerId}";

    public static IslandAdventureState Load(User user, int managerId)
    {
        var state = ArchiveMiniGameStateStore.Load<IslandAdventureState>(user, Key(managerId));
        EnsureStarterKit(state);
        ApplyDailyReset(user, state, managerId);
        return state;
    }

    /// <summary>Reset only the live event's daily missions; archive achievements persist.</summary>
    private static void ApplyDailyReset(User user, IslandAdventureState state, int managerId)
    {
        int today = user.GetDateDay();
        if (state.LastDailyResetDay == today) return;
        state.LastDailyResetDay = today;
        state.DailyFishingCount = 0;
        state.DailyFishingAttemptCount = 0;
        state.DailyPhotoCount = 0;
        state.DailyBaitUseCount = 0;
        state.DailyGoldEarned = 0;
        state.DailyPoint = 0;

        IslandAdventureManagerRecord? manager = Manager(managerId);
        if (manager == null) return;
        foreach (var mission in GameData.Instance.IslandAdventureMissionTable.Values
                     .Where(m => m.ManagerId == manager.Id
                                 && (m.MissionCategory == EventIslandAdventureMissionCategory.DailyMission
                                     || m.MissionCategory == EventIslandAdventureMissionCategory.DailyPoint)))
        {
            state.ReceivedMissions.Remove(mission.Id);
            state.MissionReceivedAt.Remove(mission.Id);
        }
    }

    public static void Save(User user, int managerId, IslandAdventureState state)
    {
        ArchiveMiniGameStateStore.Save(user, Key(managerId), state);
    }

    /// <summary>
    /// The live event started the player with the first rod and a stack of
    /// bait. The archive player otherwise arrives with an empty inventory and
    /// cannot even open the fishing minigame, so seed the same starting kit.
    /// </summary>
    public static void EnsureStarterKit(IslandAdventureState state)
    {
        if (state.StarterGranted) return;
        state.StarterGranted = true;
        if (state.CreatedAt == default) state.CreatedAt = DateTime.UtcNow;

        foreach (var item in GameData.Instance.IslandAdventureItemTable.Values.Where(i => i.FirstItem))
            AddItem(state, item.Id, 1);

        if (!state.Items.ContainsKey(StarterRodId))
            AddItem(state, StarterRodId, 1);
        if (!state.Items.ContainsKey(StarterBaitId))
            AddItem(state, StarterBaitId, StarterBaitCount);

        if (state.EquippedRodId == 0)
            state.EquippedRodId = StarterRodId;
        if (state.EquippedBaitId == 0)
            state.EquippedBaitId = StarterBaitId;
    }

    public static IslandAdventureItemRecord? Item(int itemId)
    {
        return GameData.Instance.IslandAdventureItemTable.TryGetValue(itemId, out var item) ? item : null;
    }

    public static void AddItem(IslandAdventureState state, int itemId, int count)
    {
        IslandAdventureItemRecord? record = Item(itemId);
        if (record == null || count <= 0) return;

        int current = state.Items.TryGetValue(itemId, out int c) ? c : 0;
        int max = record.MaximumAmount > 0 ? record.MaximumAmount : int.MaxValue;
        state.Items[itemId] = Math.Min(max, current + count);
    }

    public static bool RemoveItem(IslandAdventureState state, int itemId, int count)
    {
        if (!state.Items.TryGetValue(itemId, out int current) || current < count) return false;
        current -= count;
        if (current <= 0) state.Items.Remove(itemId);
        else state.Items[itemId] = current;
        if (current <= 0 && state.EquippedBaitId == itemId && IsBait(itemId)) state.EquippedBaitId = 0;
        return true;
    }

    public static bool IsRod(int itemId) => Item(itemId)?.ItemGroup == IslandAdventureItemGroupType.FishingRod;
    public static bool IsBait(int itemId) => Item(itemId)?.ItemGroup == IslandAdventureItemGroupType.Bait;
    public static bool IsBoatPart(int itemId) => Item(itemId)?.ItemGroup == IslandAdventureItemGroupType.BoatReinforce;

    public static int ItemCount(IslandAdventureState state, int itemId)
        => state.Items.TryGetValue(itemId, out int count) ? count : 0;

    public static int ItemCountByGroup(IslandAdventureState state, IslandAdventureItemGroupType group)
        => state.Items.Where(kv => Item(kv.Key)?.ItemGroup == group).Sum(kv => kv.Value);

    public static void EnsureBaitEquipped(IslandAdventureState state)
    {
        if (state.EquippedBaitId != 0 && ItemCount(state, state.EquippedBaitId) > 0) return;
        state.EquippedBaitId = state.Items
            .Where(kv => IsBait(kv.Key) && kv.Value > 0)
            .OrderBy(kv => Item(kv.Key)?.ItemOrder ?? 0)
            .Select(kv => kv.Key)
            .FirstOrDefault();
    }

    public static int OptionValue(int itemId, IslandAdventureItemOption option)
    {
        IslandAdventureItemRecord? record = Item(itemId);
        if (record == null) return 0;
        int value = 0;
        foreach (var optionData in record.ItemOption)
        {
            if (optionData.ItemOptionGroup == option)
                value = Math.Max(value, optionData.ItemOptionValue);
        }
        return value;
    }

    public static long GoldMultiplier(IslandAdventureState state)
    {
        long bonus = 0;
        if (state.EquippedRodId != 0) bonus += OptionValue(state.EquippedRodId, IslandAdventureItemOption.GainGoldRatio);
        foreach (int itemId in state.Items.Keys.Where(IsBoatPart))
            bonus += OptionValue(itemId, IslandAdventureItemOption.GainGoldRatio);
        return 10000 + bonus;
    }

    public static long BonusFishMultiplier(IslandAdventureState state)
    {
        long bonus = 0;
        if (state.EquippedRodId != 0) bonus += OptionValue(state.EquippedRodId, IslandAdventureItemOption.CastingBonusRatio);
        if (state.EquippedBaitId != 0) bonus += OptionValue(state.EquippedBaitId, IslandAdventureItemOption.CastingBonusRatio);
        foreach (int itemId in state.Items.Keys.Where(IsBoatPart))
            bonus += OptionValue(itemId, IslandAdventureItemOption.CastingBonusRatio);
        return 10000 + bonus;
    }

    public static int FishScore(IslandAdventureManagerRecord manager, IslandAdventureState state)
    {
        int score = 0;
        foreach (int fishId in state.Fish.Keys)
        {
            IslandAdventureFishCollectionRecord? row = GameData.Instance.IslandAdventureFishCollectionTable.Values
                .Where(r => r.GroupId == manager.FishCollectionGroup && r.FishId == fishId)
                .OrderBy(r => r.Id)
                .FirstOrDefault();
            if (row != null) score += row.FishPoint;
        }
        return score;
    }

    public static int PhotoScore(IslandAdventureManagerRecord manager, IslandAdventureState state)
    {
        int score = 0;
        foreach (IslandAdventurePhotoState photo in state.Photos)
        {
            PhotoSpotRecord? spot = GameData.Instance.PhotoSpotTable.Values
                .Where(s => s.GroupId == manager.AlbumDataGroup && s.PhotoId == photo.PhotoId)
                .OrderBy(s => s.Id)
                .FirstOrDefault();
            if (spot == null) continue;
            IslandAdventureAlbumRecord? album = GameData.Instance.IslandAdventureAlbumTable.Values
                .Where(a => a.GroupId == manager.AlbumDataGroup && a.PhotoSpotId == spot.Id)
                .OrderBy(a => a.Id)
                .FirstOrDefault();
            if (album != null) score += album.PhotoPoint;
        }
        return score;
    }

    public static ResArchiveGetMiniGameIslandAdventureInventory BuildInventory(IslandAdventureManagerRecord manager, IslandAdventureState state)
    {
        var response = new ResArchiveGetMiniGameIslandAdventureInventory
        {
            EquipFishingRodId = state.EquippedRodId,
            EquipFishingBaitId = state.EquippedBaitId
        };
        foreach (var item in state.Items.Where(kv => kv.Value > 0))
            response.ListItemId[item.Key] = item.Value;
        return response;
    }

    public static ResArchiveMiniGameIslandAdventureFishCollection BuildFishCollection(IslandAdventureState state)
    {
        var response = new ResArchiveMiniGameIslandAdventureFishCollection();
        foreach (IslandAdventureFishState fish in state.Fish.Values.OrderBy(f => f.FishId))
        {
            response.FishingHistoryList.Add(new NetFishingHistory
            {
                FishId = fish.FishId,
                MaxSize = fish.MaxSize,
                FishingCount = fish.FishingCount
            });
        }
        return response;
    }


    /// <summary>
    /// Extra ids the client may use as its spot dictionary key. The public
    /// FishingSpotStaticInfo.TableId is the row Id, but the generic static-info
    /// interface call used by the failure path can expose the manager/group
    /// id instead; include those aliases so the lookup cannot return null.
    /// </summary>
    private static IEnumerable<int> SyntheticSpotKeys()
    {
        for (int i = 1; i <= 10; i++) yield return i;
        for (int i = 1000; i <= 1010; i++) yield return i;
    }

    public static ResArchiveMiniGameIslandAdventureFishingSpotCountHistory BuildSpotHistory(IslandAdventureManagerRecord manager, IslandAdventureState state)
    {
        var response = new ResArchiveMiniGameIslandAdventureFishingSpotCountHistory();
        foreach (var spot in AllSpots(manager, state))
        {
            var entry = new ResArchiveMiniGameIslandAdventureFishingSpotCountHistory.Types.NetFishingSpotData
            {
                SpotId = spot.SpotId,
                CatchCount = spot.CatchCount
            };
            if (spot.LimitReachTime.HasValue)
                entry.FishingLimitReachTime = Timestamp.FromDateTime(DateTime.SpecifyKind(spot.LimitReachTime.Value, DateTimeKind.Utc));
            response.FishingSpotCountHistory.Add(entry);
        }
        HashSet<int> seen = [.. response.FishingSpotCountHistory.Select(x => x.SpotId)];
        foreach (int id in SyntheticSpotKeys())
        {
            if (seen.Add(id))
                response.FishingSpotCountHistory.Add(new ResArchiveMiniGameIslandAdventureFishingSpotCountHistory.Types.NetFishingSpotData { SpotId = id });
        }
        return response;
    }

    /// <summary>
    /// The client rebuilds its local spot table from this history and looks up
    /// the active spot by id. Returning only visited spots leaves a fresh spot
    /// (or a spot used before the first save) absent and the fishing quit path
    /// dereferences null. Always emit every spot for the manager.
    /// </summary>
    public static IEnumerable<IslandAdventureSpotState> AllSpots(IslandAdventureManagerRecord manager, IslandAdventureState state)
    {
        List<FishingSpotRecord> table = [.. GameData.Instance.FishingSpotTable.Values.OrderBy(s => s.Id)];
        foreach (FishingSpotRecord row in table)
        {
            if (!state.Spots.TryGetValue(row.Id, out IslandAdventureSpotState? spot))
            {
                spot = new IslandAdventureSpotState { SpotId = row.Id };
                state.Spots[row.Id] = spot;
            }
            if (spot.LimitReachTime.HasValue && DateTime.UtcNow >= spot.LimitReachTime.Value)
            {
                spot.CatchCount = 0;
                spot.LimitReachTime = null;
            }
            yield return spot;
        }
    }

    public static ResArchiveMiniGameIslandAdventurePhotoAlbum BuildPhotoAlbum(IslandAdventureState state)
    {
        var response = new ResArchiveMiniGameIslandAdventurePhotoAlbum();
        foreach (IslandAdventurePhotoState photo in state.Photos.OrderBy(p => p.PhotoId))
        {
            response.PhotoList.Add(new NetPhotoData
            {
                PhotoId = photo.PhotoId,
                TakenTime = Timestamp.FromDateTime(DateTime.SpecifyKind(photo.TakenTime, DateTimeKind.Utc)),
                Magnification = photo.Magnification,
                TargetObject = photo.TargetObject,
                Flash = photo.Flash
            });
        }
        return response;
    }

    public static ResGetArchiveIslandAdventureMissionData BuildMissionData(IslandAdventureManagerRecord manager, IslandAdventureState state)
    {
        var response = new ResGetArchiveIslandAdventureMissionData();
        foreach (var mission in GameData.Instance.IslandAdventureMissionTable.Values
                     .Where(m => m.ManagerId == manager.Id)
                     .OrderBy(m => m.Id))
        {
            response.AchievementMissionDataList.Add(new NetArchiveIslandAdventureMissionData
            {
                MissionId = mission.Id,
                Progress = MissionProgress(mission, state),
                IsReceived = state.ReceivedMissions.Contains(mission.Id)
            });
        }
        return response;
    }

    public static int MissionProgress(IslandAdventureMissionRecord mission, IslandAdventureState state)
    {
        bool daily = mission.MissionCategory == EventIslandAdventureMissionCategory.DailyMission
                     || mission.MissionCategory == EventIslandAdventureMissionCategory.DailyPoint;
        int progress = mission.MissionType switch
        {
            EventIslandAdventureMissionType.GainFishingRod => ItemCountByGroup(state, IslandAdventureItemGroupType.FishingRod),
            EventIslandAdventureMissionType.GainFish => mission.MissionTargetId > 0
                ? (state.Fish.ContainsKey(mission.MissionTargetId) ? 1 : 0)
                : (daily ? state.DailyFishingCount : state.FishingCount),
            EventIslandAdventureMissionType.GainPhoto => daily ? state.DailyPhotoCount : state.Photos.Count,
            EventIslandAdventureMissionType.GainGoldCount => (int)Math.Min(int.MaxValue, daily ? state.DailyGoldEarned : state.GoldEarned),
            EventIslandAdventureMissionType.GainBoatParts => ItemCountByGroup(state, IslandAdventureItemGroupType.BoatReinforce),
            EventIslandAdventureMissionType.BaitUseCount => daily ? state.DailyBaitUseCount : state.BaitUseCount,
            EventIslandAdventureMissionType.VoyageTimeCount => 0,
            EventIslandAdventureMissionType.DailyPointReward => (int)Math.Min(int.MaxValue, state.DailyPoint),
            EventIslandAdventureMissionType.FishingCount => daily ? state.DailyFishingAttemptCount : state.FishingAttemptCount,
            EventIslandAdventureMissionType.FishSize => state.MaxFishSize,
            _ => 0
        };
        state.MissionProgress[mission.Id] = progress;
        return progress;
    }

    public static bool IsSpotLimited(FishingSpotRecord spot, IslandAdventureSpotState state)
    {
        if (spot.IsInfinity || state.LimitReachTime == null) return false;
        if (DateTime.UtcNow < state.LimitReachTime.Value) return true;
        state.CatchCount = 0;
        state.LimitReachTime = null;
        return false;
    }

    public static NetRewardData GrantMissionRewards(User user, IslandAdventureState state, IEnumerable<IslandAdventureMissionRecord> missions)
    {
        List<int> rewardIds = [];
        foreach (var mission in missions)
        {
            if (mission.RewardType == EventIslandAdventureMissionRewardType.Item && mission.RewardValue > 0)
                rewardIds.Add(mission.RewardValue);
            else if (mission.RewardType == EventIslandAdventureMissionRewardType.IslandAdventureItem && mission.RewardValue > 0)
                AddItem(state, mission.RewardValue, 1);
            else if (mission.RewardType == EventIslandAdventureMissionRewardType.DailyPoint)
                state.DailyPoint += mission.RewardValue;
        }
        return rewardIds.Count == 0 ? new NetRewardData { PassPoint = new() } : RewardUtils.RegisterRewardsForUserDou(user, rewardIds);
    }

    public static ResArchiveGetIslandAdventureFishingStepUpRewardStatus BuildFishingStepStatus(IslandAdventureManagerRecord manager, IslandAdventureState state)
    {
        var response = new ResArchiveGetIslandAdventureFishingStepUpRewardStatus
        {
            FishAccumulatedScore = FishScore(manager, state)
        };
        response.FishStepUpRewardedStep.AddRange(state.ClaimedFishSteps.OrderBy(s => s));
        return response;
    }

    public static ResArchiveGetIslandAdventurePhotoStepUpRewardStatus BuildPhotoStepStatus(IslandAdventureManagerRecord manager, IslandAdventureState state)
    {
        var response = new ResArchiveGetIslandAdventurePhotoStepUpRewardStatus
        {
            PhotoAccumulatedScore = PhotoScore(manager, state)
        };
        response.PhotoStepUpRewardedStep.AddRange(state.ClaimedPhotoSteps.OrderBy(s => s));
        return response;
    }

    public static ResGetMiniGameIslandAdventureInventory BuildLegacyInventory(IslandAdventureState state)
    {
        var response = new ResGetMiniGameIslandAdventureInventory
        {
            EquipFishingRodId = state.EquippedRodId,
            EquipFishingBaitId = state.EquippedBaitId
        };
        foreach (var item in state.Items.Where(kv => kv.Value > 0))
            response.ListItemId[item.Key] = item.Value;
        return response;
    }

    public static ResMiniGameIslandAdventureFishCollection BuildLegacyFishCollection(IslandAdventureState state)
    {
        var response = new ResMiniGameIslandAdventureFishCollection();
        foreach (IslandAdventureFishState fish in state.Fish.Values.OrderBy(f => f.FishId))
        {
            response.FishingHistoryList.Add(new NetFishingHistory
            {
                FishId = fish.FishId,
                MaxSize = fish.MaxSize,
                FishingCount = fish.FishingCount
            });
        }
        return response;
    }

    public static ResMiniGameIslandAdventureFishingSpotCountHistory BuildLegacySpotHistory(IslandAdventureManagerRecord manager, IslandAdventureState state)
    {
        var response = new ResMiniGameIslandAdventureFishingSpotCountHistory();
        foreach (var spot in AllSpots(manager, state))
        {
            var entry = new ResMiniGameIslandAdventureFishingSpotCountHistory.Types.NetFishingSpotData
            {
                SpotId = spot.SpotId,
                CatchCount = spot.CatchCount
            };
            if (spot.LimitReachTime.HasValue)
                entry.FishingLimitReachTime = Timestamp.FromDateTime(DateTime.SpecifyKind(spot.LimitReachTime.Value, DateTimeKind.Utc));
            response.FishingSpotCountHistory.Add(entry);
        }
        return response;
    }

    public static ResMiniGameIslandAdventurePhotoAlbum BuildLegacyPhotoAlbum(IslandAdventureState state)
    {
        var response = new ResMiniGameIslandAdventurePhotoAlbum();
        foreach (IslandAdventurePhotoState photo in state.Photos.OrderBy(p => p.PhotoId))
        {
            response.PhotoList.Add(new NetPhotoData
            {
                PhotoId = photo.PhotoId,
                TakenTime = Timestamp.FromDateTime(DateTime.SpecifyKind(photo.TakenTime, DateTimeKind.Utc)),
                Magnification = photo.Magnification,
                TargetObject = photo.TargetObject,
                Flash = photo.Flash
            });
        }
        return response;
    }

    public static List<NetIslandAdventureMissionProgress> BuildLegacyMissionProgress(IslandAdventureManagerRecord manager, IslandAdventureState state)
    {
        List<NetIslandAdventureMissionProgress> list = [];
        foreach (var mission in GameData.Instance.IslandAdventureMissionTable.Values
                     .Where(m => m.ManagerId == manager.Id)
                     .OrderBy(m => m.Id))
        {
            Timestamp created = Timestamp.FromDateTime(DateTime.SpecifyKind(
                state.CreatedAt == default ? DateTime.UtcNow : state.CreatedAt, DateTimeKind.Utc));
            var entry = new NetIslandAdventureMissionProgress
            {
                MissionUid = mission.Id,
                MissionTid = mission.Id,
                Progress = MissionProgress(mission, state),
                CreatedAt = created
            };
            if (state.MissionReceivedAt.TryGetValue(mission.Id, out DateTime receivedAt))
                entry.ReceivedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(receivedAt, DateTimeKind.Utc));
            list.Add(entry);
        }
        return list;
    }


    public static (long Currency, long BonusCurrency, int UsedBait, int RemainingBait) ApplyFishing(
        IslandAdventureState state, int spotId, NetFishData? fishData, NetFishData? bonusFishData)
    {
        GameData.Instance.FishingSpotTable.TryGetValue(spotId, out FishingSpotRecord? spot);
        state.Spots.TryGetValue(spotId, out IslandAdventureSpotState? spotState);
        if (spotState == null)
        {
            spotState = new IslandAdventureSpotState { SpotId = spotId };
            state.Spots[spotId] = spotState;
        }
        if (spot != null && IsSpotLimited(spot, spotState))
            return (0, 0, 0, 0);

        state.FishingAttemptCount++;
        state.DailyFishingAttemptCount++;
        EnsureBaitEquipped(state);
        int usedBait = 0;
        int remainingBait = 0;
        if (state.EquippedBaitId != 0 && ItemCount(state, state.EquippedBaitId) > 0)
        {
            usedBait = state.EquippedBaitId;
            RemoveItem(state, usedBait, 1);
            state.BaitUseCount++;
            state.DailyBaitUseCount++;
            remainingBait = ItemCount(state, usedBait);
        }

        long gained = FishCurrency(fishData, GoldMultiplier(state));
        long bonusGained = FishCurrency(bonusFishData, BonusFishMultiplier(state));
        RecordFish(state, fishData);
        RecordFish(state, bonusFishData);
        state.Currency += gained + bonusGained;
        state.GoldEarned += gained + bonusGained;
        state.DailyGoldEarned += gained + bonusGained;

        // A failed cast does not remove a fish from the spot; only a landed
        // fish consumes one of FishingSpotTable.CatchableCount.
        if (spot != null && fishData != null && fishData.FishId > 0)
        {
            spotState.CatchCount++;
            if (!spot.IsInfinity && spotState.CatchCount >= Math.Max(1, spot.CatchableCount))
                spotState.LimitReachTime = DateTime.UtcNow.AddDays(1);
        }
        return (gained, bonusGained, usedBait, remainingBait);
    }

    public static void RecordFish(IslandAdventureState state, NetFishData? fish)
    {
        if (fish == null || fish.FishId <= 0) return;
        if (!GameData.Instance.FishTable.ContainsKey(fish.FishId)) return;
        if (!state.Fish.TryGetValue(fish.FishId, out IslandAdventureFishState? history))
        {
            history = new IslandAdventureFishState { FishId = fish.FishId };
            state.Fish[fish.FishId] = history;
        }
        history.FishingCount++;
        history.MaxSize = Math.Max(history.MaxSize, fish.Size);
        state.FishingCount++;
        state.DailyFishingCount++;
        state.MaxFishSize = Math.Max(state.MaxFishSize, fish.Size);
        state.FishSizeTotal += Math.Max(0, fish.Size);
    }

    public static long FishCurrency(NetFishData? fish, long multiplier)
    {
        if (fish == null || fish.FishId <= 0) return 0;
        if (!GameData.Instance.FishTable.TryGetValue(fish.FishId, out FishRecord? record)) return 0;
        return Math.Max(0, record.CatchPoint * multiplier / 10000);
    }

    public static NetRewardData ApplyDailyMissionRewards(User user, IslandAdventureState state, IEnumerable<IslandAdventureMissionRecord> missions)
    {
        List<int> rewardIds = [];
        foreach (var mission in missions)
        {
            if (mission.RewardType == EventIslandAdventureMissionRewardType.Item && mission.RewardValue > 0)
                rewardIds.Add(mission.RewardValue);
            else if (mission.RewardType == EventIslandAdventureMissionRewardType.IslandAdventureItem && mission.RewardValue > 0)
                AddItem(state, mission.RewardValue, 1);
            else if (mission.RewardType == EventIslandAdventureMissionRewardType.DailyPoint)
                state.DailyPoint += mission.RewardValue;
        }
        return rewardIds.Count == 0 ? new NetRewardData { PassPoint = new() } : RewardUtils.RegisterRewardsForUserDou(user, rewardIds);
    }

}
