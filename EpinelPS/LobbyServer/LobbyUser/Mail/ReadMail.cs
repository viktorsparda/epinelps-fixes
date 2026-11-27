using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.LobbyUser.Mail;

[GameRequest("/mail/read")]
public class ReadMail : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqReadMail req = await ReadData<ReqReadMail>();
        User user = GetUser();

        // Mark the mail read so the unread badge clears and DeleteReadMail can
        // remove it later. State 2 is the read/claimed state used by the other
        // mail handlers.
        if (user.MailDatas.TryGetValue(req.Msn, out NetUserMailData? mail))
        {
            mail.State = 2;
            JsonDb.Save();
        }

        await WriteDataAsync(new ResReadMail());
    }
}
