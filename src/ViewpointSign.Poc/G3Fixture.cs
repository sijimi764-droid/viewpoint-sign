using ViewpointSign.Geometry;
using ViewpointSign.Projection;

namespace ViewpointSign.Poc;

public static class G3Fixture
{
    public const string ProjectId = "G3-PHYSICAL-01";
    public static readonly ToleranceProfile Tolerance = ToleranceProfile.G0;
    public static readonly Point3D Viewer = new(0, 0, 1200);
    public static readonly TargetPlane Target = new(
        new Point3D(0, 1200, 1200),
        new Vector3D(1, 0, 0),
        new Vector3D(0, 0, 1),
        Tolerance);

    public static IReadOnlyList<RectSurface3D> Surfaces() => new[]
    {
        Surface("S1", new Point3D(-200, 900, 1060), new Vector3D(1, 0.12, 0)),
        Surface("S2", new Point3D(-70, 1050, 1060), new Vector3D(1, 0, 0)),
        Surface("S3", new Point3D(70, 1200, 1060), new Vector3D(1, -0.12, 0))
    };

    public static IReadOnlyList<GlyphGeometry> Glyphs()
    {
        var gOuter = Rect(-240, -90, 160, 180);
        var gInner = Rect(-200, -50, 80, 100);
        var gOpening = Rect(-125, -25, 55, 50);
        var g = gOuter.Difference(gInner).Difference(gOpening);

        var a = new MultiPolygon2D(new[]
        {
            new Polygon2D(
                new[]
                {
                    new Point2D(-40, -90),
                    new Point2D(70, 90),
                    new Point2D(180, -90)
                },
                new IReadOnlyList<Point2D>[]
                {
                    new[]
                    {
                        new Point2D(40, -30),
                        new Point2D(70, 25),
                        new Point2D(100, -30)
                    }
                })
        });
        return new[] { new GlyphGeometry("G", g), new GlyphGeometry("A", a) };
    }

    private static RectSurface3D Surface(string id, Point3D origin, Vector3D horizontal) => new(
        id,
        origin,
        horizontal,
        new Vector3D(0, 0, 1),
        190.0,
        277.0,
        Tolerance);

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
}
