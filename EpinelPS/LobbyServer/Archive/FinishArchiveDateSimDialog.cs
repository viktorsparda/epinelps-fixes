using System.Text.Json;
using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Archive;

[GameRequest("/archive/minigame/DateSim/FinishDialog")]
public class FinishArchiveDateSimDialog : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqFinishArchiveDateSimDialog req = await ReadData<ReqFinishArchiveDateSimDialog>();
        var response = new ResFinishArchiveDateSimDialog();
        if (req.ArchiveDateSimId <= 0 || string.IsNullOrWhiteSpace(req.DialogId)
            || !IsValidProgress(req.ClientProgressJson))
        {
            await WriteDataAsync(response);
            return;
        }

        User user = GetUser();
        if (!user.ArchiveDateSimProgressById.TryGetValue(req.ArchiveDateSimId, out var progress))
        {
            progress = new();
            user.ArchiveDateSimProgressById.Add(req.ArchiveDateSimId, progress);
        }

        if (!progress.CompletedDialogs.Contains(req.DialogId))
            progress.CompletedDialogs.Add(req.DialogId);
        if (req.ClientProgressJson.Length > 0)
            progress.ClientProgressJson = req.ClientProgressJson;

        JsonDb.Save();
        await WriteDataAsync(response);
    }

    private static bool IsValidProgress(string json)
    {
        if (json.Length == 0) return true;
        if (json.Length > 256 * 1024) return false;
        try
        {
            using var document = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
