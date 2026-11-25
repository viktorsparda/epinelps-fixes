using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.BattleLog;

[GameRequest("/system/battlelog/disallowed-team-types")]
public class GetBattleLogUploadDisallowedTeamTypes : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        _ = await ReadData<ReqGetBattleLogUploadDisallowedTeamTypes>();

        // An empty list means every team type is allowed to upload.
        ResGetBattleLogUploadDisallowedTeamTypes response = new();
        await WriteDataAsync(response);
    }
}
