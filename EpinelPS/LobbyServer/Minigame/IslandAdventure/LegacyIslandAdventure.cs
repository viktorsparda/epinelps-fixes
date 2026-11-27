using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer.Archive;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Minigame.IslandAdventure;

internal static class LegacyIslandAdventureRequest
{
    public static IslandAdventureManagerRecord? Resolve(int eventId)
    {
        return IslandAdventureHelper.ManagerForEvent(eventId, false)
               ?? IslandAdventureHelper.ManagerForEvent(eventId, true)
               ?? IslandAdventureHelper.ManagerForEvent(81401, false)
               ?? IslandAdventureHelper.Manager(1);
    }
}

[GameRequest("/event/minigame/islandadventure/get/currency")]
public class LegacyIslandAdventureGetCurrency : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetMiniGameIslandAdventureCurrency req = await ReadData<ReqGetMiniGameIslandAdventureCurrency>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        ResGetMiniGameIslandAdventureCurrency response = new();
        if (manager != null)
        {
            IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
            response.Currency = state.Currency;
        }
        await WriteDataAsync(response);
    }
}

[GameRequest("/event/minigame/islandadventure/get/inventory")]
public class LegacyIslandAdventureGetInventory : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetMiniGameIslandAdventureInventory req = await ReadData<ReqGetMiniGameIslandAdventureInventory>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        if (manager == null)
        {
            await WriteDataAsync(new ResGetMiniGameIslandAdventureInventory());
            return;
        }
        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.EnsureBaitEquipped(state);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(IslandAdventureHelper.BuildLegacyInventory(state));
    }
}

[GameRequest("/event/minigame/islandadventure/get/fish/collection")]
public class LegacyIslandAdventureGetFishCollection : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqMiniGameIslandAdventureFishCollection req = await ReadData<ReqMiniGameIslandAdventureFishCollection>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        if (manager == null)
        {
            await WriteDataAsync(new ResMiniGameIslandAdventureFishCollection());
            return;
        }
        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(IslandAdventureHelper.BuildLegacyFishCollection(state));
    }
}

[GameRequest("/event/minigame/islandadventure/get/fish/spotcount")]
public class LegacyIslandAdventureGetSpotCount : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqMiniGameIslandAdventureFishingSpotCountHistory req = await ReadData<ReqMiniGameIslandAdventureFishingSpotCountHistory>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        if (manager == null)
        {
            await WriteDataAsync(new ResMiniGameIslandAdventureFishingSpotCountHistory());
            return;
        }
        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(IslandAdventureHelper.BuildLegacySpotHistory(manager, state));
    }
}

[GameRequest("/event/minigame/islandadventure/get/photo/album")]
public class LegacyIslandAdventureGetPhotoAlbum : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqMiniGameIslandAdventurePhotoAlbum req = await ReadData<ReqMiniGameIslandAdventurePhotoAlbum>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        if (manager == null)
        {
            await WriteDataAsync(new ResMiniGameIslandAdventurePhotoAlbum());
            return;
        }
        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(IslandAdventureHelper.BuildLegacyPhotoAlbum(state));
    }
}

[GameRequest("/event/minigame/islandadventure/get/fishing/stepupreward")]
public class LegacyIslandAdventureGetFishingStepUpReward : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetIslandAdventureFishingStepUpRewardStatus req = await ReadData<ReqGetIslandAdventureFishingStepUpRewardStatus>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        ResGetIslandAdventureFishingStepUpRewardStatus response = new();
        if (manager != null)
        {
            IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
            response.FishAccumulatedScore = IslandAdventureHelper.FishScore(manager, state);
            response.FishStepUpRewardedStep.AddRange(state.ClaimedFishSteps.OrderBy(s => s));
        }
        await WriteDataAsync(response);
    }
}

[GameRequest("/event/minigame/islandadventure/get/photo/stepupreward")]
public class LegacyIslandAdventureGetPhotoStepUpReward : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetIslandAdventurePhotoStepUpRewardStatus req = await ReadData<ReqGetIslandAdventurePhotoStepUpRewardStatus>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        ResGetIslandAdventurePhotoStepUpRewardStatus response = new();
        if (manager != null)
        {
            IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
            response.PhotoAccumulatedScore = IslandAdventureHelper.PhotoScore(manager, state);
            response.PhotoStepUpRewardedStep.AddRange(state.ClaimedPhotoSteps.OrderBy(s => s));
        }
        await WriteDataAsync(response);
    }
}

[GameRequest("/event/minigame/islandadventure/list/mission")]
public class LegacyIslandAdventureListMission : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetIslandAdventureMissionProgress req = await ReadData<ReqGetIslandAdventureMissionProgress>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        ResGetIslandAdventureMissionProgress response = new();
        if (manager != null)
        {
            IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
            response.MissionProgressList.AddRange(IslandAdventureHelper.BuildLegacyMissionProgress(manager, state));
        }
        await WriteDataAsync(response);
    }
}

[GameRequest("/event/minigame/islandadventure/update/mission")]
public class LegacyIslandAdventureUpdateMission : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqUpdateIslandAdventureMissionProgress req = await ReadData<ReqUpdateIslandAdventureMissionProgress>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        ResUpdateIslandAdventureMissionProgress response = new();
        if (manager != null)
        {
            IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
            response.MissionProgressList.AddRange(IslandAdventureHelper.BuildLegacyMissionProgress(manager, state));
        }
        await WriteDataAsync(response);
    }
}

[GameRequest("/event/minigame/islandadventure/buy")]
public class LegacyIslandAdventureBuy : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqBuyMiniGameIslandAdventureItem req = await ReadData<ReqBuyMiniGameIslandAdventureItem>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        ResBuyMiniGameIslandAdventureItem response = new();
        if (manager != null)
        {
            IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
            if (GameData.Instance.IslandAdventureShopTable.TryGetValue(req.ShopTableId, out IslandAdventureShopRecord? shop)
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
            }
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
            response.Currency = state.Currency;
            foreach (var item in state.Items.Where(kv => kv.Value > 0))
                response.ListItemId[item.Key] = item.Value;
        }
        await WriteDataAsync(response);
    }
}

[GameRequest("/event/minigame/islandadventure/equip")]
public class LegacyIslandAdventureEquip : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqEquipMiniGameIslandAdventureItem req = await ReadData<ReqEquipMiniGameIslandAdventureItem>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        ResEquipMiniGameIslandAdventureItem response = new();
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

[GameRequest("/event/minigame/islandadventure/unequip/fishingbait")]
public class LegacyIslandAdventureUnequipBait : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqUnequipIslandAdventureFishingBait req = await ReadData<ReqUnequipIslandAdventureFishingBait>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        if (manager != null)
        {
            IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
            state.EquippedBaitId = 0;
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
        }
        await WriteDataAsync(new ResUnequipIslandAdventureFishingBait());
    }
}

[GameRequest("/event/minigame/islandadventure/startfishing")]
public class LegacyIslandAdventureStartFishing : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        await ReadData<ReqStartIslandAdventureFishing>();
        await WriteDataAsync(new ResStartIslandAdventureFishing());
    }
}

[GameRequest("/event/minigame/islandadventure/fish")]
public class LegacyIslandAdventureFish : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqFishingMiniGameIslandAdventure req = await ReadData<ReqFishingMiniGameIslandAdventure>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        if (manager == null)
        {
            await WriteDataAsync(new ResFishingMiniGameIslandAdventure());
            return;
        }
        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        var result = IslandAdventureHelper.ApplyFishing(state, req.FishingSpotId, req.FishData, req.BonusFishData);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        await WriteDataAsync(new ResFishingMiniGameIslandAdventure
        {
            Currency = (int)Math.Min(int.MaxValue, state.Currency),
            BonusFishCurrency = (int)Math.Min(int.MaxValue, result.BonusCurrency),
            LastUsedFishingBaitId = result.UsedBait,
            RemainingBaitCount = result.RemainingBait
        });
    }
}

[GameRequest("/event/minigame/islandadventure/take/photo")]
public class LegacyIslandAdventureTakePhoto : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqTakePhotoMiniGameIslandAdventure req = await ReadData<ReqTakePhotoMiniGameIslandAdventure>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
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
        await WriteDataAsync(new ResTakePhotoMiniGameIslandAdventure());
    }
}

[GameRequest("/event/minigame/islandadventure/complete/mission")]
public class LegacyIslandAdventureCompleteMission : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqCompleteIslandAdventureMission req = await ReadData<ReqCompleteIslandAdventureMission>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        if (manager == null)
        {
            await WriteDataAsync(new ResCompleteIslandAdventureMission());
            return;
        }
        IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
        List<IslandAdventureMissionRecord> completed = [];
        foreach (long missionUid in req.MissionIdList.Distinct())
        {
            int missionId = (int)missionUid;
            if (!GameData.Instance.IslandAdventureMissionTable.TryGetValue(missionId, out var mission)
                || mission.ManagerId != manager.Id
                || state.ReceivedMissions.Contains(missionId))
                continue;
            if (IslandAdventureHelper.MissionProgress(mission, state) < mission.MissionCount) continue;
            state.ReceivedMissions.Add(missionId);
            state.MissionReceivedAt[missionId] = DateTime.UtcNow;
            completed.Add(mission);
        }
        NetRewardData reward = IslandAdventureHelper.ApplyDailyMissionRewards(user, state, completed);
        IslandAdventureHelper.Save(user, manager.Id, state);
        JsonDb.Save();
        ResCompleteIslandAdventureMission response = new() { Reward = reward };
        response.MissionProgressList.AddRange(IslandAdventureHelper.BuildLegacyMissionProgress(manager, state));
        await WriteDataAsync(response);
    }
}

[GameRequest("/event/minigame/islandadventure/obtain/stepupreward")]
public class LegacyIslandAdventureObtainStepUpReward : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqObtainMiniGameIslandAdventureStepUpReward req = await ReadData<ReqObtainMiniGameIslandAdventureStepUpReward>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        if (manager == null)
        {
            await WriteDataAsync(new ResObtainMiniGameIslandAdventureStepUpReward());
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
        await WriteDataAsync(new ResObtainMiniGameIslandAdventureStepUpReward { Reward = reward });
    }
}

[GameRequest("/event/minigame/islandadventure/get/rankings")]
public class LegacyIslandAdventureGetRankings : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetMiniGameIslandAdventureRankings req = await ReadData<ReqGetMiniGameIslandAdventureRankings>();
        User user = GetUser();
        IslandAdventureManagerRecord? manager = LegacyIslandAdventureRequest.Resolve(req.EventId);
        if (manager != null)
        {
            IslandAdventureState state = IslandAdventureHelper.Load(user, manager.Id);
            IslandAdventureHelper.Save(user, manager.Id, state);
            JsonDb.Save();
        }
        ResGetMiniGameIslandAdventureRankings response = new()
        {
            UserGuildScoreRanking = new NetMiniGameIslandAdventureUserScoreRanking(),
            UserGuildSizeRanking = new NetMiniGameIslandAdventureUserSizeRanking()
        };
        await WriteDataAsync(response);
    }
}
