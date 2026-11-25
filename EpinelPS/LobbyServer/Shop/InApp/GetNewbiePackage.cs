using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Shop.InApp;

[GameRequest("/inappshop/newbiepackage/get")]
public class GetNewbiePackage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        _ = await ReadData<ReqGetNewbiePackage>();

        // Leave State null when no newbie package is active. The client's
        // NewbiePackageShopManager handles a null state, but throws a
        // NullReferenceException when it receives a partial non-null state.
        ResGetNewbiePackage response = new();

        await WriteDataAsync(response);
    }
}
