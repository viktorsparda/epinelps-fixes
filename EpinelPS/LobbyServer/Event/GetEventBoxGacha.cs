using EpinelPS.Data;
using EpinelPS.Models;

namespace EpinelPS.LobbyServer.Event;

[GameRequest("/event/boxgacha/get")]
public class GetEventBoxGacha : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetEventBoxGacha req = await ReadData<ReqGetEventBoxGacha>();
        User user = GetUser();

        ResGetEventBoxGacha response = new();
        UserEventBoxGachaData? gachaData = null;
        if (user.EventBoxGachaData.TryGetValue(req.EventId, out UserEventBoxGachaData? data))
        {
            gachaData = data;
            response.GachaCount = data.GachaCount;
        }

        HashSet<int> ownedCostumeOrders = [];
        EventBoxGachaRecord? boxGacha = GameData.Instance.EventBoxGachaTable.Values.FirstOrDefault(x => x.EventId == req.EventId);
        if (boxGacha != null)
        {
            ownedCostumeOrders = EventBoxGachaHelper.GetOwnedCostumeOrders(user, boxGacha.GachaRewardGroup);
        }

        response.RewardOrders.AddRange(EventBoxGachaHelper.GetResolvedOrders(gachaData, ownedCostumeOrders));

        await WriteDataAsync(response);
    }
}
