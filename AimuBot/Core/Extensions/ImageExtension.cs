using System.Drawing;
using System.Drawing.Imaging;

namespace AimuBot.Core.Extensions;

public static class ImageExtension
{
    private static ImageCodecInfo? GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageDecoders();

        foreach (var codec in codecs)
        {
            if (codec.FormatID == format.Guid)
                return codec;
        }
        return null;
    }

    public static void SaveToJpg(this Image im, string file, int quality = 100)
    {
        var jpgEncoder = GetEncoder(ImageFormat.Jpeg);

        if (jpgEncoder is null)
        {
            im.Save(file);
        }
        else
        {
            var myEncoder = Encoder.Quality;

            EncoderParameters myEncoderParameters = new EncoderParameters(1);

            myEncoderParameters.Param[0] = new EncoderParameter(myEncoder, quality); ;

            im.Save(file, jpgEncoder, myEncoderParameters);
        }
    }
}
