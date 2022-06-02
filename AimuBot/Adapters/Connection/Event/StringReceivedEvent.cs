namespace AimuBot.Adapters.Connection.Event;

public class StringReceivedEvent : EventArgs
{
    public StringReceivedEvent(string message)
    {
        Message = message;
    }

    public string Message { get; }
}