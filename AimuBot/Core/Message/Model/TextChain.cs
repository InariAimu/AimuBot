namespace AimuBot.Core.Message.Model;

public class TextChain : BaseChain
{
    public string Content { get; private set; }

    private TextChain(string content)
        : base(ChainType.Text, ChainMode.Multiple)
    {
        Content = content;
    }

    internal void Combine(TextChain chain)
        => Content += chain.Content;

    public static TextChain Create(string text)
        => new(text);

    public override string ToKqCode()
        => Content;

    public override string ToCsCode()
        => Content;

    public override string ToPreviewString()
        => Content.Length > 8 ? Content[..8] + "..." : Content;
}