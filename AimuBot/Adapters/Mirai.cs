using AimuBot.Adapters.Connection;
using AimuBot.Adapters.Connection.Event;
using AimuBot.Core.Extensions;
using AimuBot.Core.Utils;

namespace AimuBot.Adapters;

public class Mirai : BotAdapter
{
    public Mirai()
    {
        Name = "Mirai";
        Protocol = "CSCode";
    }

    public StringOverSocket StringOverSocket { get; set; }

    public async void StartReceiveMessage()
    {
        while (true)
        {
            var s = await StringOverSocket.Receive();
            if (s.IsNullOrEmpty())
            {
                BotLogger.LogW($"{Name}", "Disconnected");
                StringOverSocket._workSocket.Dispose();
                return;
            }

            RaiseMessageEvent(s);
        }
    }

    public override async Task SendRawMessage(string message)
    {
        if (StringOverSocket._workSocket.Connected && Core.AimuBot.Config.EnableSendMessage)
        {
            BotLogger.LogV("Bot", message);

            await StringOverSocket.Send(message);

            //Information.MessageSent++;
        }
        else
        {
            BotLogger.LogV("Bot", "[EnableSendMessage: Off] " + message);
        }
    }

    public override async Task SendGroupMessageSimple(long uin, string message)
        => await SendRawMessage($"[cs:gs:{uin}]{message}");

    public override async Task SendGroupMessageImage(long uin, string imgPath)
        => await SendRawMessage($"[cs:gs:{uin}][cs:image:{BotUtil.CombinePath(imgPath)}]");

    public override async Task ReplyGroupMessageText(long uin, int msgId, string message)
        => await SendRawMessage($"[cs:gs:{uin}][cs:reply:{msgId}]{message}");

    public override async Task ReplyGroupMessageImage(long uin, int msgId, string imgPath)
        => await SendRawMessage($"[cs:gs:{uin}][cs:reply:{msgId}][cs:image:{BotUtil.CombinePath(imgPath)}]");
}