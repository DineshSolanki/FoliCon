using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FoliCon.Views
{
    /// <summary>
    /// Interaction logic for PosterIconLiaher.xaml
    /// </summary>
    public partial class PosterIconLiaher : UserControl
    {
        public PosterIconLiaher()
        {
            InitializeComponent();
        }

        public PosterIconLiaher(object dataContext)
        {
            DataContext = dataContext;
            InitializeComponent();
        }
        public Bitmap RenderToBitmap()
        {
            return RenderTargetBitmapTo32BppArgb(AsRenderTargetBitmap());
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

        private static Bitmap RenderTargetBitmapTo32BppArgb(BitmapSource rtb)
        {
            var stream = new MemoryStream();
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            encoder.Save(stream);
            return new Bitmap(stream);
        }
    }
}
