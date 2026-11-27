using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;

namespace EpinelPS.LobbyServer.Archive;

internal static class ArchiveIslandAdventureRequest
{
    public static IslandAdventureManagerRecord? Resolve(int managerId)
    {
        return IslandAdventureHelper.Manager(managerId)
               ?? IslandAdventureHelper.ManagerForEvent(81401, true)
               ?? GameData.Instance.IslandAdventureManagerTable.Values
                   .Where(m => m.IsArchiveEvent)
                   .OrderBy(m => m.Id)
                   .FirstOrDefault();
    }
}

[GameRequest("/archive/minigame/islandadventure/getalldata")]
public class ArchiveIslandAdventureGetAllData : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveIslandAdventureGetAllData req = await ReadData<ReqArchiveIslandAdventureGetAllData>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager == null)
        {
            await WriteDataAsync(new ResArchiveIslandAdventureGetAllData());
            return;
        }

        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.EnsureBaitEquipped(state);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();

        ResArchiveIslandAdventureGetAllData response = new()
        {
            InventoryData = IslandAdventureHelper.BuildInventory(manager, state),
            FishingSpotCountData = IslandAdventureHelper.BuildSpotHistory(manager, state),
            CurrencyData = new ResArchiveGetMiniGameIslandAdventureCurrency { Currency = state.Currency },
            MissionData = IslandAdventureHelper.BuildMissionData(manager, state),
            PhotoAlbumData = IslandAdventureHelper.BuildPhotoAlbum(state)
        };
        await WriteDataAsync(response);
    }
}

[GameRequest("/archive/minigame/islandadventure/get/currency")]
public class ArchiveIslandAdventureGetCurrency : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveGetMiniGameIslandAdventureCurrency req = await ReadData<ReqArchiveGetMiniGameIslandAdventureCurrency>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager == null)
        {
            await WriteDataAsync(new ResArchiveGetMiniGameIslandAdventureCurrency());
            return;
        }

        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(new ResArchiveGetMiniGameIslandAdventureCurrency { Currency = state.Currency });
    }
}

[GameRequest("/archive/minigame/islandadventure/get/inventory")]
public class ArchiveIslandAdventureGetInventory : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveGetMiniGameIslandAdventureInventory req = await ReadData<ReqArchiveGetMiniGameIslandAdventureInventory>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager == null)
        {
            await WriteDataAsync(new ResArchiveGetMiniGameIslandAdventureInventory());
            return;
        }

        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.EnsureBaitEquipped(state);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(IslandAdventureHelper.BuildInventory(manager, state));
    }
}

[GameRequest("/archive/minigame/islandadventure/get/fish/collection")]
public class ArchiveIslandAdventureGetFishCollection : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveMiniGameIslandAdventureFishCollection req = await ReadData<ReqArchiveMiniGameIslandAdventureFishCollection>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager == null)
        {
            await WriteDataAsync(new ResArchiveMiniGameIslandAdventureFishCollection());
            return;
        }

        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(IslandAdventureHelper.BuildFishCollection(state));
    }
}

[GameRequest("/archive/minigame/islandadventure/get/fish/spotcount")]
public class ArchiveIslandAdventureGetSpotCount : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveMiniGameIslandAdventureFishingSpotCountHistory req = await ReadData<ReqArchiveMiniGameIslandAdventureFishingSpotCountHistory>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager == null)
        {
            await WriteDataAsync(new ResArchiveMiniGameIslandAdventureFishingSpotCountHistory());
            return;
        }

        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(IslandAdventureHelper.BuildSpotHistory(manager, state));
    }
}

[GameRequest("/archive/minigame/islandadventure/get/photo/album")]
public class ArchiveIslandAdventureGetPhotoAlbum : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveMiniGameIslandAdventurePhotoAlbum req = await ReadData<ReqArchiveMiniGameIslandAdventurePhotoAlbum>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager == null)
        {
            await WriteDataAsync(new ResArchiveMiniGameIslandAdventurePhotoAlbum());
            return;
        }

        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(IslandAdventureHelper.BuildPhotoAlbum(state));
    }
}

[GameRequest("/archive/minigame/islandadventure/get/fishing/stepupreward")]
public class ArchiveIslandAdventureGetFishingStepUpReward : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveGetIslandAdventureFishingStepUpRewardStatus req = await ReadData<ReqArchiveGetIslandAdventureFishingStepUpRewardStatus>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager == null)
        {
            await WriteDataAsync(new ResArchiveGetIslandAdventureFishingStepUpRewardStatus());
            return;
        }

        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(IslandAdventureHelper.BuildFishingStepStatus(manager, state));
    }
}

[GameRequest("/archive/minigame/islandadventure/get/photo/stepupreward")]
public class ArchiveIslandAdventureGetPhotoStepUpReward : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveGetIslandAdventurePhotoStepUpRewardStatus req = await ReadData<ReqArchiveGetIslandAdventurePhotoStepUpRewardStatus>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager == null)
        {
            await WriteDataAsync(new ResArchiveGetIslandAdventurePhotoStepUpRewardStatus());
            return;
        }

        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(IslandAdventureHelper.BuildPhotoStepStatus(manager, state));
    }
}

[GameRequest("/archive/minigame/islandadventure/list/mission")]
public class ArchiveIslandAdventureListMission : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetArchiveIslandAdventureMissionData req = await ReadData<ReqGetArchiveIslandAdventureMissionData>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager == null)
        {
            await WriteDataAsync(new ResGetArchiveIslandAdventureMissionData());
            return;
        }

        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(IslandAdventureHelper.BuildMissionData(manager, state));
    }
}

[GameRequest("/archive/minigame/islandadventure/buy")]
public class ArchiveIslandAdventureBuy : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveBuyMiniGameIslandAdventureItem req = await ReadData<ReqArchiveBuyMiniGameIslandAdventureItem>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        IslandAdventureState state = manager == null
            ? new IslandAdventureState()
            : IslandAdventureHelper.Load(user, manager.Id);

        ResArchiveBuyMiniGameIslandAdventureItem response = new();
        if (manager != null
            && GameData.Instance.IslandAdventureShopTable.TryGetValue(req.ShopTableId, out IslandAdventureShopRecord? shop)
            && shop.GroupId == manager.ShopDataGroup)
        {
            int buyCount = Math.Max(1, req.BuyCount);
            state.ShopPurchaseCounts.TryGetValue(shop.Id, out int purchased);
            bool conditionMet = shop.OpenConditionType switch
            {
                IslandAdventureItemOpenType.GainItem => IslandAdventureHelper.ItemCount(state, shop.OpenConditionValue) > 0,
                _ => shop.IsOpened
            };
            bool canBuy = conditionMet && (shop.RepeatPurchase || purchased == 0);
            long price = (long)shop.ItemPrice * buyCount;
            if (canBuy && state.Currency >= price)
            {
                state.Currency -= price;
                IslandAdventureHelper.AddItem(state, shop.ItemId, shop.RepeatPurchase ? buyCount : 1);
                state.ShopPurchaseCounts[shop.Id] = purchased + (shop.RepeatPurchase ? buyCount : 1);
            }
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
        }

        response.Currency = state.Currency;
        foreach (var item in state.Items.Where(kv => kv.Value > 0))
            response.ListItemId[item.Key] = item.Value;
        await WriteDataAsync(response);
    }
}

[GameRequest("/archive/minigame/islandadventure/equip")]
public class ArchiveIslandAdventureEquip : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveEquipMiniGameIslandAdventureItem req = await ReadData<ReqArchiveEquipMiniGameIslandAdventureItem>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        ResArchiveEquipMiniGameIslandAdventureItem response = new();
        if (manager != null)
        {
            IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
            if (IslandAdventureHelper.ItemCount(state, req.ItemTableId) > 0)
            {
                if (IslandAdventureHelper.IsRod(req.ItemTableId)) state.EquippedRodId = req.ItemTableId;
                else if (IslandAdventureHelper.IsBait(req.ItemTableId)) state.EquippedBaitId = req.ItemTableId;
            }
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
            response.FishingRodId = state.EquippedRodId;
            response.FishingBaitId = state.EquippedBaitId;
            foreach (var item in state.Items.Where(kv => kv.Value > 0))
                response.ListItemId[item.Key] = item.Value;
        }
        await WriteDataAsync(response);
    }
}

[GameRequest("/archive/minigame/islandadventure/unequip/fishingbait")]
public class ArchiveIslandAdventureUnequipBait : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveUnequipIslandAdventureFishingBait req = await ReadData<ReqArchiveUnequipIslandAdventureFishingBait>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager != null)
        {
            IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
            state.EquippedBaitId = 0;
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
        }
        await WriteDataAsync(new ResArchiveUnequipIslandAdventureFishingBait());
    }
}

[GameRequest("/archive/minigame/islandadventure/fish")]
public class ArchiveIslandAdventureFish : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveFishingMiniGameIslandAdventure req = await ReadData<ReqArchiveFishingMiniGameIslandAdventure>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager == null)
        {
            await WriteDataAsync(new ResArchiveFishingMiniGameIslandAdventure());
            return;
        }

        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        var result = IslandAdventureHelper.ApplyFishing(state, req.FishingSpotId, req.FishData, req.BonusFishData);

        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();

        ResArchiveFishingMiniGameIslandAdventure response = new()
        {
            Currency = (int)Math.Min(int.MaxValue, state.Currency),
            BonusFishCurrency = (int)Math.Min(int.MaxValue, result.BonusCurrency),
            LastUsedFishingBaitId = result.UsedBait,
            RemainingBaitCount = result.RemainingBait
        };
        await WriteDataAsync(response);
    }

}

[GameRequest("/archive/minigame/islandadventure/take/photo")]
public class ArchiveIslandAdventureTakePhoto : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveTakePhotoMiniGameIslandAdventure req = await ReadData<ReqArchiveTakePhotoMiniGameIslandAdventure>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager != null && req.Photo != null && req.Photo.PhotoId > 0
            && GameData.Instance.PhotoTable.ContainsKey(req.Photo.PhotoId))
        {
            IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
            IslandAdventurePhotoState? existing = state.Photos.FirstOrDefault(p => p.PhotoId == req.Photo.PhotoId);
            if (existing == null)
            {
                existing = new IslandAdventurePhotoState { PhotoId = req.Photo.PhotoId };
                state.Photos.Add(existing);
                state.DailyPhotoCount++;
            }
            existing.TakenTime = DateTime.UtcNow;
            existing.Magnification = req.Photo.Magnification;
            existing.TargetObject = req.Photo.TargetObject;
            existing.Flash = req.Photo.Flash;
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
        }
        await WriteDataAsync(new ResArchiveTakePhotoMiniGameIslandAdventure());
    }
}

[GameRequest("/archive/minigame/islandadventure/complete/mission")]
public class ArchiveIslandAdventureCompleteMission : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveCompleteIslandAdventureMission req = await ReadData<ReqArchiveCompleteIslandAdventureMission>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager == null)
        {
            await WriteDataAsync(new ResArchiveCompleteIslandAdventureMission());
            return;
        }

        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        List<IslandAdventureMissionRecord> completed = [];
        foreach (int missionId in req.MissionIdList.Distinct())
        {
            if (!GameData.Instance.IslandAdventureMissionTable.TryGetValue(missionId, out var mission)
                || mission.ManagerId != manager.Id
                || state.ReceivedMissions.Contains(missionId))
                continue;
            if (IslandAdventureHelper.MissionProgress(mission, state) < mission.MissionCount) continue;
            state.ReceivedMissions.Add(missionId);
            state.MissionReceivedAt[missionId] = DateTime.UtcNow;
            completed.Add(mission);
        }

        NetRewardData reward = IslandAdventureHelper.GrantMissionRewards(user, state, completed);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();

        ResArchiveCompleteIslandAdventureMission response = new()
        {
            Reward = reward
        };
        response.MissionProgressList.AddRange(IslandAdventureHelper.BuildMissionData(manager, state).AchievementMissionDataList);
        await WriteDataAsync(response);
    }
}

[GameRequest("/archive/minigame/islandadventure/obtain/stepupreward")]
public class ArchiveIslandAdventureObtainStepUpReward : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveObtainMiniGameIslandAdventureStepUpReward req = await ReadData<ReqArchiveObtainMiniGameIslandAdventureStepUpReward>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager == null)
        {
            await WriteDataAsync(new ResArchiveObtainMiniGameIslandAdventureStepUpReward());
            return;
        }

        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        int groupId = req.GameType == IslandAdventureGameType.Photo
            ? manager.PhotoStepUpRewardGroupId
            : manager.FishStepUpRewardGroupId;
        int score = req.GameType == IslandAdventureGameType.Photo
            ? IslandAdventureHelper.PhotoScore(manager, state)
            : IslandAdventureHelper.FishScore(manager, state);
        HashSet<int> claimed = req.GameType == IslandAdventureGameType.Photo
            ? state.ClaimedPhotoSteps
            : state.ClaimedFishSteps;

        IslandAdventureStepUpRewardRecord? row = GameData.Instance.IslandAdventureStepUpRewardTable.Values
            .Where(r => r.GroupId == groupId && r.Step == req.Step)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
        NetRewardData reward = new() { PassPoint = new() };
        if (row != null && score >= row.TargetPoint && !claimed.Contains(req.Step) && row.RewardId > 0)
        {
            reward = RewardUtils.RegisterRewardsForUser(user, row.RewardId);
            claimed.Add(req.Step);
        }

        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(new ResArchiveObtainMiniGameIslandAdventureStepUpReward { Reward = reward });
    }
}

[GameRequest("/archive/minigame/islandadventure/get/rankings")]
public class ArchiveIslandAdventureGetRankings : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveGetMiniGameIslandAdventureRankings req = await ReadData<ReqArchiveGetMiniGameIslandAdventureRankings>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = ArchiveIslandAdventureRequest.Resolve(req.IslandAdventureManagerId);
        if (manager != null)
        {
            IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
        }

        ResArchiveGetMiniGameIslandAdventureRankings response = new()
        {
            UserGuildScoreRanking = new NetMiniGameIslandAdventureUserScoreRanking(),
            UserGuildSizeRanking = new NetMiniGameIslandAdventureUserSizeRanking()
        };
        await WriteDataAsync(response);
    }
}
