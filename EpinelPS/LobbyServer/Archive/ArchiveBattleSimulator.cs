using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive;

public class ArchiveBattleSimulatorState
{
    public int UpgradeCurrency { get; set; }
    public List<int> BuffIds { get; set; } = [];
    public List<int> ClearedStages { get; set; } = [];
    public int LastCharacterId { get; set; }
}

[GameRequest("/archive/minigame/battlesimulator/enter")]
public class EnterArchiveBattleSimulatorLobby : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqEnterArchiveBattleSimulatorLobby req = await ReadData<ReqEnterArchiveBattleSimulatorLobby>();
        User user = GetUser();
        var state = ArchiveMiniGameStateStore.Load<ArchiveBattleSimulatorState>(user, $"battlesim:{req.ArchiveEventId}");
        ResEnterArchiveBattleSimulatorLobby response = new() { LastPlayedCharacterId = state.LastCharacterId };
        response.ClearedStageIdList.AddRange(state.ClearedStages);
        await WriteDataAsync(response);
    }
}

[GameRequest("/archive/minigame/battlesimulator/buff/get")]
public class GetArchiveBattleSimulatorBuffList : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetArchiveBattleSimulatorBuffList req = await ReadData<ReqGetArchiveBattleSimulatorBuffList>();
        User user = GetUser();
        var state = ArchiveMiniGameStateStore.Load<ArchiveBattleSimulatorState>(user, $"battlesim:{req.ArchiveEventId}");
        ResGetArchiveBattleSimulatorBuffList response = new() { UpgradeCurrency = state.UpgradeCurrency };
        response.BuffIdList.AddRange(state.BuffIds);
        await WriteDataAsync(response);
    }
}

[GameRequest("/archive/minigame/battlesimulator/buff/upgrade")]
public class UpgradeArchiveBattleSimulatorBuff : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqUpgradeArchiveBattleSimulatorBuff req = await ReadData<ReqUpgradeArchiveBattleSimulatorBuff>();
        User user = GetUser();
        var state = ArchiveMiniGameStateStore.Load<ArchiveBattleSimulatorState>(user, $"battlesim:{req.ArchiveEventId}");
        if (!state.BuffIds.Contains(req.BuffId)) state.BuffIds.Add(req.BuffId);
        state.UpgradeCurrency++;
        ArchiveMiniGameStateStore.Save(user, $"battlesim:{req.ArchiveEventId}", state);
        JsonDb.Save();
        await WriteDataAsync(new ResUpgradeArchiveBattleSimulatorBuff { UpgradeCurrency = state.UpgradeCurrency });
    }
}

[GameRequest("/archive/minigame/battlesimulator/buff/reset")]
public class ResetArchiveBattleSimulatorBuff : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqResetArchiveBattleSimulatorBuff req = await ReadData<ReqResetArchiveBattleSimulatorBuff>();
        User user = GetUser();
        var state = ArchiveMiniGameStateStore.Load<ArchiveBattleSimulatorState>(user, $"battlesim:{req.ArchiveEventId}");
        state.BuffIds.Clear();
        state.UpgradeCurrency++;
        ArchiveMiniGameStateStore.Save(user, $"battlesim:{req.ArchiveEventId}", state);
        JsonDb.Save();
        await WriteDataAsync(new ResResetArchiveBattleSimulatorBuff { UpgradeCurrency = state.UpgradeCurrency });
    }
}

[GameRequest("/archive/minigame/battlesimulator/stage/normal/enter")]
public class EnterArchiveBattleSimulatorNormalStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqEnterArchiveBattleSimulatorNormalStage req = await ReadData<ReqEnterArchiveBattleSimulatorNormalStage>();
        User user = GetUser();
        var state = ArchiveMiniGameStateStore.Load<ArchiveBattleSimulatorState>(user, $"battlesim:{req.ArchiveEventId}");
        state.LastCharacterId = req.CharacterId;
        ArchiveMiniGameStateStore.Save(user, $"battlesim:{req.ArchiveEventId}", state);
        JsonDb.Save();
        await WriteDataAsync(new ResEnterArchiveBattleSimulatorNormalStage());
    }
}

[GameRequest("/archive/minigame/battlesimulator/stage/normal")]
public class CompleteArchiveBattleSimulatorNormalStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqCompleteArchiveBattleSimulatorNormalStage req = await ReadData<ReqCompleteArchiveBattleSimulatorNormalStage>();
        User user = GetUser();
        var state = ArchiveMiniGameStateStore.Load<ArchiveBattleSimulatorState>(user, $"battlesim:{req.ArchiveEventId}");
        ArchiveMiniGameStateStore.Save(user, $"battlesim:{req.ArchiveEventId}", state);
        JsonDb.Save();
        await WriteDataAsync(new ResCompleteArchiveBattleSimulatorNormalStage { UpgradeCurrency = state.UpgradeCurrency });
    }
}

[GameRequest("/archive/minigame/battlesimulator/stage/challenge/enter")]
public class EnterArchiveBattleSimulatorChallengeStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqEnterArchiveBattleSimulatorChallengeStage req = await ReadData<ReqEnterArchiveBattleSimulatorChallengeStage>();
        User user = GetUser();
        var state = ArchiveMiniGameStateStore.Load<ArchiveBattleSimulatorState>(user, $"battlesim:{req.ArchiveEventId}");
        state.LastCharacterId = req.CharacterId;
        ArchiveMiniGameStateStore.Save(user, $"battlesim:{req.ArchiveEventId}", state);
        JsonDb.Save();
        await WriteDataAsync(new ResEnterArchiveBattleSimulatorChallengeStage());
    }
}

[GameRequest("/archive/minigame/battlesimulator/stage/challenge")]
public class CompleteArchiveBattleSimulatorChallengeStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqCompleteArchiveBattleSimulatorChallengeStage req = await ReadData<ReqCompleteArchiveBattleSimulatorChallengeStage>();
        User user = GetUser();
        var state = ArchiveMiniGameStateStore.Load<ArchiveBattleSimulatorState>(user, $"battlesim:{req.ArchiveEventId}");
        ArchiveMiniGameStateStore.Save(user, $"battlesim:{req.ArchiveEventId}", state);
        JsonDb.Save();
        await WriteDataAsync(new ResCompleteArchiveBattleSimulatorChallengeStage());
    }
}
