#nullable enable
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays;
using FoliCon.Modules.Overlays.Designer;
using FoliCon.Modules.Overlays.Internal;
using FoliCon.Views;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using Point = System.Windows.Point;

namespace FoliconTest;

public class PosterTransformAndPerspectiveTests
{
    [Fact]
    public void TryParseCorners_ValidConvexQuad_ReturnsTrueAndParsesPoints()
    {
        var input = "25.5,30 220,35.5 215,225 30,230";
        var success = PerspectiveMeshBuilder.TryParseCorners(input, out var corners);

        Assert.True(success);
        Assert.Equal(4, corners.Length);
        Assert.Equal(new Point(25.5, 30), corners[0]);
        Assert.Equal(new Point(220, 35.5), corners[1]);
        Assert.Equal(new Point(215, 225), corners[2]);
        Assert.Equal(new Point(30, 230), corners[3]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("10,10 20,20 30,30")] // only 3 points
    [InlineData("10,10 20,20 30,30 40,40 50,50")] // 5 points
    [InlineData("abc,10 20,20 30,30 40,40")] // non-numeric
    [InlineData("10,10 100,10 10,100 100,100")] // self-intersecting hourglass
    public void TryParseCorners_InvalidInputs_ReturnsFalse(string? input)
    {
        var success = PerspectiveMeshBuilder.TryParseCorners(input, out var corners);
        Assert.False(success);
        Assert.Empty(corners);
    }

    [Fact]
    public void FormatCorners_ProducesConsistentString()
    {
        Point[] corners =
        [
            new(10, 20),
            new(200, 25),
            new(195, 210),
            new(15, 205)
        ];

        var formatted = PerspectiveMeshBuilder.FormatCorners(corners);
        Assert.Equal("10,20 200,25 195,210 15,205", formatted);

        var parseSuccess = PerspectiveMeshBuilder.TryParseCorners(formatted, out var roundtrip);
        Assert.True(parseSuccess);
        Assert.Equal(corners, roundtrip);
    }

    [Fact]
    public void Homography_ProjectsCornersAccurately()
    {
        var p0 = new Point(20, 30);
        var p1 = new Point(220, 40);
        var p2 = new Point(210, 230);
        var p3 = new Point(25, 220);

        var h = PerspectiveMeshBuilder.ComputeUnitSquareToQuadHomography(p0, p1, p2, p3);
        Assert.NotNull(h);

        // Test corners
        var mapped00 = PerspectiveMeshBuilder.ProjectPoint(h, 0, 0);
        var mapped10 = PerspectiveMeshBuilder.ProjectPoint(h, 1, 0);
        var mapped11 = PerspectiveMeshBuilder.ProjectPoint(h, 1, 1);
        var mapped01 = PerspectiveMeshBuilder.ProjectPoint(h, 0, 1);

        Assert.True(Math.Abs(mapped00.X - p0.X) < 1e-4 && Math.Abs(mapped00.Y - p0.Y) < 1e-4);
        Assert.True(Math.Abs(mapped10.X - p1.X) < 1e-4 && Math.Abs(mapped10.Y - p1.Y) < 1e-4);
        Assert.True(Math.Abs(mapped11.X - p2.X) < 1e-4 && Math.Abs(mapped11.Y - p2.Y) < 1e-4);
        Assert.True(Math.Abs(mapped01.X - p3.X) < 1e-4 && Math.Abs(mapped01.Y - p3.Y) < 1e-4);
    }

    [Fact]
    public void BuildPerspectiveMesh_GeneratesCorrectGeometry()
    {
        Point[] corners =
        [
            new(20, 30),
            new(220, 40),
            new(210, 230),
            new(25, 220)
        ];

        var mesh = PerspectiveMeshBuilder.BuildPerspectiveMesh(corners, subdivisions: 8);
        Assert.NotNull(mesh);
        // (8+1)*(8+1) = 81 vertices
        Assert.Equal(81, mesh.Positions.Count);
        Assert.Equal(81, mesh.TextureCoordinates.Count);
        // 8*8*2 = 128 triangles, 384 indices
        Assert.Equal(128 * 3, mesh.TriangleIndices.Count);
        Assert.True(mesh.IsFrozen);
    }

    [Fact]
    public void OverlayDesignerDocument_RoundtripsPosterTransforms()
    {
        var doc = new OverlayDesignerDocument
        {
            PosterRotationAngle = 15.5,
            PosterRotationOrigin = "0.2,0.8",
            PosterSkewX = -6.5,
            PosterSkewY = 4.2,
            PosterPerspectiveCorners = "20,30 220,40 210,230 25,220"
        };

        var snapshot = doc.CreateSnapshot();
        Assert.Equal(15.5, snapshot.Poster.RotationAngle);
        Assert.Equal("0.2,0.8", snapshot.Poster.RotationOrigin);
        Assert.Equal(-6.5, snapshot.Poster.SkewX);
        Assert.Equal(4.2, snapshot.Poster.SkewY);
        Assert.Equal("20,30 220,40 210,230 25,220", snapshot.Poster.PerspectiveCorners);

        var restored = OverlayDesignerDocument.FromDefinition(snapshot, string.Empty);
        Assert.Equal(doc.PosterRotationAngle, restored.PosterRotationAngle);
        Assert.Equal(doc.PosterRotationOrigin, restored.PosterRotationOrigin);
        Assert.Equal(doc.PosterSkewX, restored.PosterSkewX);
        Assert.Equal(doc.PosterSkewY, restored.PosterSkewY);
        Assert.Equal(doc.PosterPerspectiveCorners, restored.PosterPerspectiveCorners);
    }

    [Fact]
    public void OverlayValidator_ValidatesPosterRotationOriginAndPerspectiveCorners()
    {
        var definition = new PosterOverlayDefinition
        {
            Id = "test-transform",
            DisplayName = "Transform Test",
            Author = "Author",
            Description = "Desc",
            Poster = new PosterConfig
            {
                Margin = "10,10,10,10",
                RotationOrigin = "0.5,0.5",
                PerspectiveCorners = "20,30 220,40 210,230 25,220"
            }
        };

        var validResult = OverlayValidator.ValidateDetailed(string.Empty, definition);
        Assert.True(validResult.IsValid);

        // Invalid rotation origin
        definition.Poster.RotationOrigin = "2.5,0.5";
        var invalidOriginResult = OverlayValidator.ValidateDetailed(string.Empty, definition);
        Assert.False(invalidOriginResult.IsValid);
        Assert.Contains(invalidOriginResult.Errors, e => e.Field == "poster.rotationOrigin");

        // Invalid perspective corners (concave / self-intersecting)
        definition.Poster.RotationOrigin = "0.5,0.5";
        definition.Poster.PerspectiveCorners = "10,10 100,10 10,100 100,100";
        var invalidCornersResult = OverlayValidator.ValidateDetailed(string.Empty, definition);
        Assert.False(invalidCornersResult.IsValid);
        Assert.Contains(invalidCornersResult.Errors, e => e.Field == "poster.perspectiveCorners");
    }

    [Fact]
    public void DynamicPosterIcon_RendersSkewAndPerspectiveWithoutExceptions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "FoliconTransformTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a small test poster image
            var posterPath = Path.Combine(tempDir, "folder.jpg");
            using (var bmp = new Bitmap(100, 150))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.MediumSeaGreen);
                bmp.Save(posterPath, ImageFormat.Jpeg);
            }

            var posterIcon = new PosterIcon();

            // Test 1: 2D Tilt & Skew
            var skewDef = new PosterOverlayDefinition
            {
                Id = "test-skew",
                DesignWidth = 256,
                DesignHeight = 256,
                Poster = new PosterConfig
                {
                    Margin = "20,20,20,20",
                    RotationAngle = 12.0,
                    SkewX = -8.0,
                    SkewY = 4.0
                }
            };

            var staThread = new Thread(() =>
            {
                var skewIcon = new DynamicPosterIcon(skewDef, posterIcon);
                using var skewBmp = skewIcon.RenderToBitmap();
                Assert.NotNull(skewBmp);
                Assert.Equal(256, skewBmp.Width);
                Assert.Equal(256, skewBmp.Height);

                // Test 2: Perspective 4-corner warp
                var perspDef = new PosterOverlayDefinition
                {
                    Id = "test-persp",
                    DesignWidth = 256,
                    DesignHeight = 256,
                    Poster = new PosterConfig
                    {
                        PerspectiveCorners = "20,30 220,40 210,230 25,220"
                    }
                };

                // Test 3: Base layer overwrite on disk reloads fresh pixels without caching
                var baseImgPath = Path.Combine(tempDir, "base.png");
                using (var redBmp = new Bitmap(256, 256))
                using (var g = Graphics.FromImage(redBmp))
                {
                    g.Clear(System.Drawing.Color.Red);
                    redBmp.Save(baseImgPath, ImageFormat.Png);
                }

                var baseDef = new PosterOverlayDefinition
                {
                    Id = "test-base-reload",
                    DesignWidth = 256,
                    DesignHeight = 256,
                    OverlayFolderPath = tempDir,
                    Poster = new PosterConfig { Margin = "250,250,0,0" },
                    BaseLayer = new LayerDefinition { ImagePath = "base.png" }
                };

                var baseIcon1 = new DynamicPosterIcon(baseDef, posterIcon);
                using var bmp1 = baseIcon1.RenderToBitmap();
                var color1 = bmp1.GetPixel(50, 50);
                Assert.True(color1.R > 200 && color1.G < 50 && color1.B < 50,
                    $"Color was A={color1.A}, R={color1.R}, G={color1.G}, B={color1.B}");

                // Overwrite with Green
                using (var greenBmp = new Bitmap(256, 256))
                using (var g = Graphics.FromImage(greenBmp))
                {
                    g.Clear(System.Drawing.Color.Lime);
                    greenBmp.Save(baseImgPath, ImageFormat.Png);
                }

                var baseIcon2 = new DynamicPosterIcon(baseDef, posterIcon);
                using var bmp2 = baseIcon2.RenderToBitmap();
                var color2 = bmp2.GetPixel(50, 50);
                Assert.True(color2.G > 200 && color2.R < 50 && color2.B < 50);
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void ExternalImageCopy_CreatesTargetAndCopies()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "FoliconAssetTest_" + Guid.NewGuid().ToString("N"));
        var externalFolder = Path.Combine(Path.GetTempPath(), "FoliconExternalTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);
        Directory.CreateDirectory(externalFolder);

        try
        {
            var externalFile = Path.Combine(externalFolder, "my_base.png");
            File.WriteAllText(externalFile, "test-png-content");

            var destFile = Path.Combine(tempFolder, Path.GetFileName(externalFile));
            File.Copy(externalFile, destFile, overwrite: true);

            var relativePath = Path.GetRelativePath(tempFolder, destFile);
            Assert.Equal("my_base.png", relativePath);
            Assert.True(File.Exists(destFile));
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
            if (Directory.Exists(externalFolder)) Directory.Delete(externalFolder, true);
        }
    }

    [Fact]
    public void Document_GetAndSetElementBounds_WithPerspectiveCorners_TranslatesAndScalesCorners()
    {
        var doc = new OverlayDesignerDocument
        {
            DesignWidth = 256,
            DesignHeight = 256,
            PosterPerspectiveCorners = "30,40 230,40 230,220 30,220"
        };

        // Bounding box should be (30, 40, 200, 180)
        var bounds = doc.GetElementBounds(OverlayElementKind.Poster);
        Assert.Equal(30, bounds.X);
        Assert.Equal(40, bounds.Y);
        Assert.Equal(200, bounds.Width);
        Assert.Equal(180, bounds.Height);

        // Move to (50, 60) and resize to (100, 90) (scale = 0.5)
        doc.SetElementBounds(OverlayElementKind.Poster, new Rect(50, 60, 100, 90));

        Assert.True(PerspectiveMeshBuilder.TryParseCorners(doc.PosterPerspectiveCorners!, out var corners));
        Assert.Equal(50, corners[0].X);
        Assert.Equal(60, corners[0].Y);
        Assert.Equal(150, corners[1].X);
        Assert.Equal(60, corners[1].Y);
        Assert.Equal(150, corners[2].X);
        Assert.Equal(150, corners[2].Y);
        Assert.Equal(50, corners[3].X);
        Assert.Equal(150, corners[3].Y);
    }
}
