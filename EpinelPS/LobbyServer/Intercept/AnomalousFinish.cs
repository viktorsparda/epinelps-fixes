namespace EpinelPS.LobbyServer.Intercept;

[GameRequest("/intercept/Anomalous/Finish")]
public class FinishAnomalousIntercept : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        _ = await ReadData<ReqFinishInterceptAnomalous>();

        ResFinishInterceptAnomalous response = new()
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
