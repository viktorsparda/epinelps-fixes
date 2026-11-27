using EpinelPS.Database;
using Newtonsoft.Json;

namespace EpinelPS.LobbyServer.Archive;

public static class ArchiveMiniGameStateStore
{
    public static T Load<T>(User user, string key) where T : new()
    {
        if (!user.ArchiveMiniGameState.TryGetValue(key, out string? json) || string.IsNullOrWhiteSpace(json))
            return new T();
        try { return JsonConvert.DeserializeObject<T>(json) ?? new T(); }
        catch { return new T(); }
    }

    public static void Save<T>(User user, string key, T value)
    {
        user.ArchiveMiniGameState[key] = JsonConvert.SerializeObject(value);
    }
}
