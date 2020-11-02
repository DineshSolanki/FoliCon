using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FoliCon.Views
{
    /// <summary>
    /// Interaction logic for PosterIcon
    /// </summary>
    public partial class Page1
    {
        public Page1(object dataContext)
        {
            DataContext = dataContext;
            InitializeComponent();
        }

        public Bitmap RenderToBitmap()
        {
            return RenderTargetBitmapTo32bppArgb(AsRenderTargetBitmap());
        }

        private RenderTargetBitmap AsRenderTargetBitmap()
        {
            var size = new System.Windows.Size(256, 256);
            Measure(size);
            Arrange(new Rect(size));

            var rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Default);
            rtb.Render(this);

            return rtb;
        }

        private Bitmap RenderTargetBitmapTo32bppArgb(RenderTargetBitmap rtb)
        {
            var stream = new MemoryStream();
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            encoder.Save(stream);
            return new Bitmap(stream); //png;
        }
    }
}