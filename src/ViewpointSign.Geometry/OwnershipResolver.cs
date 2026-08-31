using NetTopologySuite.Geometries;

namespace ViewpointSign.Geometry;

public sealed record SurfaceFootprint(RectSurface3D Surface, Polygon2D Footprint);

public static class OwnershipResolver
{
    public static IReadOnlyDictionary<string, MultiPolygon2D> Resolve(
        Point3D viewer,
        TargetPlane target,
        IReadOnlyList<SurfaceFootprint> surfaces,
        ToleranceProfile? tolerance = null)
    {
        tolerance ??= ToleranceProfile.G0;
        var result = new SortedDictionary<string, MultiPolygon2D>(StringComparer.Ordinal);

        foreach (var current in surfaces.OrderBy(s => s.Surface.Id, StringComparer.Ordinal))
        {
            var owned = new MultiPolygon2D(new[] { current.Footprint });
            foreach (var other in surfaces.Where(s => s.Surface.Id != current.Surface.Id))
            {
                var overlap = owned.Intersect(new MultiPolygon2D(new[] { other.Footprint }));
                if (overlap.Polygons.Count == 0 || overlap.Area <= tolerance.BoundaryToleranceMm * tolerance.BoundaryToleranceMm)
                    continue;

                var good = KeepWhereFirstIsNearer(viewer, target, current.Surface, other.Surface, overlap, tolerance);
                owned = owned.Difference(overlap).Union(good);
            }
            result[current.Surface.Id] = owned;
        }

        return result;
    }

    private static MultiPolygon2D KeepWhereFirstIsNearer(
        Point3D viewer,
        TargetPlane target,
        RectSurface3D first,
        RectSurface3D second,
        MultiPolygon2D overlap,
        ToleranceProfile tolerance)
    {
        var f = DepthEquality(first, second, viewer, target);
        var ntsOverlap = overlap.ToNts();
        var sample = ntsOverlap.PointOnSurface.Coordinate;
        var p = new Point2D(sample.X, sample.Y);
        var firstDepth = DepthAt(first, viewer, target, p, tolerance);
        var secondDepth = DepthAt(second, viewer, target, p, tolerance);

        if (!double.IsFinite(firstDepth) || !double.IsFinite(secondDepth))
            throw new InvalidOperationException("Ownership overlap contains a point without valid positive depth.");

        var q = f.A * p.X + f.B * p.Y + f.C;
        if (Math.Abs(q) <= tolerance.ParallelEpsilon)
        {
            // Choose a deterministic nearby sample to establish the side of the analytic equality line.
            p = new Point2D(p.X + Math.Max(1e-6, tolerance.BoundaryToleranceMm), p.Y);
            firstDepth = DepthAt(first, viewer, target, p, tolerance);
            secondDepth = DepthAt(second, viewer, target, p, tolerance);
            q = f.A * p.X + f.B * p.Y + f.C;
        }

        if (Math.Abs(f.A) <= tolerance.ParallelEpsilon && Math.Abs(f.B) <= tolerance.ParallelEpsilon)
        {
            if (Math.Abs(firstDepth - secondDepth) <= tolerance.ParallelEpsilon)
                return StringComparer.Ordinal.Compare(first.Id, second.Id) <= 0 ? overlap : MultiPolygon2D.Empty;
            return firstDepth < secondDepth ? overlap : MultiPolygon2D.Empty;
        }

        var desiredSign = firstDepth <= secondDepth ? Math.Sign(q) : -Math.Sign(q);
        if (desiredSign == 0) desiredSign = 1;
        var halfPlane = BuildHalfPlane(ntsOverlap.EnvelopeInternal, f, desiredSign, tolerance);
        return MultiPolygon2D.FromNts(ntsOverlap.Intersection(halfPlane.ToNts()));
    }

    private static (double A, double B, double C) DepthEquality(
        RectSurface3D first,
        RectSurface3D second,
        Point3D viewer,
        TargetPlane target)
    {
        var c1 = Vector3D.Dot(first.Plane.Point - viewer, first.Plane.Normal);
        var c2 = Vector3D.Dot(second.Plane.Point - viewer, second.Plane.Normal);

        var baseVector = target.Origin - viewer;
        var d10 = Vector3D.Dot(baseVector, first.Plane.Normal);
        var d1x = Vector3D.Dot(target.Tx, first.Plane.Normal);
        var d1y = Vector3D.Dot(target.Ty, first.Plane.Normal);
        var d20 = Vector3D.Dot(baseVector, second.Plane.Normal);
        var d2x = Vector3D.Dot(target.Tx, second.Plane.Normal);
        var d2y = Vector3D.Dot(target.Ty, second.Plane.Normal);

        return (
            c1 * d2x - c2 * d1x,
            c1 * d2y - c2 * d1y,
            c1 * d20 - c2 * d10);
    }

    private static double DepthAt(
        RectSurface3D surface,
        Point3D viewer,
        TargetPlane target,
        Point2D targetPoint,
        ToleranceProfile tolerance)
    {
        var world = target.ToWorld(targetPoint);
        var hit = Geometry3D.Intersect(new Ray3D(viewer, world - viewer), surface.Plane, tolerance);
        return hit.IsHit ? hit.T : double.NaN;
    }

    private static Polygon2D BuildHalfPlane(
        Envelope envelope,
        (double A, double B, double C) line,
        int desiredSign,
        ToleranceProfile tolerance)
    {
        var span = Math.Max(Math.Max(envelope.Width, envelope.Height), 1.0);
        var pad = span * 4 + 1;
        var polygon = new List<Point2D>
        {
            new(envelope.MinX - pad, envelope.MinY - pad),
            new(envelope.MaxX + pad, envelope.MinY - pad),
            new(envelope.MaxX + pad, envelope.MaxY + pad),
            new(envelope.MinX - pad, envelope.MaxY + pad)
        };

        var output = new List<Point2D>();
        for (var i = 0; i < polygon.Count; i++)
        {
            var s = polygon[i];
            var e = polygon[(i + 1) % polygon.Count];
            var fs = desiredSign * Evaluate(line, s);
            var fe = desiredSign * Evaluate(line, e);
            var sInside = fs >= -tolerance.ParallelEpsilon;
            var eInside = fe >= -tolerance.ParallelEpsilon;

            if (sInside && eInside) output.Add(e);
            else if (sInside && !eInside) output.Add(LineIntersection(s, e, fs, fe));
            else if (!sInside && eInside)
            {
                output.Add(LineIntersection(s, e, fs, fe));
                output.Add(e);
            }
        }

        if (output.Count < 3) throw new InvalidOperationException("Analytic ownership half-plane produced an empty clipping polygon unexpectedly.");
        return new Polygon2D(output);
    }

    private static double Evaluate((double A, double B, double C) line, Point2D p) => line.A * p.X + line.B * p.Y + line.C;

    private static Point2D LineIntersection(Point2D s, Point2D e, double fs, double fe)
    {
        var denominator = fs - fe;
        if (Math.Abs(denominator) <= 1e-15) return s;
        var t = fs / denominator;
        return new Point2D(s.X + t * (e.X - s.X), s.Y + t * (e.Y - s.Y));
    }
}
