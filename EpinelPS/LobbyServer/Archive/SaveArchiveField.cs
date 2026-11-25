using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/field/save")]
public class SaveArchiveField : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqSaveArchiveFieldRawData req = await ReadData<ReqSaveArchiveFieldRawData>();
        User user = GetUser();

        if (!user.ArchiveFieldInfo.TryGetValue(req.MapId, out ArchiveFieldInfo? field))
        {
            field = new ArchiveFieldInfo();
            user.ArchiveFieldInfo[req.MapId] = field;
        }

        field.Json = req.Json ?? "";
        JsonDb.Save();

        await WriteDataAsync(new ResSaveArchiveFieldRawData());
    }
}
