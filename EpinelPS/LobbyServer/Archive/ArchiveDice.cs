using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive;

public class ArchiveDiceState
{
    public int Position { get; set; } = -1;
    public int Gauge { get; set; }
    public int Chain { get; set; }
    public int GaugeMaxEventCount { get; set; }
}

public static class ArchiveDiceHelper
{
    public const int ArchiveDiceId = 4;
    public const int StartingDice = 20;

    public static EventDiceRecord? Record(int diceId)
    {
        return GameData.Instance.EventDiceTable.TryGetValue(diceId, out EventDiceRecord? r) ? r : null;
    }

    public static void EnsureDice(User user)
    {
        EventDiceRecord? record = Record(ArchiveDiceId);
        if (record == null || record.EventItemId <= 0) return;
        DbItemData? item = user.Items.FirstOrDefault(i => i.ItemType == record.EventItemId);
        if (item == null)
        {
            user.Items.Add(new DbItemData
            {
                ItemType = record.EventItemId,
                Count = StartingDice,
                Csn = 0,
                Level = 0,
                Exp = 0,
                Position = 0,
                Corp = 0,
                Isn = user.GenerateUniqueItemId()
            });
        }
        else if (item.Count <= 0)
        {
            item.Count = StartingDice;
        }
    }

    public static List<EventDiceBoardRecord> Board(int diceId)
    {
        EventDiceRecord? record = Record(diceId);
        if (record == null) return [];
        return [.. GameData.Instance.EventDiceBoardTable.Values
            .Where(b => b.GroupId == record.BoardId)
            .OrderBy(b => b.Id)];
    }

    public static int RewardFor(int diceId, int position)
    {
        EventDiceRecord? record = Record(diceId);
        if (record == null || position < 0) return 0;
        EventDiceRecord? live = GameData.Instance.EventDiceTable.Values
            .FirstOrDefault(r => r.EventId == record.EventId && !r.IsArchive);
        if (live == null) return 0;
        List<EventDiceBoardRecord> archiveBoard = Board(diceId);
        List<EventDiceBoardRecord> liveBoard = [.. GameData.Instance.EventDiceBoardTable.Values
            .Where(b => b.GroupId == live.BoardId).OrderBy(b => b.Id)];
        if (archiveBoard.Count != liveBoard.Count || position >= archiveBoard.Count) return 0;
        return liveBoard[position].RewardId;
    }

    public static int Consume(User user)
    {
        EventDiceRecord? record = Record(ArchiveDiceId);
        if (record == null || record.EventItemId <= 0) return -1;
        DbItemData? item = user.Items.FirstOrDefault(i => i.ItemType == record.EventItemId);
        if (item == null || item.Count < Math.Max(1, record.DiceConsume))
        {
            EnsureDice(user);
            item = user.Items.FirstOrDefault(i => i.ItemType == record.EventItemId);
        }
        if (item == null || item.Count < Math.Max(1, record.DiceConsume)) return -1;
        item.Count = Math.Max(0, item.Count - Math.Max(1, record.DiceConsume));
        return item.Count;
    }
}

[GameRequest("/archive/minigame/dice/get")]
public class GetArchiveDice : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetArchiveDice req = await ReadData<ReqGetArchiveDice>();
        User user = GetUser();
        ArchiveDiceHelper.EnsureDice(user);
        JsonDb.Save();
        var state = ArchiveMiniGameStateStore.Load<ArchiveDiceState>(user, $"dice:{req.DiceMiniGameId}");
        List<EventDiceBoardRecord> board = ArchiveDiceHelper.Board(req.DiceMiniGameId);
        // CurrentBoardId 1 is the board's start sentinel; the first roll moves
        // onto the first tile (position roll-1).
        int currentBoardId = state.Position < 0
            ? 1
            : (board.Count > 0 ? board[Math.Clamp(state.Position, 0, board.Count - 1)].Id : 0);
        ArchiveMiniGameStateStore.Save(user, $"dice:{req.DiceMiniGameId}", state);
        JsonDb.Save();
        ResGetArchiveDice response = new()
        {
            CurrentBoardId = currentBoardId,
            GaugeMaxEventCount = state.GaugeMaxEventCount,
            ChainCount = state.Chain,
            DiceGauge = state.Gauge
        };
        await WriteDataAsync(response);
    }
}

[GameRequest("/archive/minigame/dice/roll")]
public class ArchiveDiceRoll : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqArchiveDiceRoll req = await ReadData<ReqArchiveDiceRoll>();
        User user = GetUser();
        int remaining = ArchiveDiceHelper.Consume(user);
        var state = ArchiveMiniGameStateStore.Load<ArchiveDiceState>(user, $"dice:{req.DiceMiniGameId}");
        List<EventDiceBoardRecord> board = ArchiveDiceHelper.Board(req.DiceMiniGameId);
        int roll = remaining < 0 ? 0 : Random.Shared.Next(1, 7);
        bool wrapped = false;
        if (board.Count > 0 && roll > 0)
        {
            int previous = state.Position;
            if (previous < 0)
            {
                state.Position = roll - 1;
            }
            else
            {
                int stepped = previous + roll;
                wrapped = stepped >= board.Count;
                state.Position = stepped % board.Count;
            }
        }
        EventDiceBoardRecord? tile = board.Count > 0 && state.Position >= 0
            ? board[Math.Clamp(state.Position, 0, board.Count - 1)]
            : null;

        // Black teleport tiles add GoTo extra steps (3/4/6); they are not
        // SlotIds (no SlotId 3/4/6 exists). Move the server-side position by
        // the same extra steps so the next roll continues from the right tile,
        // but keep the original GoTo for the client's extra-step animation.
        int goTo = tile?.GoTo ?? 0;
        if (tile != null && goTo > 0 && board.Count > 0)
        {
            state.Position = (Math.Max(0, state.Position) + goTo) % board.Count;
            tile = board[state.Position];
        }

        // Official Volt Road gauge: passing the start adds BoardFinishGauge
        // (50); reaching MaxGauge (100) pays an exploration reward and resets.
        EventDiceRecord? diceRecord = ArchiveDiceHelper.Record(req.DiceMiniGameId);
        EventDiceRecord? rewardRecord = diceRecord;
        if (diceRecord != null && diceRecord.DiceClearRewardList.All(r => r.ClearRewardId <= 0))
        {
            rewardRecord = GameData.Instance.EventDiceTable.Values
                .FirstOrDefault(r => r.EventId == diceRecord.EventId && !r.IsArchive) ?? diceRecord;
        }
        NetMiniGameDiceReward gaugeReward = new();
        if (wrapped && diceRecord != null)
        {
            state.Gauge += diceRecord.BoardFinishGauge;
            while (state.Gauge >= Math.Max(1, diceRecord.MaxGauge))
            {
                state.Gauge -= diceRecord.MaxGauge;
                state.GaugeMaxEventCount++;
                int clearIndex = rewardRecord == null ? -1 : Math.Min(state.GaugeMaxEventCount - 1, rewardRecord.DiceClearRewardList.Count - 1);
                int clearReward = clearIndex >= 0 ? rewardRecord!.DiceClearRewardList[clearIndex].ClearRewardId : 0;
                if (clearReward <= 0) clearReward = rewardRecord?.ClearRewardIdRepeat ?? 0;
                if (clearReward > 0)
                {
                    gaugeReward.Reward = RewardUtils.RegisterRewardsForUser(user, clearReward);
                    gaugeReward.DiceGauge = new NetDiceGauge();
                }
            }
        }

        state.Chain++;
        ArchiveMiniGameStateStore.Save(user, $"dice:{req.DiceMiniGameId}", state);
        JsonDb.Save();
        int rewardId = ArchiveDiceHelper.RewardFor(req.DiceMiniGameId, state.Position);
        NetMiniGameDiceReward eventReward = new();
        if (rewardId > 0)
        {
            eventReward.Reward = RewardUtils.RegisterRewardsForUser(user, rewardId);
            eventReward.DiceGauge = new NetDiceGauge();
        }

        ResArchiveDiceRoll response = new()
        {
            DiceResult = roll,
            CurrentBoardId = tile?.Id ?? 0,
            GoTo = goTo,
            GaugeMaxEventCount = state.GaugeMaxEventCount,
            ChainCount = state.Chain,
            IsFinish = false,
            GaugeMaxEventResult = GaugeMaxEventResult.None,
            DiceGauge = state.Gauge,
            EventReward = eventReward,
            GaugeMaxEventReward = gaugeReward
        };
        await WriteDataAsync(response);
    }
}
