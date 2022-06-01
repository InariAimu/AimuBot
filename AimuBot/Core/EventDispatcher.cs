using System.Diagnostics.CodeAnalysis;

using AimuBot.Adapters;
using AimuBot.Core.Events;

namespace AimuBot.Core;
public class EventDispatcher
{
    public delegate void BotEvent<in TArgs>(BotAdapter sender, TArgs args);

    private Dictionary<Type, Action<BotAdapter, BaseEvent>>? _dict;

    public event BotEvent<LogEvent>? OnLog;
    public event BotEvent<MessageEvent>? OnMessage;

    [MemberNotNull(nameof(_dict))]
    public void InitializeHandlers() => _dict = new()
    {
        { typeof(LogEvent), (bot, e) => OnLog?.Invoke(bot, (LogEvent)e) },
        { typeof(MessageEvent), (bot, e) => OnMessage?.Invoke(bot, (MessageEvent)e) },
    };

    public void RaiseEvent(BotAdapter bot, BaseEvent anyEvent)
    {
        if (_dict is null) return;

        Task.Run(() =>
        {
            try
            {
                // Call user event
                _dict[anyEvent.GetType()].Invoke(bot, anyEvent);
            }
            catch (Exception ex)
            {
                // Suppress exceptions
                OnLog?.Invoke(bot, LogEvent.Create("Bot",
                    LogLevel.Warning, $"{ex.StackTrace}\n{ex.Message}"));
            }
        });
    }
}
