using System.Globalization;
using System.Text;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace ViewpointSign.Geometry;

public sealed record Polygon2D(IReadOnlyList<Point2D> Shell, IReadOnlyList<IReadOnlyList<Point2D>> Holes)
{
    public Polygon2D(IReadOnlyList<Point2D> shell) : this(shell, Array.Empty<IReadOnlyList<Point2D>>()) { }

    public double Area => ToNts().Area;

    internal Polygon ToNts()
    {
        var factory = NtsGeometryServices.Instance.CreateGeometryFactory();
        var shell = factory.CreateLinearRing(ToClosedCoordinates(Shell));
        var holes = Holes.Select(h => factory.CreateLinearRing(ToClosedCoordinates(h))).ToArray();
        var polygon = factory.CreatePolygon(shell, holes);
        if (!polygon.IsValid) throw new ArgumentException("Polygon is invalid.");
        return polygon;
    }

    private static Coordinate[] ToClosedCoordinates(IReadOnlyList<Point2D> ring)
    {
        if (ring.Count < 3) throw new ArgumentException("A polygon ring requires at least three points.");
        var points = ring.ToList();
        if (points[0] != points[^1]) points.Add(points[0]);
        return points.Select(p => new Coordinate(p.X, p.Y)).ToArray();
    }
}

public sealed record MultiPolygon2D(IReadOnlyList<Polygon2D> Polygons)
{
    public static MultiPolygon2D Empty { get; } = new(Array.Empty<Polygon2D>());
    public double Area => Polygons.Sum(p => p.Area);

    public MultiPolygon2D Intersect(MultiPolygon2D other) => FromNts(ToNts().Intersection(other.ToNts()));
    public MultiPolygon2D Difference(MultiPolygon2D other) => FromNts(ToNts().Difference(other.ToNts()));
    public MultiPolygon2D Union(MultiPolygon2D other) => FromNts(ToNts().Union(other.ToNts()));

    public string ToDeterministicText()
    {
        var normalized = Normalize(Polygons);
        var sb = new StringBuilder();
        foreach (var polygon in normalized)
        {
            AppendRing(sb, polygon.Shell);
            sb.Append('|');
            foreach (var hole in NormalizeRings(polygon.Holes)) { AppendRing(sb, hole); sb.Append('|'); }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    internal NetTopologySuite.Geometries.Geometry ToNts()
    {
        var factory = NtsGeometryServices.Instance.CreateGeometryFactory();
        if (Polygons.Count == 0) return factory.CreatePolygon();
        return factory.CreateMultiPolygon(Polygons.Select(p => p.ToNts()).ToArray());
    }

    internal static MultiPolygon2D FromNts(NetTopologySuite.Geometries.Geometry geometry)
    {
        var polygons = new List<Polygon2D>();
        Collect(geometry, polygons);
        return new MultiPolygon2D(Normalize(polygons));
    }

    private static void Collect(NetTopologySuite.Geometries.Geometry geometry, List<Polygon2D> output)
    {
        switch (geometry)
        {
            case Polygon p when !p.IsEmpty:
                output.Add(FromPolygon(p));
                break;
            case GeometryCollection collection:
                for (var i = 0; i < collection.NumGeometries; i++) Collect(collection.GetGeometryN(i), output);
                break;
        }
    }

    private static Polygon2D FromPolygon(Polygon polygon)
    {
        var shell = FromRing(polygon.ExteriorRing.Coordinates);
        var holes = Enumerable.Range(0, polygon.NumInteriorRings)
            .Select(i => (IReadOnlyList<Point2D>)FromRing(polygon.GetInteriorRingN(i).Coordinates))
            .ToArray();
        return new Polygon2D(shell, holes);
    }

    private static IReadOnlyList<Point2D> FromRing(Coordinate[] coordinates)
    {
        var points = coordinates.Take(Math.Max(0, coordinates.Length - 1))
            .Select(c => new Point2D(c.X, c.Y)).ToList();
        return NormalizeRing(points);
    }

    private static IReadOnlyList<Polygon2D> Normalize(IEnumerable<Polygon2D> polygons) => polygons
        .Select(p => new Polygon2D(NormalizeRing(p.Shell), NormalizeRings(p.Holes)))
        .OrderByDescending(p => p.Area)
        .ThenBy(p => p.Shell[0].X)
        .ThenBy(p => p.Shell[0].Y)
        .ToArray();

    private static IReadOnlyList<IReadOnlyList<Point2D>> NormalizeRings(IEnumerable<IReadOnlyList<Point2D>> rings) => rings
        .Select(NormalizeRing)
        .OrderBy(r => r[0].X)
        .ThenBy(r => r[0].Y)
        .Cast<IReadOnlyList<Point2D>>()
        .ToArray();

    private static IReadOnlyList<Point2D> NormalizeRing(IReadOnlyList<Point2D> ring)
    {
        if (ring.Count == 0) return ring;
        var forward = CanonicalRotation(ring);
        var reverse = CanonicalRotation(ring.Reverse().ToArray());
        return CompareSequences(forward, reverse) <= 0 ? forward : reverse;
    }

    private static IReadOnlyList<Point2D> CanonicalRotation(IReadOnlyList<Point2D> ring)
    {
        var start = Enumerable.Range(0, ring.Count)
            .OrderBy(i => ring[i].X).ThenBy(i => ring[i].Y).First();
        return Enumerable.Range(0, ring.Count).Select(k => ring[(start + k) % ring.Count]).ToArray();
    }

    private static int CompareSequences(IReadOnlyList<Point2D> a, IReadOnlyList<Point2D> b)
    {
        for (var i = 0; i < Math.Min(a.Count, b.Count); i++)
        {
            var x = a[i].X.CompareTo(b[i].X);
            if (x != 0) return x;
            var y = a[i].Y.CompareTo(b[i].Y);
            if (y != 0) return y;
        }
        return a.Count.CompareTo(b.Count);
    }

    private static void AppendRing(StringBuilder sb, IReadOnlyList<Point2D> ring)
    {
        sb.Append('[');
        for (var i = 0; i < ring.Count; i++)
        {
            if (i > 0) sb.Append(';');
            sb.Append(ring[i].X.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(ring[i].Y.ToString("R", CultureInfo.InvariantCulture));
        }
        sb.Append(']');
    }
}

public static class PolygonBoolean
{
    public static MultiPolygon2D Intersect(Polygon2D subject, Polygon2D clip) =>
        MultiPolygon2D.FromNts(subject.ToNts().Intersection(clip.ToNts()));
}
