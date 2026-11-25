using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/field/enter")]
public class EnterArchiveField : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqEnterArchiveField req = await ReadData<ReqEnterArchiveField>();
        User user = GetUser();

        if (!user.ArchiveFieldInfo.TryGetValue(req.MapId, out ArchiveFieldInfo? field))
        {
            field = new ArchiveFieldInfo();
            user.ArchiveFieldInfo[req.MapId] = field;
        }

        // The client sends the progress groups it wants loaded. Remember them so
        // later visits report the same progress state.
        foreach (int groupId in req.ArchiveProgressGroupIdList)
        {
            field.UserArchiveProgressIdList.AddUnique(groupId);
        }

        ResEnterArchiveField response = new()
        {
            Field = new NetFieldObjectData(),
            Json = field.Json,
        };

        response.Field.Stages.AddRange(field.CompletedStages);
        response.Field.Objects.AddRange(field.CompletedObjects);
        response.UserArchiveProgressIdList.AddRange(field.UserArchiveProgressIdList);
        response.NonResettableFieldObjects.AddRange(field.NonResettableFieldObjects);
        response.TeamPositions.AddRange(field.TeamPositions);
        response.ViewedEventFieldNoticePopupTableIds.AddRange(field.ViewedEventFieldNoticePopupTableIds);

        JsonDb.Save();
        await WriteDataAsync(response);
    }
}
