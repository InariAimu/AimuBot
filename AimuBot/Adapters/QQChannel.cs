
using AimuBot.Adapters.Connection;

namespace AimuBot.Adapters;

internal class QQChannel : BotAdapter
{
    public AsyncSocket AsyncSocket { get; init; } = new();

    public QQChannel()
    {
        Name = "QQChannel";
        Protocol = "CSCode";
    }

    public override Task ReplyGroupMessageImage(long uin, int msgId, string imgPath) => throw new NotImplementedException();
    public override Task ReplyGroupMessageText(long uin, int msgId, string message) => throw new NotImplementedException();
    public override Task SendGroupMessageImage(long uin, string imgPath) => throw new NotImplementedException();
    public override Task SendGroupMessageSimple(long uin, string message) => throw new NotImplementedException();
    public override Task SendRawMessage(string message) => throw new NotImplementedException();
}
