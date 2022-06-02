using System.Collections;
using System.Text.RegularExpressions;

using AimuBot.Core.Message.Model;

namespace AimuBot.Core.Message;

public class MessageBuilder : IEnumerable
{
    private MessageChain _chain;

    public MessageBuilder()
    {
        _chain = new MessageChain();
    }

    public MessageBuilder(params BaseChain[] chains)
    {
        _chain = new MessageChain(chains);
    }

    public MessageBuilder(IEnumerable<BaseChain> chains)
    {
        _chain = new MessageChain();
        _chain.AddRange(chains);
    }

    public MessageBuilder(string text)
    {
        _chain = new MessageChain();
        Text(text);
    }

    public IEnumerator GetEnumerator()
        => _chain.GetEnumerator();

    public MessageChain Build()
    {
        TextChain last = null;
        var chain = new MessageChain();

        // Scan chains
        foreach (var i in _chain.Chains)
        {
            // If found a singleton chain
            if (i.Mode == BaseChain.ChainMode.Singleton)

                // Then drop other chains
                return new MessageChain(_chain[BaseChain.ChainMode.Singletag].FirstOrDefault(), i);

            // Combine text chains
            //////////////////////

            // Just append if not text chain
            if (i.Type != BaseChain.ChainType.Text)
            {
                last = null;
                chain.Add(i);
                continue;
            }

            // Combine with last text chain
            if (last != null)
            {
                last.Combine((TextChain)i);
            }
            else
            {
                chain.Add(i);

                // Keep last text chain
                last = (TextChain)i;
            }
        }

        return _chain = chain;
    }

    public static MessageBuilder EvalKqCode(string kqCode)
    {
        var builder = new MessageBuilder();
        {
            var regexp = new Regex
                (@"\[KQ:(at|image|flash|face|bface|record|video|reply|json|xml).*?\]");

            // Match pattern
            var matches = regexp.Matches(kqCode);

            if (matches.Count != 0)
            {
                var textIndex = 0;

                // Process each code
                foreach (Match i in matches)
                {
                    if (i.Index != textIndex) builder.Text(kqCode[textIndex..i.Index]);

                    // Convert the code to a chain
                    BaseChain? chain = i.Groups[1].Value switch
                    {
                        "at"    => AtChain.ParseKqCode(i.Value),
                        "image" => ImageChain.ParseKqCode(i.Value),

                        //"flash" => FlashImageChain.ParseKqCode(i.Value),
                        //"face" => QFaceChain.ParseKqCode(i.Value),
                        //"bface" => BFaceChain.ParseKqCode(i.Value),
                        //"record" => RecordChain.ParseKqCode(i.Value),
                        //"video" => VideoChain.ParseKqCode(i.Value),
                        "reply" => ReplyChain.ParseKqCode(i.Value),

                        //"json" => JsonChain.ParseKqCode(i.Value),
                        //"xml" => XmlChain.ParseKqCode(i.Value),
                        _ => null
                    };

                    // Add new chain
                    if (chain != null) builder.Add(chain);

                    // Update index
                    textIndex = i.Index + i.Length;
                }

                // Process the suffix
                if (textIndex != kqCode.Length) builder.Text(kqCode[textIndex..kqCode.Length]);
            }

            // No code included
            else
            {
                builder.Text(kqCode);
            }
        }

        return builder;
    }

    public static MessageBuilder EvalCsCode(string csCode)
    {
        var builder = new MessageBuilder();
        {
            var regexp = new Regex
                (@"\[(cs|mirai):(at|image|reply):.*?\]");

            // Match pattern
            var matches = regexp.Matches(csCode);

            if (matches.Count != 0)
            {
                var textIndex = 0;

                // Process each code
                foreach (Match i in matches)
                {
                    if (i.Index != textIndex) builder.Text(csCode[textIndex..i.Index]);

                    // Convert the code to a chain
                    BaseChain? chain = i.Groups[1].Value switch
                    {
                        "at"    => AtChain.ParseCsCode(i.Value),
                        "image" => ImageChain.ParseCsCode(i.Value),

                        //"flash" => FlashImageChain.ParseKqCode(i.Value),
                        //"face" => QFaceChain.ParseKqCode(i.Value),
                        //"bface" => BFaceChain.ParseKqCode(i.Value),
                        //"record" => RecordChain.ParseKqCode(i.Value),
                        //"video" => VideoChain.ParseKqCode(i.Value),
                        "reply" => ReplyChain.ParseCsCode(i.Value),

                        //"json" => JsonChain.ParseKqCode(i.Value),
                        //"xml" => XmlChain.ParseKqCode(i.Value),
                        _ => null
                    };

                    // Add new chain
                    if (chain != null) builder.Add(chain);

                    // Update index
                    textIndex = i.Index + i.Length;
                }

                // Process the suffix
                if (textIndex != csCode.Length) builder.Text(csCode[textIndex..csCode.Length]);
            }

            // No code included
            else
            {
                builder.Text(csCode);
            }
        }

        return builder;
    }

    public MessageBuilder Add(BaseChain chain)
    {
        _chain.Add(chain);
        return this;
    }

    public MessageBuilder Add(IEnumerable<BaseChain> chain)
    {
        _chain.AddRange(chain);
        return this;
    }

    public MessageBuilder Text(string message)
    {
        _chain.Add(TextChain.Create(message));
        return this;
    }

    public MessageBuilder At(uint uin)
    {
        _chain.Add(AtChain.Create(uin));
        return this;
    }

    //public MessageBuilder QFace(int faceId)
    //{
    //    _chain.Add(QFaceChain.Create(faceId));
    //    return this;
    //}

    //public MessageBuilder Image(byte[] data)
    //{
    //    _chain.Add(ImageChain.Create(data));
    //    return this;
    //}

    //public MessageBuilder Image(string filePath)
    //{
    //    _chain.Chains.Add(ImageChain.CreateFromFile(filePath));
    //    return this;
    //}

    //public MessageBuilder Record(string filePath)
    //{
    //    _chain.Add(RecordChain.CreateFromFile(filePath));
    //    return this;
    //}

    public static MessageBuilder operator +(MessageBuilder x, MessageBuilder y)
    {
        var z = new MessageBuilder();
        {
            z._chain.AddRange(x._chain.Chains);
            z._chain.AddRange(y._chain.Chains);
        }
        return z;
    }
}