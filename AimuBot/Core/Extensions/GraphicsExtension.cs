using System.Drawing;

namespace AimuBot.Core.Extensions;

public static class GraphicsExtension
{
    public static void DrawImageCenterPivot(this Graphics g, Image image, float x, float y) => g.DrawImage(image, x - image.Width / 2, y - image.Height / 2);

    public static void DrawImageFromPivot(this Graphics g, Image image, float x, float y, float px, float py, float dx = 0, float dy = 0) => g.DrawImage(image, x - image.Width * px + dx, y - image.Height * py + dy);

    public static void DrawImageFillInRect(this Graphics g, Image image, float x, float y, int w, int h)
    {
        float rate = (float)image.Width / image.Height;
        float rate_d = (float)w / h;
        if (rate < rate_d)
        {
            float dh = h;
            float dw = h * rate_d;
            g.DrawImage(image, x - (dw - w) / 2, y, dw, dh);
        }
        else
        {
            float dw = w;
            float dh = w / rate_d;
            g.DrawImage(image, x, y - (dh - h) / 2, dw, dh);
        }
    }
}
