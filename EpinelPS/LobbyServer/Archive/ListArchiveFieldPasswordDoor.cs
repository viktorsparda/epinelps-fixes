using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/field/password-door/list")]
public class ListArchiveFieldPasswordDoor : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqListArchiveFieldPasswordDoorData req = await ReadData<ReqListArchiveFieldPasswordDoorData>();
        User user = GetUser();

        ResListArchiveFieldPasswordDoorData response = new();
        if (user.ArchiveFieldInfo.TryGetValue(req.MapId, out ArchiveFieldInfo? field))
        {
            response.AcquiredFieldPasswordIdList.AddRange(field.AcquiredFieldPasswordIdList);
            response.UnlockedFieldPasswordDoorIdList.AddRange(field.UnlockedFieldPasswordDoorIdList);
        }

        await WriteDataAsync(response);
    }
}
