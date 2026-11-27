using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Character;

[GameRequest("/character/coreupgrade")]
public class CoreUpgrade : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        // Read the incoming request that contains the current CSN and ISN
        ReqCharacterCoreUpgrade req = await ReadData<ReqCharacterCoreUpgrade>(); // Contains csn and isn (read-only)
        ResCharacterCoreUpgrade response = new();

        User user = GetUser();

        // Get all character data from the game's character table
        List<CharacterRecord> fullchardata = [.. GameData.Instance.CharacterTable.Values];

        CharacterModel? targetCharacter = user.GetCharacterBySerialNumber(req.Csn);
        if (targetCharacter == null)
        {
            Logging.WriteLine($"Core upgrade rejected: no character for csn {req.Csn}", LogType.Warning);
            await WriteDataAsync(response);
            return;
        }

        if (!GameData.Instance.CharacterTable.TryGetValue(targetCharacter.Tid, out CharacterRecord? currentCharacter))
        {
            Logging.WriteLine($"Core upgrade rejected: tid {targetCharacter.Tid} is not in the character table", LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        int maxLimitBreakGradeCoreId = CharacterGradeHelper.MaxLimitBreakGradeCoreId(currentCharacter.OriginalRare);
        int maxGradeCoreId = CharacterGradeHelper.MaxGradeCoreId(currentCharacter.OriginalRare);

        if (currentCharacter.OriginalRare != OriginalRareType.SSR || maxGradeCoreId == maxLimitBreakGradeCoreId)
        {
            Logging.WriteLine($"Core upgrade rejected: {targetCharacter.Tid} ({currentCharacter.OriginalRare}) has no core enhancement", LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        if (currentCharacter.GradeCoreId < maxLimitBreakGradeCoreId)
        {
            Logging.WriteLine(
                $"Core upgrade rejected: {targetCharacter.Tid} is not at maximum limit break yet " +
                $"({currentCharacter.GradeCoreId}/{maxLimitBreakGradeCoreId})",
                LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        if (currentCharacter.GradeCoreId >= maxGradeCoreId)
        {
            Logging.WriteLine($"Core upgrade rejected: {targetCharacter.Tid} is already at maximum grade", LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        if (req.Count <= 0)
        {
            Logging.WriteLine($"Core upgrade rejected: invalid count {req.Count} for {targetCharacter.Tid}", LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        int used = Math.Min(req.Count, maxGradeCoreId - currentCharacter.GradeCoreId);
        int newGradeCoreId = currentCharacter.GradeCoreId + used;

        // Find a new CSN based on the `NameCode` of the current character and `GradeCoreId + used`
        // For some reason, there is a seperate character for each limit/core break value.
        CharacterRecord? newCharacter = fullchardata.FirstOrDefault(
            c => c.NameCode == currentCharacter.NameCode && c.GradeCoreId == newGradeCoreId);

        if (newCharacter == null)
        {
            // No next grade exists. Return the current character so the client keeps
            // the correct grade/state instead of resetting to Grade 0.
            Logging.WriteLine($"Core upgrade rejected: no grade {newGradeCoreId} for {currentCharacter.NameCode}", LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        DbItemData? bodyItem = user.Items.FirstOrDefault(i => i.Isn == req.Isn);
        if (bodyItem == null || bodyItem.ItemType != currentCharacter.PieceId || bodyItem.Count < used)
        {
            Logging.WriteLine(
                $"Core upgrade rejected: spare body isn {req.Isn} missing/wrong/insufficient for " +
                $"tid {targetCharacter.Tid} (need {used})",
                LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        // replace character in DB with new character
        targetCharacter.Tid = newCharacter.Id;
        targetCharacter.Grade = CharacterGradeHelper.GradeIndex(newCharacter);
        response.Character = ToNet(targetCharacter);

        // remove the consumed spare bodies
        bodyItem.Count -= used;
        if (bodyItem.Count <= 0)
            user.Items.Remove(bodyItem);
        response.Items.Add(NetUtils.ToNet(bodyItem));

        if (newCharacter.GradeCoreId == maxGradeCoreId)
        {
            user.AddTrigger(Trigger.CharacterGradeMax, 1);
        }

        JsonDb.Save();

        // Send the response back to the client
        await WriteDataAsync(response);
    }

    private static NetUserCharacterDefaultData ToNet(CharacterModel character)
    {
        return new NetUserCharacterDefaultData()
        {
            Csn = character.Csn,
            CostumeId = character.CostumeId,
            Grade = character.Grade,
            Lv = character.Level,
            Skill1Lv = character.Skill1Lvl,
            Skill2Lv = character.Skill2Lvl,
            Tid = character.Tid,
            UltiSkillLv = character.UltimateLevel
        };
    }
}
