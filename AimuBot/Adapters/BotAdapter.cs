using AimuBot.Core;
using AimuBot.Core.Events;
using AimuBot.Core.Message;

namespace AimuBot.Adapters;

public enum BotAdapterStatus
{
    Waiting,
    Connected,
    Disconnected,
    Disposed,
}

public abstract class BotAdapter
{
    public string Name { get; set; } = "Bot";
    public string Protocol { get; set; } = "None";

    public BotAdapterStatus Status { get; set; } = BotAdapterStatus.Waiting;

    public event EventDispatcher.BotEvent<MessageEvent>? OnMessageReceived;

    public int MessageSent { get; set; }
    public int MessageReceived { get; set; }
    
    internal void RaiseMessageEvent(string message)
    {
        var m = BotMessage.FromCSCode(message);
        m.Bot = this;
        OnMessageReceived?.Invoke(this, MessageEvent.Create(Name, LogLevel.Verbose, "", m));
    }

    public abstract Task SendRawMessage(string message);

    public abstract Task SendGroupMessageSimple(long uin, string message);

    public abstract Task SendGroupMessageImage(long uin, string imgPath);

    public abstract Task ReplyGroupMessageText(long uin, int msgId, string message);

    public abstract Task ReplyGroupMessageImage(long uin, int msgId, string imgPath);
}