using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Character;

[GameRequest("/character/toggle/list")]
public class ListCharacterToggle : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        _ = await ReadData<ReqListCharacterToggle>();

        // The client only needs the server's stored toggles. The game currently
        // has no setter request for these lists, so the defaults (empty) are the
        // correct state. Respond with a typed message instead of falling through
        // to the generic empty handler.
        ResListCharacterToggle response = new();
        await WriteDataAsync(response);
    }
}
