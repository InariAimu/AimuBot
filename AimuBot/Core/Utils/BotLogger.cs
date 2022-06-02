namespace AimuBot.Core.Utils;

public class BotLogger
{
    public static void Log(object obj)
        => Console.WriteLine(obj);

    public static void LogN(string tag, object obj)
        => Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {tag} {obj}");

    public static void LogE(string tag, object obj)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        LogN($"E/{tag}", obj);
        Console.ResetColor();
    }

    public static void LogW(string tag, object obj)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        LogN($"W/{tag}", obj);
        Console.ResetColor();
    }

    public static void LogI(string tag, object obj)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        LogN($"I/{tag}", obj);
        Console.ResetColor();
    }

    public static void LogV(string tag, object obj)
        => LogN($"V/{tag}", obj);
}