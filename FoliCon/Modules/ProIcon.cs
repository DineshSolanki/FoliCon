using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace FoliCon.Modules
{
    public class ProIcon
    {
        private readonly string _filePath;

        public ProIcon(string filePath)
        {
            _filePath = filePath;
        }

        public Bitmap RenderToBitmap()
        {
            return RenderTargetBitmapTo32BppArgb(AsRenderTargetBitmap());
        }

        private BitmapSource AsRenderTargetBitmap()
        {
            using var img = new Bitmap(_filePath);
            using var icon = new Bitmap(img, 256, 256);
            return Util.LoadBitmap(icon);
        }

        private static Bitmap RenderTargetBitmapTo32BppArgb(BitmapSource rtb)
        {
            var stream = new MemoryStream();
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            encoder.Save(stream);
            return new Bitmap(stream); //png;
        }
    }
}