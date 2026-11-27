using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/minigame/storydungeon/enterplaysoda")]
public class EnterArchivePlaySodaStoryStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        _ = await ReadData<ReqEnterArchivePlaySodaStoryStage>();
        await WriteDataAsync(new ResEnterArchivePlaySodaStoryStage());
    }
}

[GameRequest("/archive/minigame/storydungeon/clearrunning")]
public class ClearArchivePlaySodaRunning : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqClearArchivePlaySodaRunningStoryStage req = await ReadData<ReqClearArchivePlaySodaRunningStoryStage>();
        User user = GetUser();
        int stage = req.ArchiveEventDungeonStageTableId;
        bool first = !user.EventInfo.TryGetValue(req.ArchiveEventId, out var e) || !e.ClearedStages.Contains(stage);
        if (!user.EventInfo.TryGetValue(req.ArchiveEventId, out e)) { e = new EventData(); user.EventInfo[req.ArchiveEventId] = e; }
        if (!e.ClearedStages.Contains(stage)) { e.ClearedStages.Add(stage); e.LastStage = stage; }
        var reward = ArchiveStageRewardHelper.Grant(user, stage, first);
        JsonDb.Save();
        await WriteDataAsync(new ResClearArchivePlaySodaRunningStoryStage());
    }
}

[GameRequest("/archive/minigame/storydungeon/clearstackup")]
public class ClearArchivePlaySodaStackUp : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqClearArchivePlaySodaStackUpStoryStage req = await ReadData<ReqClearArchivePlaySodaStackUpStoryStage>();
        User user = GetUser();
        int stage = req.ArchiveEventDungeonStageTableId;
        bool first = !user.EventInfo.TryGetValue(req.ArchiveEventId, out var e) || !e.ClearedStages.Contains(stage);
        if (!user.EventInfo.TryGetValue(req.ArchiveEventId, out e)) { e = new EventData(); user.EventInfo[req.ArchiveEventId] = e; }
        if (!e.ClearedStages.Contains(stage)) { e.ClearedStages.Add(stage); e.LastStage = stage; }
        ArchiveStageRewardHelper.Grant(user, stage, first);
        JsonDb.Save();
        await WriteDataAsync(new ResClearArchivePlaySodaStackUpStoryStage());
    }
}

[GameRequest("/archive/minigame/storydungeon/clearcatchcoin")]
public class ClearArchivePlaySodaCatchCoin : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqClearArchivePlaySodaCatchCoinStoryStage req = await ReadData<ReqClearArchivePlaySodaCatchCoinStoryStage>();
        User user = GetUser();
        int stage = req.ArchiveEventDungeonStageTableId;
        bool first = !user.EventInfo.TryGetValue(req.ArchiveEventId, out var e) || !e.ClearedStages.Contains(stage);
        if (!user.EventInfo.TryGetValue(req.ArchiveEventId, out e)) { e = new EventData(); user.EventInfo[req.ArchiveEventId] = e; }
        if (!e.ClearedStages.Contains(stage)) { e.ClearedStages.Add(stage); e.LastStage = stage; }
        ArchiveStageRewardHelper.Grant(user, stage, first);
        JsonDb.Save();
        await WriteDataAsync(new ResClearArchivePlaySodaCatchCoinStoryStage());
    }
}

[GameRequest("/archive/minigame/storydungeon/clearsmash")]
public class ClearArchivePlaySodaSmash : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqClearArchivePlaySodaSmashStoryStage req = await ReadData<ReqClearArchivePlaySodaSmashStoryStage>();
        User user = GetUser();
        int stage = req.ArchiveEventDungeonStageTableId;
        bool first = !user.EventInfo.TryGetValue(req.ArchiveEventId, out var e) || !e.ClearedStages.Contains(stage);
        if (!user.EventInfo.TryGetValue(req.ArchiveEventId, out e)) { e = new EventData(); user.EventInfo[req.ArchiveEventId] = e; }
        if (!e.ClearedStages.Contains(stage)) { e.ClearedStages.Add(stage); e.LastStage = stage; }
        ArchiveStageRewardHelper.Grant(user, stage, first);
        JsonDb.Save();
        await WriteDataAsync(new ResClearArchivePlaySodaSmashStoryStage());
    }
}

[GameRequest("/archive/minigame/storydungeon/clearclimbing")]
public class ClearArchivePlaySodaClimbing : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqClearArchivePlaySodaClimbingStoryStage req = await ReadData<ReqClearArchivePlaySodaClimbingStoryStage>();
        User user = GetUser();
        int stage = req.ArchiveEventDungeonStageTableId;
        bool first = !user.EventInfo.TryGetValue(req.ArchiveEventId, out var e) || !e.ClearedStages.Contains(stage);
        if (!user.EventInfo.TryGetValue(req.ArchiveEventId, out e)) { e = new EventData(); user.EventInfo[req.ArchiveEventId] = e; }
        if (!e.ClearedStages.Contains(stage)) { e.ClearedStages.Add(stage); e.LastStage = stage; }
        ArchiveStageRewardHelper.Grant(user, stage, first);
        JsonDb.Save();
        await WriteDataAsync(new ResClearArchivePlaySodaClimbingStoryStage());
    }
}
