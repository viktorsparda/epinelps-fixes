namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/minigame/DateSim/GetData")]
public class GetArchiveDateSim : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetArchiveDateSim req = await ReadData<ReqGetArchiveDateSim>();
        var response = new ResGetArchiveDateSim
        {
            ClientProgressJson = "{}",
            PermanentData = new()
        };

        if (GetUser().ArchiveDateSimProgressById.TryGetValue(req.ArchiveDateSimId, out var progress))
        {
            response.ClientProgressJson = progress.ClientProgressJson;
            response.PermanentData.Dialogs.AddRange(progress.CompletedDialogs);
        }

        await WriteDataAsync(response);
    }
}
