namespace AimuBot.Core.Extensions;

public static class IEnumerableTExtension
{
    public static bool IsEmpty<TSource>(this IEnumerable<TSource> source) => !source.Any();

    public static bool IsNotEmpty<TSource>(this IEnumerable<TSource> source) => source.Any();

    public static TSource? Random<TSource>(this IEnumerable<TSource> source) where TSource : class?
    {
        if (source.IsEmpty())
            return null;
        return source.ElementAt(ExtensionUtils.Random.Next(0, source.Count()));
    }

    public static (bool, TSource) RandomWhenNotEmpty<TSource>(this IEnumerable<TSource> source) where TSource : struct
    {
        if (source.IsEmpty())
            return (false, default);
        return (true, source.ElementAt(ExtensionUtils.Random.Next(0, source.Count())));
    }
}