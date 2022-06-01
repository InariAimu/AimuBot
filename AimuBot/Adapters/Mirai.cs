using AimuBot.Adapters.Connection;
using AimuBot.Adapters.Connection.Event;
using AimuBot.Core.Utils;

namespace AimuBot.Adapters;

public class Mirai : BotAdapter
{
    public AsyncSocket AsyncSocket { get; private set; } = new AsyncSocket();

    public Mirai()
    {
        Name = "Mirai";
        Protocol = "CSCode";
    }

    public async void WaitForConnection()
    {
        AsyncSocket.OnConnected += AsyncSocket_OnConnected;
        AsyncSocket.OnStringReceived += AsyncSocket_OnStringReceived;
        await AsyncSocket.WaitConnection();
    }

    public void AsyncSocket_OnConnected(FuturedSocket sender, SocketConnetedEvent args)
        => AsyncSocket.Receive();

    public void AsyncSocket_OnStringReceived(FuturedSocket sender, StringReceivedEvent args)
        => RaiseMessageEvent(args.Message);

    public override async Task SendRawMessage(string message)
    {
        if (AsyncSocket.Connected && Core.AimuBot.Config.EnableSendMessage)
        {
            BotLogger.LogV("Bot", message);

            await AsyncSocket.SendString(message);

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
