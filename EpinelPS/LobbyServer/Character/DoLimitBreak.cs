using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Character;

[GameRequest("/character/upgrade")]
public class DoLimitBreak : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        // Read the incoming request that contains the current CSN and ISN
        ReqCharacterUpgrade req = await ReadData<ReqCharacterUpgrade>(); // Contains csn and isn (read-only)
        ResCharacterUpgrade response = new();
        User user = GetUser();

        // Get all character data from the game's character table
        List<CharacterRecord> fullchardata = [.. GameData.Instance.CharacterTable.Values];

        CharacterModel? targetCharacter = user.GetCharacterBySerialNumber(req.Csn);
        if (targetCharacter == null)
        {
            Logging.WriteLine($"Limit break rejected: no character for csn {req.Csn}", LogType.Warning);
            await WriteDataAsync(response);
            return;
        }

        if (!GameData.Instance.CharacterTable.TryGetValue(targetCharacter.Tid, out CharacterRecord? currentCharacter))
        {
            Logging.WriteLine($"Limit break rejected: tid {targetCharacter.Tid} is not in the character table", LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        int maxLimitBreakGradeCoreId = CharacterGradeHelper.MaxLimitBreakGradeCoreId(currentCharacter.OriginalRare);
        if (currentCharacter.GradeCoreId >= maxLimitBreakGradeCoreId)
        {
            // Limit break is finished; further copies are core enhancement, which
            // the client must send to /character/coreupgrade. Never let a stale
            // limit-break request cross into core levels here.
            Logging.WriteLine(
                $"Limit break rejected: {targetCharacter.Tid} is already at maximum limit break " +
                $"({currentCharacter.GradeCoreId}/{maxLimitBreakGradeCoreId}, grade {targetCharacter.Grade})",
                LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        if (req.Count <= 0)
        {
            Logging.WriteLine($"Limit break rejected: invalid count {req.Count} for {targetCharacter.Tid}", LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        // Only consume what is needed to reach the limit-break cap. A stale
        // client may offer more spare bodies than the character can use.
        int used = Math.Min(req.Count, maxLimitBreakGradeCoreId - currentCharacter.GradeCoreId);
        int newGradeCoreId = currentCharacter.GradeCoreId + used;

        CharacterRecord? newCharacter = fullchardata.FirstOrDefault(
            c => c.NameCode == currentCharacter.NameCode && c.GradeCoreId == newGradeCoreId);
        if (newCharacter == null)
        {
            // No next grade exists. Return the current character so the client keeps
            // the correct grade instead of resetting to Grade 0 / keeping the
            // limit-break button enabled (and without throwing a client NRE).
            Logging.WriteLine($"Limit break rejected: no grade {newGradeCoreId} for {currentCharacter.NameCode}", LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        DbItemData? bodyItem = user.Items.FirstOrDefault(i => i.Isn == req.Isn);
        if (bodyItem == null || bodyItem.ItemType != currentCharacter.PieceId || bodyItem.Count < used)
        {
            Logging.WriteLine(
                $"Limit break rejected: spare body isn {req.Isn} missing/wrong/insufficient for " +
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

        if (newCharacter.GradeCoreId == CharacterGradeHelper.MaxGradeCoreId(newCharacter.OriginalRare))
            user.AddTrigger(Trigger.CharacterGradeMax, 1);

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
