using AimuBot.Core.Extensions;

namespace AimuBot.Core.Message.Model;

public class ReplyChain : BaseChain
{
    internal uint Uin { get; }

    internal uint Sequence { get; }

    internal long Uuid { get; }

    internal uint Time { get; }

    internal string Preview { get; }

    private ReplyChain(uint uin, uint sequence, long uuid, uint time, string preview)
        : base(ChainType.Reply, ChainMode.Singletag)
    {
        Uin = uin;
        Sequence = sequence;
        Uuid = uuid;
        Time = time;
        Preview = preview;
    }

    public static ReplyChain Create(BotMessage reference)
        => new(0, (uint)reference.Id, reference.SenderId, 0, "");

    internal static ReplyChain Create(uint uin, uint sequence, long uuid, uint time, string preview)
        => new(uin, sequence, uuid, time, preview);

    internal static ReplyChain ParseKqCode(string code)
    {
        var args = GetKqCodeArgs(code);
        {
            var qq = uint.Parse(args["qq"]);
            var seq = uint.Parse(args["seq"]);
            var uuid = long.Parse(args["uuid"]);
            var time = uint.Parse(args["time"]);
            var content = ""; //ByteConverter.UnBase64String(args["content"]);

            return Create(qq, seq, uuid, time, content);
        }
    }

    internal static ReplyChain ParseCsCode(string code) =>

        // [mirai:at:0]
        Create(0, 0, Convert.ToInt64(code.GetSandwichedText("[mirai:at:", "]")), 0, "");

    public override string ToKqCode()
        => $"[KQ:reply," +
           $"qq={Uin}," +
           $"seq={Sequence}," +
           $"uuid={Uuid}," +
           $"time={Time}," +
           $"content={Preview}]";

    public override string ToCsCode() => $"[cs:reply:{Uuid}]";

    public override string ToPreviewString()
        => "[回复]";
}