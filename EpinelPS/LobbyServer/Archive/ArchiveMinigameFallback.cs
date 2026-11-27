using Google.Protobuf;
using System.Reflection;

namespace EpinelPS.LobbyServer.Archive;

/// <summary>
/// Typed fallback for archive minigame endpoints that do not have a dedicated
/// handler yet. Instead of letting the request fall into EmptyHandler (which
/// answers with ResGetNow and makes the client throw on deserialize), return the
/// correct archive response type with every nested message initialized.
///
/// This makes unsupported archive minigames launchable/inspectable; handlers
/// with real state/persistence still take precedence.
/// </summary>
public static class ArchiveMinigameFallback
{
    private static readonly Dictionary<string, string> ResponseTypes = new()
    {
        ["/archive/minigame/DateSim/ObtainAlbumReward"] = "ResObtainArchiveDateSimAlbumReward",
        ["/archive/minigame/DateSim/ObtainSpecialReward"] = "ResObtainArchiveDateSimSpecialReward",
        ["/archive/minigame/battlesimulator/buff/get"] = "ResGetArchiveBattleSimulatorBuffList",
        ["/archive/minigame/battlesimulator/buff/reset"] = "ResResetArchiveBattleSimulatorBuff",
        ["/archive/minigame/battlesimulator/buff/upgrade"] = "ResUpgradeArchiveBattleSimulatorBuff",
        ["/archive/minigame/battlesimulator/enter"] = "ResEnterArchiveBattleSimulatorLobby",
        ["/archive/minigame/battlesimulator/currency/add"] = "ResCheatAddArchiveBattleSimulatorBuffCurrency",
        ["/archive/minigame/battlesimulator/reset"] = "ResCheatResetArchiveBattleSimulator",
        ["/archive/minigame/battlesimulator/stage/challenge"] = "ResCompleteArchiveBattleSimulatorChallengeStage",
        ["/archive/minigame/battlesimulator/stage/challenge/enter"] = "ResEnterArchiveBattleSimulatorChallengeStage",
        ["/archive/minigame/battlesimulator/stage/normal"] = "ResCompleteArchiveBattleSimulatorNormalStage",
        ["/archive/minigame/battlesimulator/stage/normal/enter"] = "ResEnterArchiveBattleSimulatorNormalStage",
        ["/archive/minigame/dice/get"] = "ResGetArchiveDice",
        ["/archive/minigame/dice/roll"] = "ResArchiveDiceRoll",
        ["/archive/minigame/storydungeon/clearcatchcoin"] = "ResClearArchivePlaySodaCatchCoinStoryStage",
        ["/archive/minigame/storydungeon/clearclimbing"] = "ResClearArchivePlaySodaClimbingStoryStage",
        ["/archive/minigame/storydungeon/clearrunning"] = "ResClearArchivePlaySodaRunningStoryStage",
        ["/archive/minigame/storydungeon/clearsmash"] = "ResClearArchivePlaySodaSmashStoryStage",
        ["/archive/minigame/storydungeon/clearstackup"] = "ResClearArchivePlaySodaStackUpStoryStage",
        ["/archive/minigame/storydungeon/enterplaysoda"] = "ResEnterArchivePlaySodaStoryStage",
        ["/archive/minigame/textadv/deletedata"] = "ResDeleteArchiveTextAdvData",
        ["/archive/minigame/textadv/getdata"] = "ResGetArchiveTextAdvData",
        ["/archive/minigame/textadv/setdata"] = "ResSetArchiveTextAdvData",
        ["/archive/minigame/towerdefense/enter"] = "ResEnterArchiveTowerDefense",
        ["/archive/minigame/towerdefense/finish"] = "ResFinishArchiveTowerDefense",
        ["/archive/minigame/towerdefense/gettutorialids"] = "ResGetArchiveTowerDefenseTutorialIds",
        ["/archive/minigame/towerdefense/settutorialids"] = "ResSetArchiveTowerDefenseTutorialIds",
        ["/archive/minigame/ban"] = "ResCheatArchiveMiniGameBan",
    };

    public static bool CanHandle(string path) => ResponseTypes.ContainsKey(path);

    public static async Task<bool> TryWriteAsync(string path, HttpContext ctx)
    {
        if (!ResponseTypes.TryGetValue(path, out string? typeName))
            return false;
        Type? type = typeof(LobbyHandler).Assembly.GetType("EpinelPS." + typeName);
        if (type == null || !typeof(IMessage).IsAssignableFrom(type))
            return false;

        IMessage message = (IMessage)Activator.CreateInstance(type)!;
        InitializeMessages(message, 0);

        ctx.Response.ContentType = "application/octet-stream+protobuf";
        ctx.Response.ContentLength = message.CalculateSize();
        using CodedOutputStream output = new(ctx.Response.Body);
        message.WriteTo(output);
        output.Flush();
        await Task.CompletedTask;
        return true;
    }

    private static void InitializeMessages(IMessage message, int depth)
    {
        if (depth > 3) return;
        foreach (PropertyInfo property in message.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length != 0)
                continue;
            Type propertyType = property.PropertyType;
            if (!typeof(IMessage).IsAssignableFrom(propertyType) || propertyType.IsAbstract)
                continue;
            if (property.GetValue(message) is not IMessage child)
            {
                child = (IMessage)Activator.CreateInstance(propertyType)!;
                property.SetValue(message, child);
            }
            InitializeMessages(child, depth + 1);
        }
    }
}
