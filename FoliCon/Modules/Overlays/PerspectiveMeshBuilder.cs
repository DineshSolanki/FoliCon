using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Point = System.Windows.Point;

#nullable enable
namespace FoliCon.Modules.Overlays;

/// <summary>
/// Computes 4-corner projective homography mapping and builds perspective-warped
/// 3D meshes for poster icon overlays.
/// </summary>
public static class PerspectiveMeshBuilder
{
    private const int GridSubdivision = 32;

    /// <summary>
    /// Attempts to parse 4 corner points from a string: "x0,y0 x1,y1 x2,y2 x3,y3"
    /// (TopLeft, TopRight, BottomRight, BottomLeft).
    /// </summary>
    public static bool TryParseCorners(string? cornersStr, out Point[] corners)
    {
        corners = [];
        if (string.IsNullOrWhiteSpace(cornersStr))
        {
            return false;
        }

        var parts = cornersStr.Split([' ', ';', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
        {
            return false;
        }

        var points = new Point[4];
        for (var i = 0; i < 4; i++)
        {
            var coords = parts[i].Split(',');
            if (coords.Length != 2 ||
                !double.TryParse(coords[0].Trim(), CultureInfo.InvariantCulture, out var x) ||
                !double.TryParse(coords[1].Trim(), CultureInfo.InvariantCulture, out var y))
            {
                return false;
            }

            points[i] = new Point(x, y);
        }

        if (!IsConvexQuad(points))
        {
            return false;
        }

        corners = points;
        return true;
    }

    /// <summary>
    /// Formats 4 corner points as a standardized string: "x0,y0 x1,y1 x2,y2 x3,y3".
    /// </summary>
    public static string FormatCorners(Point[] corners)
    {
        if (corners.Length != 4)
        {
            throw new ArgumentException("Must contain exactly 4 corner points.", nameof(corners));
        }

        return string.Format(CultureInfo.InvariantCulture,
            "{0:0.##},{1:0.##} {2:0.##},{3:0.##} {4:0.##},{5:0.##} {6:0.##},{7:0.##}",
            corners[0].X, corners[0].Y,
            corners[1].X, corners[1].Y,
            corners[2].X, corners[2].Y,
            corners[3].X, corners[3].Y);
    }

    /// <summary>
    /// Checks whether 4 points form a non-degenerate, strictly convex quadrilateral in clockwise or counter-clockwise order.
    /// </summary>
    public static bool IsConvexQuad(Point[] pts)
    {
        if (pts.Length != 4)
        {
            return false;
        }

        var sign = 0;
        for (var i = 0; i < 4; i++)
        {
            var p0 = pts[i];
            var p1 = pts[(i + 1) % 4];
            var p2 = pts[(i + 2) % 4];

            var crossProduct = ((p1.X - p0.X) * (p2.Y - p1.Y)) - ((p1.Y - p0.Y) * (p2.X - p1.X));
            if (Math.Abs(crossProduct) < 1e-6)
            {
                return false; // Collinear edges
            }

            var currentSign = crossProduct > 0 ? 1 : -1;
            if (sign == 0)
            {
                sign = currentSign;
            }
            else if (sign != currentSign)
            {
                return false; // Concave or self-intersecting
            }
        }

        return true;
    }

    /// <summary>
    /// Computes the 3x3 projective homography matrix mapping unit square [0,1]x[0,1]
    /// (TopLeft=(0,0), TopRight=(1,0), BottomRight=(1,1), BottomLeft=(0,1))
    /// to 4 arbitrary destination points (p0, p1, p2, p3).
    /// </summary>
    public static double[]? ComputeUnitSquareToQuadHomography(Point p0, Point p1, Point p2, Point p3)
    {
        var x0 = p0.X; var y0 = p0.Y;
        var x1 = p1.X; var y1 = p1.Y;
        var x2 = p2.X; var y2 = p2.Y;
        var x3 = p3.X; var y3 = p3.Y;

        var dx1 = x1 - x2;
        var dx2 = x3 - x2;
        var sx = x0 - x1 + x2 - x3;
        var dy1 = y1 - y2;
        var dy2 = y3 - y2;
        var sy = y0 - y1 + y2 - y3;

        double h00, h01, h02, h10, h11, h12, h20, h21, h22;

        if (Math.Abs(sx) < 1e-9 && Math.Abs(sy) < 1e-9)
        {
            // Affine / Parallelogram mapping
            h00 = x1 - x0;
            h01 = x3 - x0;
            h02 = x0;
            h10 = y1 - y0;
            h11 = y3 - y0;
            h12 = y0;
            h20 = 0;
            h21 = 0;
            h22 = 1;
        }
        else
        {
            var det = (dx1 * dy2) - (dx2 * dy1);
            if (Math.Abs(det) < 1e-9)
            {
                return null;
            }

            h20 = ((sx * dy2) - (sy * dx2)) / det;
            h21 = ((dx1 * sy) - (dy1 * sx)) / det;
            h22 = 1;
            h00 = x1 - x0 + (h20 * x1);
            h01 = x3 - x0 + (h21 * x3);
            h02 = x0;
            h10 = y1 - y0 + (h20 * y1);
            h11 = y3 - y0 + (h21 * y3);
            h12 = y0;
        }

        return [h00, h01, h02, h10, h11, h12, h20, h21, h22];
    }

    /// <summary>
    /// Evaluates the projective homography at (u, v) in [0, 1].
    /// </summary>
    public static Point ProjectPoint(double[] h, double u, double v)
    {
        var w = (h[6] * u) + (h[7] * v) + h[8];
        if (Math.Abs(w) < 1e-9)
        {
            w = 1e-9;
        }

        var x = ((h[0] * u) + (h[1] * v) + h[2]) / w;
        var y = ((h[3] * u) + (h[4] * v) + h[5]) / w;
        return new Point(x, y);
    }

    /// <summary>
    /// Builds a subdivided MeshGeometry3D for the given 4 corners using projective homography.
    /// </summary>
    public static MeshGeometry3D? BuildPerspectiveMesh(Point[] corners, int subdivisions = GridSubdivision)
    {
        if (corners.Length != 4)
        {
            return null;
        }

        var h = ComputeUnitSquareToQuadHomography(corners[0], corners[1], corners[2], corners[3]);
        if (h == null)
        {
            return null;
        }

        var mesh = new MeshGeometry3D();
        var n = Math.Max(2, subdivisions);

        // Generate vertices
        for (var j = 0; j <= n; j++)
        {
            var v = (double)j / n;
            for (var i = 0; i <= n; i++)
            {
                var u = (double)i / n;
                var pt = ProjectPoint(h, u, v);

                mesh.Positions.Add(new Point3D(pt.X, pt.Y, 0));
                mesh.TextureCoordinates.Add(new Point(u, v));
            }
        }

        // Generate triangles
        for (var j = 0; j < n; j++)
        {
            for (var i = 0; i < n; i++)
            {
                var tl = (j * (n + 1)) + i;
                var tr = tl + 1;
                var bl = ((j + 1) * (n + 1)) + i;
                var br = bl + 1;

                // Triangle 1
                mesh.TriangleIndices.Add(tl);
                mesh.TriangleIndices.Add(tr);
                mesh.TriangleIndices.Add(bl);

                // Triangle 2
                mesh.TriangleIndices.Add(tr);
                mesh.TriangleIndices.Add(br);
                mesh.TriangleIndices.Add(bl);
            }
        }

        mesh.Freeze();
        return mesh;
    }

    /// <summary>
    /// Creates a Viewport3D element configured to render a perspective-warped poster image.
    /// </summary>
    public static Viewport3D? CreatePerspectivePosterElement(
        Point[] corners,
        ImageSource imageSource,
        double designWidth,
        double designHeight)
    {
        var mesh = BuildPerspectiveMesh(corners);
        if (mesh == null)
        {
            return null;
        }

        var imageBrush = new ImageBrush(imageSource)
        {
            Stretch = Stretch.Fill
        };
        RenderOptions.SetBitmapScalingMode(imageBrush, BitmapScalingMode.HighQuality);

        var material = new DiffuseMaterial(imageBrush);
        var geometryModel = new GeometryModel3D(mesh, material)
        {
            BackMaterial = material
        };

        var modelGroup = new Model3DGroup();
        modelGroup.Children.Add(new AmbientLight(Colors.White));
        modelGroup.Children.Add(geometryModel);

        var visual = new ModelVisual3D
        {
            Content = modelGroup
        };

        var camera = new OrthographicCamera
        {
            Position = new Point3D(designWidth / 2.0, designHeight / 2.0, 100.0),
            LookDirection = new Vector3D(0, 0, -1),
            UpDirection = new Vector3D(0, -1, 0),
            Width = designWidth
        };

        var viewport = new Viewport3D
        {
            Width = designWidth,
            Height = designHeight,
            Camera = camera,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        RenderOptions.SetBitmapScalingMode(viewport, BitmapScalingMode.HighQuality);
        viewport.Children.Add(visual);

        return viewport;
    }
}
