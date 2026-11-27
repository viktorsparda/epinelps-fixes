using EpinelPS.Data;

namespace EpinelPS.LobbyServer.Event;

public static class ChallengeEventHelper
{
    /// <summary>
    /// Challenge stage IDs look like 21{eventNumber}1xx (e.g. event 60123 ->
    /// 212310101), and the matching spot-battle row carries the ticket item.
    /// </summary>
    public static int GetTicketItemId(int eventId)
    {
        int eventNumber = eventId % 100;
        string prefix = $"21{eventNumber:00}";
        return GameData.Instance.EventDungeonSpotBattleTable.Values
            .Where(r => r.Id.ToString().StartsWith(prefix, StringComparison.Ordinal)
                        && (r.ShortName ?? "").Contains("CHALLENGE", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.TicketItemId)
            .FirstOrDefault(id => id > 0);
    }
}
