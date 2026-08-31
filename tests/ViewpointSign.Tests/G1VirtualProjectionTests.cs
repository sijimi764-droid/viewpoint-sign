using ViewpointSign.Geometry;
using ViewpointSign.Projection;
using Xunit;

namespace ViewpointSign.Tests;

public sealed class G1VirtualProjectionTests
{
    private static readonly ToleranceProfile Tol = ToleranceProfile.G0;
    private static readonly Point3D Viewer = new(0, 0, 1500);
    private static readonly TargetPlane Target = new(
        new Point3D(0, 1000, 1500),
        new Vector3D(1, 0, 0),
        new Vector3D(0, 0, 1),
        Tol);

    [Fact]
    public void Test01_SinglePlane_ReconstructsGAWithoutGap()
    {
        var surfaces = new[]
        {
            Surface("S1", new Point3D(-600, 1500, 900), new Vector3D(1, 0, 0), 1200, 1200)
        };

        var result = VirtualProjector.Project(Viewer, Target, GaGlyphs(), surfaces, Tol);

        AssertPassingCase(result);
        Assert.Single(result.OwnershipRegions);
        Assert.All(result.Fragments, f => Assert.Equal("S1", f.SurfaceId));
    }

    [Fact]
    public void Test02_ThreeParallelDepthPlanes_ReconstructGAWithCorrectOwnership()
    {
        var surfaces = new[]
        {
            Surface("S1", new Point3D(-700, 1400, 950), new Vector3D(1, 0, 0), 650, 1100),
            Surface("S2", new Point3D(-300, 1600, 900), new Vector3D(1, 0, 0), 700, 1200),
            Surface("S3", new Point3D(100, 1800, 850), new Vector3D(1, 0, 0), 700, 1300)
        };

        var result = VirtualProjector.Project(Viewer, Target, GaGlyphs(), surfaces, Tol);

        AssertPassingCase(result);
        Assert.Equal(3, result.Fragments.Select(f => f.SurfaceId).Distinct().Count());
    }

    [Fact]
    public void Test03_DifferentlyOrientedPlanes_ReconstructGA()
    {
        var surfaces = new[]
        {
            Surface("wall-left", new Point3D(-750, 1500, 900), new Vector3D(1, 0.20, 0), 700, 1200),
            Surface("rack-mid", new Point3D(-300, 1700, 900), new Vector3D(1, 0, 0), 700, 1200),
            Surface("column-right", new Point3D(50, 1900, 900), new Vector3D(1, -0.20, 0), 700, 1200)
        };

        var result = VirtualProjector.Project(Viewer, Target, GaGlyphs(), surfaces, Tol);

        AssertPassingCase(result);
        Assert.Equal(3, result.Fragments.Select(f => f.SurfaceId).Distinct().Count());
    }

    private static void AssertPassingCase(VirtualProjectionResult result)
    {
        Assert.NotEmpty(result.Fragments);
        Assert.InRange(result.GapGeometry.Area, 0, 1e-6);
        Assert.InRange(result.MaxRoundTripErrorMm, 0, Tol.ReconstructedContourToleranceMm);

        var missing = result.TargetGeometry.Difference(result.ReconstructedGeometry).Area;
        var extra = result.ReconstructedGeometry.Difference(result.TargetGeometry).Area;
        Assert.InRange(missing, 0, 1e-5);
        Assert.InRange(extra, 0, 1e-5);

        var ownership = result.OwnershipRegions.Values.ToArray();
        for (var i = 0; i < ownership.Length; i++)
            for (var j = i + 1; j < ownership.Length; j++)
                Assert.InRange(ownership[i].Intersect(ownership[j]).Area, 0, 1e-6);

        foreach (var fragment in result.Fragments)
        {
            Assert.True(fragment.LocalArtwork.Area > 0);
            foreach (var polygon in fragment.LocalArtwork.Polygons)
            {
                Assert.All(polygon.Shell, AssertFinite);
                foreach (var hole in polygon.Holes) Assert.All(hole, AssertFinite);
            }
        }
    }

    private static IReadOnlyList<GlyphGeometry> GaGlyphs()
    {
        var gOuter = Rect(-300, -180, 200, 360);
        var gInner = Rect(-250, -120, 90, 240);
        var gOpening = Rect(-160, -40, 80, 80);
        var g = gOuter.Difference(gInner).Difference(gOpening);

        var a = new MultiPolygon2D(new[]
        {
            new Polygon2D(
                new[]
                {
                    new Point2D(-60, -180),
                    new Point2D(100, 180),
                    new Point2D(260, -180)
                },
                new IReadOnlyList<Point2D>[]
                {
                    new[]
                    {
                        new Point2D(65, -70),
                        new Point2D(100, 20),
                        new Point2D(140, -70)
                    }
                })
        });

        return new[]
        {
            new GlyphGeometry("G", g),
            new GlyphGeometry("A", a)
        };
    }

    private static RectSurface3D Surface(
        string id,
        Point3D origin,
        Vector3D horizontal,
        double width,
        double height) => new(
            id,
            origin,
            horizontal,
            new Vector3D(0, 0, 1),
            width,
            height,
            Tol);

    private static MultiPolygon2D Rect(double x, double y, double width, double height) => new(new[]
    {
        new Polygon2D(new[]
        {
            new Point2D(x, y),
            new Point2D(x + width, y),
            new Point2D(x + width, y + height),
            new Point2D(x, y + height)
        })
    });

    private static void AssertFinite(Point2D p)
    {
        Assert.True(double.IsFinite(p.X));
        Assert.True(double.IsFinite(p.Y));
    }
}
