using ViewpointSign.Geometry;
using Xunit;

namespace ViewpointSign.Tests;

public sealed class G0AdvancedTests
{
    private static readonly ToleranceProfile Tol = ToleranceProfile.G0;

    [Fact]
    public void Homography_RoundTrip_IsWithin001Millimetre()
    {
        var target = new[] { new Point2D(0,0), new Point2D(100,0), new Point2D(100,100), new Point2D(0,100) };
        var surface = new[] { new Point2D(10,20), new Point2D(180,30), new Point2D(160,140), new Point2D(20,120) };
        var forward = Homography2D.FromFourPointPairs(target, surface);
        var inverse = Homography2D.FromFourPointPairs(surface, target);

        foreach (var p in new[] { new Point2D(25,25), new Point2D(50,50), new Point2D(75,40) })
        {
            var roundTrip = inverse.Map(forward.Map(p));
            Assert.InRange(Math.Abs(roundTrip.X - p.X), 0, Tol.RoundTripToleranceMm);
            Assert.InRange(Math.Abs(roundTrip.Y - p.Y), 0, Tol.RoundTripToleranceMm);
        }
    }

    [Fact]
    public void Homography_PreservesCollinearity()
    {
        var source = new[] { new Point2D(0,0), new Point2D(100,0), new Point2D(100,100), new Point2D(0,100) };
        var destination = new[] { new Point2D(0,0), new Point2D(200,20), new Point2D(160,150), new Point2D(10,120) };
        var h = Homography2D.FromFourPointPairs(source, destination);
        var a = h.Map(new Point2D(20,50));
        var b = h.Map(new Point2D(50,50));
        var c = h.Map(new Point2D(80,50));
        var cross = (b.X-a.X)*(c.Y-a.Y) - (b.Y-a.Y)*(c.X-a.X);
        Assert.InRange(Math.Abs(cross), 0, 1e-8);
    }

    [Fact]
    public void PolygonClipping_HandlesConcaveGlyph()
    {
        var lShape = new Polygon2D(new[]
        {
            new Point2D(0,0), new Point2D(4,0), new Point2D(4,1),
            new Point2D(1,1), new Point2D(1,4), new Point2D(0,4)
        });
        var clip = Rect(-0.5, 0.5, 3, 3);
        var intersection = PolygonBoolean.Intersect(lShape, clip);
        Assert.Equal(3.75, intersection.Area, 8);
    }

    [Fact]
    public void NearParallelSurface_OwnsCompleteOverlap()
    {
        var viewer = new Point3D(0,0,0);
        var target = Target();
        var front = Surface("front", -400, 800, -200, 800, 400);
        var rear = Surface("rear", -500, 1000, -250, 1000, 500);
        var resolved = OwnershipResolver.Resolve(viewer, target, new[]
        {
            new SurfaceFootprint(front, Footprint(viewer, front, target)),
            new SurfaceFootprint(rear, Footprint(viewer, rear, target))
        }, Tol);

        Assert.Equal(125000, resolved["front"].Area, 6);
        Assert.Equal(0, resolved["rear"].Area, 6);
    }

    [Fact]
    public void PartiallyOccludedRearSurface_RetainsVisibleHalf()
    {
        var viewer = new Point3D(0,0,0);
        var target = Target();
        var front = Surface("front", -400, 800, -200, 400, 400);
        var rear = Surface("rear", -500, 1000, -250, 1000, 500);
        var resolved = OwnershipResolver.Resolve(viewer, target, new[]
        {
            new SurfaceFootprint(front, Footprint(viewer, front, target)),
            new SurfaceFootprint(rear, Footprint(viewer, rear, target))
        }, Tol);

        Assert.Equal(62500, resolved["front"].Area, 6);
        Assert.Equal(62500, resolved["rear"].Area, 6);
        Assert.Equal(0, resolved["front"].Intersect(resolved["rear"]).Area, 6);
    }

    [Fact]
    public void DeterministicGeometryText_IsStableAcrossInputOrder()
    {
        var a = Rect(10, 10, 20, 20);
        var b = Rect(0, 0, 5, 5);
        var first = new MultiPolygon2D(new[] { a, b }).ToDeterministicText();
        var second = new MultiPolygon2D(new[] { b, a }).ToDeterministicText();
        Assert.Equal(first, second);
    }

    [Fact]
    public void DeterministicGeometryText_IsStableAcrossRingDirection()
    {
        var forward = Rect(0, 0, 10, 5);
        var reverse = new Polygon2D(forward.Shell.Reverse().ToArray());
        Assert.Equal(
            new MultiPolygon2D(new[] { forward }).ToDeterministicText(),
            new MultiPolygon2D(new[] { reverse }).ToDeterministicText());
    }

    [Fact]
    public void SingularHomography_IsRejectedExplicitly()
    {
        var collinear = new[] { new Point2D(0,0), new Point2D(1,0), new Point2D(2,0), new Point2D(3,0) };
        Assert.Throws<ArgumentException>(() => Homography2D.FromFourPointPairs(collinear, collinear));
    }

    private static TargetPlane Target() => new(
        new Point3D(0,500,0), new Vector3D(1,0,0), new Vector3D(0,0,1), Tol);

    private static RectSurface3D Surface(string id, double x, double y, double z, double width, double height) =>
        new(id, new Point3D(x,y,z), new Vector3D(1,0,0), new Vector3D(0,0,1), width, height, Tol);

    private static Polygon2D Footprint(Point3D viewer, RectSurface3D surface, TargetPlane target) =>
        new(Geometry3D.ProjectSurfaceFootprint(viewer, surface, target, Tol));

    private static Polygon2D Rect(double x, double y, double width, double height) => new(new[]
    {
        new Point2D(x,y), new Point2D(x+width,y), new Point2D(x+width,y+height), new Point2D(x,y+height)
    });
}
