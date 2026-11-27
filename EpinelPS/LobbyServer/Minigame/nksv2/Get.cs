using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;

namespace EpinelPS.LobbyServer.Minigame.nksv2;

[GameRequest("/arcade/nksv2/get")]
public class Get : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetMiniGameNKSV2Data req = await ReadData<ReqGetMiniGameNKSV2Data>();
        User user = GetUser();
        ResGetMiniGameNKSV2Data response = new();

        // Archive NKS runs use the archive content manager (e.g. 2) for the
        // missions/static data, while the client still sends nKsId 3 for the
        // v2 engine. Keep the save keyed by the requested id, but build the
        // mission list from the archive manager.
        int managerId = user.ArchiveNksManagerId > 0 ? user.ArchiveNksManagerId : req.NKsId;
        List<EventNKSMissionRecord> missionlist = GameData.Instance.EventNKSMissionTable.Values
            .Where(x => x.CommonSettingsID == managerId)
            .OrderBy(x => x.Id)
            .ToList();

        bool needsRebuild = !user.Nksv2Datas.TryGetValue(req.NKsId, out Nksv2Data? nksv2Data);
        if (!needsRebuild && managerId != req.NKsId)
        {
            needsRebuild = missionlist.Count > 0 &&
                !missionlist.All(m => nksv2Data!.MissionProgressData.ContainsKey(m.Id));
        }

        if (needsRebuild)
        {
            nksv2Data = new Nksv2Data();
            foreach (EventNKSMissionRecord item in missionlist)
            {
                nksv2Data.MissionProgressData[item.Id] = new MiniGameNKSV2MissionProgress
                {
                    NKsMissionId = item.Id,
                    CreatedAt = DateTime.UtcNow.ToTimestamp(),
                    Progress = 0,
                    Seq = (long)User.GenerateMsn()
                };
            }
            user.Nksv2Datas[req.NKsId] = nksv2Data;
        }

        if (nksv2Data!.MissionProgressData.Count > 0)
        {
            var missprodata = MiniGameHelper
                .ToProtoDict<int, NetMiniGameNKSV2MissionProgress, MiniGameNKSV2MissionProgress>(nksv2Data.MissionProgressData);
            response.MissionProgressList.AddRange(missprodata.Values);
        }

        if (nksv2Data.ProgressJson != null)
            response.ProgressJson = nksv2Data.ProgressJson;

        JsonDb.Save();
        await WriteDataAsync(response);
    }
}
