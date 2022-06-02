using System.Drawing;

using AimuBot.Core.Extensions;

namespace AimuBot.Core.Utils;

internal class BotUtil
{
    public static Random Random = new((int)Timestamp);

    public static long Timestamp
        => (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;

    public static string CombinePath(string relativePath)
        => Path.Combine(AimuBot.Config.ResourcePath, relativePath);

    public static string ResourcePath
        => AimuBot.Config.ResourcePath;

    public static void SaveImageToJpg(Image image, string relativePath, int quality = 90)
        => image.SaveToJpg(CombinePath(relativePath), quality);
}