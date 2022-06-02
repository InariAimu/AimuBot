using AimuBot.Adapters;
using AimuBot.Core.Extensions;

namespace AimuBot.Core.Message;

public class BotMessage
{
    public BotAdapter Bot { get; set; }
    public MessageChain Chain { get; set; }
    public int Id { get; set; } = 0;
    public MessageType Type { get; set; } = MessageType.None;
    public long SubjectId { get; set; } = 0;
    public string SubjectName { get; set; } = string.Empty;
    public long SenderId { get; set; } = 0;
    public string SenderName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Raw { get; private set; } = string.Empty;
    public GroupLevel Level { get; set; } = GroupLevel.Member;

    public string Content { get; set; } = string.Empty;

    public string SubjectNameShort
        => SubjectName.Length > 8 ? SubjectName[..7] + ".." : SubjectName;

    public string SenderNameShort
        => SenderName.Length > 8 ? SenderName[..7] + ".." : SenderName;

    public override string ToString()
        => $"[{SubjectNameShort}({SubjectId})] {SenderNameShort}({SenderId}) -> {Body}";

    public static BotMessage FromCSCode(string csCode)
    {
        BotMessage messageDesc = new()
        {
            Raw = csCode
        };

        ForEachCSCode(csCode, (origin, name, args) =>
        {
            //Console.WriteLine($"{origin} {name} {args}");

            if (name == null)
            {
                messageDesc.Body += origin;
            }
            else if (name == "image" || name == "at")
            {
                messageDesc.Body += origin;
            }
            else if (name.StartsWith("cs:"))
            {
                var op = name[3..];
                if (op == "gid")
                {
                    messageDesc.Type = MessageType.Group;
                    messageDesc.SubjectId = Convert.ToInt64(args);
                }
                else if (op == "gn")
                {
                    messageDesc.SubjectName = args.UnEscapeMiraiCode();
                }
                else if (op == "id")
                {
                    messageDesc.SenderId = Convert.ToInt64(args);
                }
                else if (op == "n")
                {
                    messageDesc.SenderName = args.UnEscapeMiraiCode();
                }
                else if (op == "mid")
                {
                    messageDesc.Id = Convert.ToInt32(args);
                }
                else if (op == "gl")
                {
                    messageDesc.Level = (GroupLevel)Convert.ToInt32(args);
                }
                else if (op == "quote")
                {
                    messageDesc.Body += origin;
                }
            }
        });
        messageDesc.Body = messageDesc.Body.UnEscapeMiraiCode().Trim();
        messageDesc.Chain = MessageBuilder.EvalCsCode(messageDesc.Body).Build();
        return messageDesc;
    }

    private static void ForEachCSCode(string s, Action<string, string?, string> block)
    {
        var pos = 0;
        var lastPos = 0;
        var len = s.Length - 4;

        int findEnding(int start)
        {
            var pos0 = start;
            while (pos0 < s.Length)
                switch (s[pos0])
                {
                    case '\\':
                        pos0 += 2;
                        break;
                    case ']': return pos0;
                    default:
                        pos0++;
                        break;
                }

            return -1;
        }

        while (pos < len)
            switch (s[pos])
            {
                case '\\':
                {
                    pos += 2;
                }
                    break;
                case '[':
                {
                    if (s[pos + 1] == 'm' && s[pos + 2] == 'i' &&
                        s[pos + 3] == 'r' && s[pos + 4] == 'a' &&
                        s[pos + 5] == 'i' && s[pos + 6] == ':')
                    {
                        var begin = pos;
                        pos += 7;
                        var ending = findEnding(pos);
                        if (ending == -1)
                        {
                            block(s[lastPos..], null, "");
                            return;
                        }
                        else
                        {
                            if (lastPos < begin)
                                block(s.Substring(lastPos, begin - lastPos), null, "");
                            var v = s.Substring(begin, ending + 1 - begin);
                            var splitter = v.IndexOf(':', 7);
                            block(
                                v, splitter == -1 ? v.Substring(7, v.Length - 1 - 7) : v.Substring(7, splitter - 7),
                                splitter == -1 ? "" : v.Substring(splitter + 1, v.Length - 1 - (splitter + 1))
                            );
                            lastPos = ending + 1;
                            pos = lastPos;
                        }
                    }
                    else if (s[pos + 1] == 'c' && s[pos + 2] == 's' && s[pos + 3] == ':')
                    {
                        var begin = pos;
                        pos += 4;
                        var ending = findEnding(pos);
                        if (ending == -1)
                        {
                            block(s.Substring(lastPos), null, "");
                            return;
                        }
                        else
                        {
                            if (lastPos < begin)
                                block(s.Substring(lastPos, begin - lastPos), null, "");
                            var v = s.Substring(begin, ending + 1 - begin);
                            var splitter = v.IndexOf(':', 4);
                            block(v,
                                "cs:" + (splitter == -1 ? v.Substring(4, v.Length - 1) : v.Substring(4, splitter - 4)),
                                splitter == -1 ? "" : v.Substring(splitter + 1, v.Length - 1 - (splitter + 1))
                            );
                            lastPos = ending + 1;
                            pos = lastPos;
                        }
                    }
                    else
                    {
                        pos++;
                    }
                }
                    break;
                default:
                {
                    pos++;
                }
                    break;
            }

        if (lastPos < s.Length)
            block(s[lastPos..], null, "");
    }
}