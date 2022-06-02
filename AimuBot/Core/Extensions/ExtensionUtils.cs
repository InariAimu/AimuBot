namespace AimuBot.Core.Extensions;

public static class ExtensionUtils
{
    public static Random Random { get; private set; }

    static ExtensionUtils()
    {
        Random = new Random(DateTime.Now.Millisecond);
    }
}