namespace AimuBot.Core.Message.Model;

public abstract class BaseChain
{
    public enum ChainType
    {
        At,
        Reply,
        Text,
        Image,
        Flash,
        Record,
        Video,
        QFace,
        BFace,
        Xml,
        MultiMsg,
        Json,
    }

    public enum ChainMode
    {
        Multiple,
        Singleton,
        Singletag
    }
    public ChainType Type { get; protected set; }

    public ChainMode Mode { get; protected set; }

    protected BaseChain(ChainType type, ChainMode mode)
    {
        Type = type;
        Mode = mode;
    }

    public abstract string ToPreviewString();
    public abstract string ToCsCode();
    public abstract string ToKqCode();

    /// <summary>
    /// Get arguments of a code string
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    internal static Dictionary<string, string> GetKqCodeArgs(string code)
    {
        Dictionary<string, string>? kvpair = new Dictionary<string, string>();

        // Split with a comma
        // [KQ:x,x=1,y=2] will becomes
        // "KQ:x" "x=1" "y=2"
        string[]? split = code[..^1].Split(',');
        {
            // Split every kvpair with an equal
            // "KQ:x" ignored
            // "x=1" will becomes "x" "1"
            for (int i = 1; i < split.Length; ++i)
            {
                string[]? eqpair = split[i].Split('=');
                if (eqpair.Length < 2) continue;
                {
                    kvpair.Add(eqpair[0],
                        string.Join("=", eqpair[1..]));
                }
            }

            return kvpair;
        }
    }
}
