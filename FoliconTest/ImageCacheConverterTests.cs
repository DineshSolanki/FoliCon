#nullable enable
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using FoliCon.Modules.Convertor;

namespace FoliconTest;

public class ImageCacheConverterTests
{
    [Fact]
    public void Convert_NullOrEmpty_ReturnsNull()
    {
        var converter = new ImageCacheConverter();

        Assert.Null(converter.Convert(null!, typeof(object), null!, CultureInfo.InvariantCulture));
        Assert.Null(converter.Convert(string.Empty, typeof(object), null!, CultureInfo.InvariantCulture));
        Assert.Null(converter.Convert("   ", typeof(object), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Convert_NonExistentFile_ReturnsNull()
    {
        var converter = new ImageCacheConverter();

        var result = converter.Convert(@"C:\non_existent_folder_xyz\image.png", typeof(object), null!, CultureInfo.InvariantCulture);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_ValidLocalFile_ReturnsImageSourceAndCaches()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"folicon_test_{Guid.NewGuid():N}.png");
        try
        {
            using (var bmp = new Bitmap(16, 16))
            {
                using var g = Graphics.FromImage(bmp);
                g.Clear(Color.Blue);
                bmp.Save(tempFile, ImageFormat.Png);
            }

            var converter = new ImageCacheConverter();

            var firstResult = converter.Convert(tempFile, typeof(object), null!, CultureInfo.InvariantCulture);
            Assert.NotNull(firstResult);

            var secondResult = converter.Convert(tempFile, typeof(object), null!, CultureInfo.InvariantCulture);
            Assert.Same(firstResult, secondResult);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
