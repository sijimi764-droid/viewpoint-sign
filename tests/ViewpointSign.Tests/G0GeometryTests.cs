using ViewpointSign.Geometry;
using Xunit;

namespace ViewpointSign.Tests;

public sealed class G0GeometryTests
{
    private static readonly ToleranceProfile Tol = ToleranceProfile.G0;

    [Fact]
    public void RayPlaneIntersection_ReturnsAnalyticPoint()
    {
        var ray = new Ray3D(new Point3D(0, 0, 0), new Vector3D(0, 2, 0));
        var plane = new Plane3D(new Point3D(0, 10, 0), new Vector3D(0, 1, 0));

        var hit = Geometry3D.Intersect(ray, plane, Tol);

        Assert.True(hit.IsHit);
        Assert.Equal(5.0, hit.T, 12);
        Assert.Equal(new Point3D(0, 10, 0), hit.Point);
    }

    [Fact]
    public void ParallelRay_IsRejectedWithoutNonFinitePropagation()
    {
        var ray = new Ray3D(new Point3D(0, 0, 0), new Vector3D(1, 0, 0));
        var plane = new Plane3D(new Point3D(0, 10, 0), new Vector3D(0, 1, 0));

        var hit = Geometry3D.Intersect(ray, plane, Tol);

        Assert.False(hit.IsHit);
        Assert.Equal("parallel-or-degenerate", hit.Diagnostic);
    }

    [Fact]
    public void IntersectionBehindViewer_IsRejected()
    {
        var ray = new Ray3D(new Point3D(0, 0, 0), new Vector3D(0, -1, 0));
        var plane = new Plane3D(new Point3D(0, 10, 0), new Vector3D(0, 1, 0));

        var hit = Geometry3D.Intersect(ray, plane, Tol);

        Assert.False(hit.IsHit);
        Assert.Equal("behind-viewer-or-at-origin", hit.Diagnostic);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1000, 500, true)]
    [InlineData(-0.0005, 250, true)]
    [InlineData(-0.01, 250, false)]
    [InlineData(1001, 250, false)]
    public void FiniteSurface_UsesBoundaryTolerance(double u, double v, bool expected)
    {
        var surface = Surface();
        Assert.Equal(expected, surface.ContainsLocal(new Point2D(u, v), Tol.BoundaryToleranceMm));
    }

    [Fact]
    public void SurfaceLocalWorld_RoundTrip_IsWithin001Millimetre()
    {
        var surface = new RectSurface3D(
            "angled",
            new Point3D(120, 300, 40),
            new Vector3D(1, 1, 0),
            new Vector3D(0, 0, 1),
            1200,
            800,
            Tol);
        var source = new Point2D(712.345, 456.789);

        var roundTrip = surface.ToLocal(surface.ToWorld(source));

        Assert.InRange(Math.Abs(roundTrip.X - source.X), 0, Tol.RoundTripToleranceMm);
        Assert.InRange(Math.Abs(roundTrip.Y - source.Y), 0, Tol.RoundTripToleranceMm);
    }

    [Fact]
    public void FiniteSurfaceIntersection_RejectsPointOutsideRectangle()
    {
        var surface = Surface();
        var viewer = new Point3D(2000, 0, 250);
        var ray = new Ray3D(viewer, new Vector3D(0, 1, 0));

        var hit = Geometry3D.Intersect(ray, surface, Tol);

        Assert.False(hit.IsHit);
        Assert.Equal("outside-finite-surface", hit.Diagnostic);
    }

    [Fact]
    public void AxisAlignedSurfaceFootprint_IsAnalyticallyCorrect()
    {
        // Viewer at origin. Surface is at Y=1000. Target plane is at Y=500.
        // Perspective therefore scales X/Z coordinates by exactly 0.5.
        var viewer = new Point3D(0, 0, 0);
        var surface = new RectSurface3D(
            "S1",
            new Point3D(-500, 1000, -250),
            new Vector3D(1, 0, 0),
            new Vector3D(0, 0, 1),
            1000,
            500,
            Tol);
        var target = new TargetPlane(
            new Point3D(0, 500, 0),
            new Vector3D(1, 0, 0),
            new Vector3D(0, 0, 1),
            Tol);

        var footprint = Geometry3D.ProjectSurfaceFootprint(viewer, surface, target, Tol);

        AssertPoint(footprint[0], -250, -125);
        AssertPoint(footprint[1], 250, -125);
        AssertPoint(footprint[2], 250, 125);
        AssertPoint(footprint[3], -250, 125);
    }

    [Fact]
    public void DegenerateBasis_IsRejectedExplicitly()
    {
        var ex = Assert.Throws<ArgumentException>(() => new RectSurface3D(
            "bad",
            new Point3D(0, 0, 0),
            new Vector3D(1, 0, 0),
            new Vector3D(1, 0, 0),
            100,
            100,
            Tol));

        Assert.Contains("orthogonal", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static RectSurface3D Surface() => new(
        "S1",
        new Point3D(0, 1000, 0),
        new Vector3D(1, 0, 0),
        new Vector3D(0, 0, 1),
        1000,
        500,
        Tol);

    private static void AssertPoint(Point2D actual, double expectedX, double expectedY)
    {
        Assert.Equal(expectedX, actual.X, 9);
        Assert.Equal(expectedY, actual.Y, 9);
    }
}
