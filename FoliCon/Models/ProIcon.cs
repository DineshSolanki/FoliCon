using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace FoliCon.Modules
{
    public class ProIcon
    {
        private string _filePath;

        public ProIcon(string filePath)
        {
            _filePath = filePath;
        }

        public Bitmap RenderToBitmap()
        {
            return RenderTargetBitmapTo32bppArgb(AsRenderTargetBitmap());
        }

        private BitmapSource AsRenderTargetBitmap()
        {
            using (Bitmap img = new Bitmap(_filePath))
            {
                using (var icon = new Bitmap(img, 256, 256))
                {
                    return Util.LoadBitmap(icon);
                }
            }
        }

        private Bitmap RenderTargetBitmapTo32bppArgb(BitmapSource rtb)
        {
            MemoryStream stream = new MemoryStream();
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            encoder.Save(stream);
            return new Bitmap(stream); //png;
        }
    }

}
