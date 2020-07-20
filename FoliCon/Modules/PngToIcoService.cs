using System.Drawing;
using System.Drawing.IconLib;

namespace FoliCon.Modules
{
    public static class PngToIcoService
    {
        public static void Convert(Bitmap bitmap, string icoPath)
        {
            MultiIcon mIcon = new MultiIcon();
            mIcon.Add("Untitled").CreateFrom(bitmap, IconOutputFormat.Vista);
            mIcon.SelectedIndex = 0;
            mIcon.Save(icoPath, MultiIconFormat.ICO);
        }
    }
}
