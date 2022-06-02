using AimuBot.Core.Utils;

namespace AimuBot.Core.Message.Model;

public enum ImageChainType
{
    Hash,
    LocalFile
}

public class ImageChain : BaseChain
{
    public string Content { get; }

    public ImageChainType ImageChainType { get; } = ImageChainType.LocalFile;

    private ImageChain(string relativePath, ImageChainType imageChainType)
        : base(ChainType.Image, ChainMode.Multiple)
    {
        Content = relativePath;
        ImageChainType = imageChainType;
    }

    public static ImageChain Create(string relativePath, ImageChainType imageChainType = ImageChainType.LocalFile)
        => new(relativePath, imageChainType);

    internal static ImageChain ParseKqCode(string code)
        => throw new NotImplementedException();

    internal static ImageChain ParseCsCode(string code)
        => new(code, ImageChainType.Hash);

    public override string ToKqCode()
        => throw new NotImplementedException();

    public override string ToCsCode()
        => ImageChainType switch
        {
            ImageChainType.LocalFile => $"[cs:image:{BotUtil.CombinePath(Content)}]",
            ImageChainType.Hash      => $"{Content}",
            _                        => Content
        };

    public override string ToPreviewString()
        => "[图片]";
}