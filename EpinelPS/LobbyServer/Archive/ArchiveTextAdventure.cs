using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/minigame/textadv/getdata")]
public class GetArchiveTextAdvData : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetArchiveTextAdvData req = await ReadData<ReqGetArchiveTextAdvData>();
        User user = GetUser();
        ResGetArchiveTextAdvData response = new();
        if (user.ArchiveMiniGameState.TryGetValue($"textadv:{req.TextAdvId}", out string? json))
            response.Json = json;
        await WriteDataAsync(response);
    }
}

[GameRequest("/archive/minigame/textadv/setdata")]
public class SetArchiveTextAdvData : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqSetArchiveTextAdvData req = await ReadData<ReqSetArchiveTextAdvData>();
        User user = GetUser();
        user.ArchiveMiniGameState[$"textadv:{req.TextAdvId}"] = req.Json;
        JsonDb.Save();
        await WriteDataAsync(new ResSetArchiveTextAdvData());
    }
}

[GameRequest("/archive/minigame/textadv/deletedata")]
public class DeleteArchiveTextAdvData : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqDeleteArchiveTextAdvData req = await ReadData<ReqDeleteArchiveTextAdvData>();
        User user = GetUser();
        user.ArchiveMiniGameState.Remove($"textadv:{req.TextAdvId}");
        JsonDb.Save();
        await WriteDataAsync(new ResDeleteArchiveTextAdvData());
    }
}
