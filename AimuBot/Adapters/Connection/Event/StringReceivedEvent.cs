namespace AimuBot.Adapters.Connection.Event;

public class StringReceivedEvent : EventArgs
{
    public string Message { get; set; }

    public StringReceivedEvent(string message) => Message = message;
}
