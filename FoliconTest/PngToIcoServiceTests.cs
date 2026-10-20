#nullable enable
using System.IO;
using FoliCon.Modules.Media;

namespace FoliconTest;

public class PngToIcoServiceTests
{
    [Fact]
    public void TransparentMark_CreatesValidTransparentIco()
    {
        var markPath = Path.GetFullPath(@"..\..\..\..\FoliCon\Resources\icons\folicon Icon.png");
        Assert.True(File.Exists(markPath));
        var tempIcoPath = Path.Combine(Path.GetTempPath(), $"folicon_test_{Guid.NewGuid():N}.ico");
        try
        {
            using var bmp = new System.Drawing.Bitmap(markPath);
            PngToIcoService.Convert(bmp, tempIcoPath);
            Assert.True(File.Exists(tempIcoPath));
            var fi = new FileInfo(tempIcoPath);
            Assert.True(fi.Length > 0);
        }
        finally
        {
            if (File.Exists(tempIcoPath))
            {
                File.Delete(tempIcoPath);
            }
        }
    }
}
