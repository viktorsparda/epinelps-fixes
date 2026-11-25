using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Auth;

[GameRequest("/auth/logout")]
public class Logout : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqLogout req = await ReadData<ReqLogout>();

        if (!string.IsNullOrEmpty(req.EndLogInfo))
        {
            Logging.WriteLine($"Logout requested: {req.EndLogInfo}", LogType.Info);
        }

        await WriteDataAsync(new ResLogout());
    }
}
