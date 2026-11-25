using EpinelPS.Data;

namespace EpinelPS.LobbyServer.Character;

[GameRequest("/character/costume/get")]
public class GetCharacterCostume : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        _ = await ReadData<ReqGetCharacterCostumeData>();

        ResGetCharacterCostumeData response = new();
        User user = GetUser();

        // Return the costumes this user actually owns. Returning every costume
        // from static data made removed/unowned costumes appear owned to the
        // client and broke the costume-gacha "already owned" state.
        response.CostumeIds.AddRange(user.CostumeList);

        await WriteDataAsync(response);
    }
}
