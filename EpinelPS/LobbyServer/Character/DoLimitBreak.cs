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

        CharacterModel targetCharacter = user.GetCharacterBySerialNumber(req.Csn) ?? throw new NullReferenceException();

        // Find the element with the current csn from the request
        CharacterRecord currentCharacter = fullchardata.FirstOrDefault(c => c.Id == targetCharacter.Tid) ?? throw new NullReferenceException();

        if (currentCharacter.GradeCoreId == 103 || currentCharacter.GradeCoreId == 11 || currentCharacter.GradeCoreId == 201)
        {
            Logging.WriteLine($"Limit break rejected: {targetCharacter.Tid} is already at maximum grade", LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        // Find a new CSN based on the `NameCode` of the current character and `GradeCoreId + req.Count`
        // For some reason, there is a seperate character for each limit/core break value.
        CharacterRecord? newCharacter = fullchardata.FirstOrDefault(c => c.NameCode == currentCharacter.NameCode && c.GradeCoreId == currentCharacter.GradeCoreId + req.Count);

        if (newCharacter == null)
        {
            // No next grade exists. Return the current character so the client keeps
            // the correct grade instead of resetting to Grade 0 / keeping the
            // limit-break button enabled (and without throwing a client NRE).
            Logging.WriteLine($"Limit break rejected: no grade {currentCharacter.GradeCoreId + req.Count} for {currentCharacter.NameCode}", LogType.Warning);
            response.Character = ToNet(targetCharacter);
            await WriteDataAsync(response);
            return;
        }

        // replace character in DB with new character
        targetCharacter.Grade += req.Count;
        targetCharacter.Tid = newCharacter.Id;

        response.Character = ToNet(targetCharacter);

        // remove spare body item
        DbItemData bodyItem = user.Items.FirstOrDefault(i => i.Isn == req.Isn) ?? throw new NullReferenceException();
        user.RemoveItemBySerialNumber(req.Isn, req.Count);
        response.Items.Add(NetUtils.ToNet(bodyItem));

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
