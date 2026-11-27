namespace EpinelPS.LobbyServer.Intercept;

[GameRequest("/intercept/Anomalous/FastClear")]
public class AnomalousFastClear : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        _ = await ReadData<ReqFastClearInterceptAnomalous>();

        // The client applies these rewards after the result screen; never send
        // null NetRewardData payloads.
        ResFastClearInterceptAnomalous response = new()
        {
            NormalReward = new NetRewardData(),
            BonusReward = new NetRewardData(),
            NormalRewardByRewardUpEvent = new NetRewardData(),
            BonusRewardByRewardUpEvent = new NetRewardData(),
            TodayRemainingTickets = 5,
            MaxTicketCount = 5,
        };

        await WriteDataAsync(response);
    }
}
