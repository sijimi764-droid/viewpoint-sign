namespace ViewpointSign.Geometry;

public readonly record struct Point2D(double X, double Y);

public readonly record struct Point3D(double X, double Y, double Z)
{
    public static Vector3D operator -(Point3D a, Point3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Point3D operator +(Point3D p, Vector3D v) => new(p.X + v.X, p.Y + v.Y, p.Z + v.Z);
}

public readonly record struct Vector3D(double X, double Y, double Z)
{
    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

    public Vector3D Normalize(double epsilon = 1e-10)
    {
        var length = Length;
        if (!double.IsFinite(length) || length <= epsilon)
            throw new ArgumentException("Cannot normalize a zero or non-finite vector.");
        return new Vector3D(X / length, Y / length, Z / length);
    }

    public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3D operator *(Vector3D v, double s) => new(v.X * s, v.Y * s, v.Z * s);
    public static Vector3D operator *(double s, Vector3D v) => v * s;

    public static double Dot(Vector3D a, Vector3D b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    public static Vector3D Cross(Vector3D a, Vector3D b) =>
        new(a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);
}

public readonly record struct Ray3D(Point3D Origin, Vector3D Direction)
{
    public Point3D At(double t) => Origin + Direction * t;
}

public sealed record ToleranceProfile(
    double ParallelEpsilon = 1e-10,
    double ForwardEpsilon = 1e-10,
    double OrthogonalityEpsilon = 1e-8,
    double BoundaryToleranceMm = 0.001,
    double RoundTripToleranceMm = 0.001,
    double ReconstructedContourToleranceMm = 0.5)
{
    public static ToleranceProfile G0 { get; } = new();
}

public sealed class Plane3D
{
    public Point3D Point { get; }
    public Vector3D Normal { get; }

    public Plane3D(Point3D point, Vector3D normal, ToleranceProfile? tolerance = null)
    {
        tolerance ??= ToleranceProfile.G0;
        Point = point;
        Normal = normal.Normalize(tolerance.ParallelEpsilon);
    }
}

public readonly record struct IntersectionResult(bool IsHit, double T, Point3D Point, string? Diagnostic)
{
    public static IntersectionResult Miss(string diagnostic) => new(false, double.NaN, default, diagnostic);
    public static IntersectionResult Hit(double t, Point3D point) => new(true, t, point, null);
}

public sealed class RectSurface3D
{
    public string Id { get; }
    public Point3D Origin { get; }
    public Vector3D Eu { get; }
    public Vector3D Ev { get; }
    public Vector3D Normal { get; }
    public double Width { get; }
    public double Height { get; }
    public Plane3D Plane { get; }

    public RectSurface3D(
        string id,
        Point3D origin,
        Vector3D eu,
        Vector3D ev,
        double width,
        double height,
        ToleranceProfile? tolerance = null)
    {
        tolerance ??= ToleranceProfile.G0;
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Surface ID is required.", nameof(id));
        if (!double.IsFinite(width) || width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (!double.IsFinite(height) || height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        Id = id;
        Origin = origin;
        Eu = eu.Normalize(tolerance.ParallelEpsilon);
        Ev = ev.Normalize(tolerance.ParallelEpsilon);

        if (Math.Abs(Vector3D.Dot(Eu, Ev)) > tolerance.OrthogonalityEpsilon)
            throw new ArgumentException("Surface basis vectors must be orthogonal.");

        Normal = Vector3D.Cross(Eu, Ev).Normalize(tolerance.ParallelEpsilon);
        Width = width;
        Height = height;
        Plane = new Plane3D(origin, Normal, tolerance);
    }

    public Point3D ToWorld(Point2D local) => Origin + Eu * local.X + Ev * local.Y;

    public Point2D ToLocal(Point3D world)
    {
        var d = world - Origin;
        return new Point2D(Vector3D.Dot(d, Eu), Vector3D.Dot(d, Ev));
    }

    public bool ContainsLocal(Point2D local, double toleranceMm) =>
        local.X >= -toleranceMm && local.X <= Width + toleranceMm &&
        local.Y >= -toleranceMm && local.Y <= Height + toleranceMm;

    public IReadOnlyList<Point3D> Corners => new[]
    {
        Origin,
        ToWorld(new Point2D(Width, 0)),
        ToWorld(new Point2D(Width, Height)),
        ToWorld(new Point2D(0, Height))
    };
}

public sealed class TargetPlane
{
    public Point3D Origin { get; }
    public Vector3D Tx { get; }
    public Vector3D Ty { get; }
    public Plane3D Plane { get; }

    public TargetPlane(Point3D origin, Vector3D tx, Vector3D ty, ToleranceProfile? tolerance = null)
    {
        tolerance ??= ToleranceProfile.G0;
        Origin = origin;
        Tx = tx.Normalize(tolerance.ParallelEpsilon);
        Ty = ty.Normalize(tolerance.ParallelEpsilon);
        if (Math.Abs(Vector3D.Dot(Tx, Ty)) > tolerance.OrthogonalityEpsilon)
            throw new ArgumentException("Target basis vectors must be orthogonal.");
        Plane = new Plane3D(origin, Vector3D.Cross(Tx, Ty), tolerance);
    }

    public Point3D ToWorld(Point2D local) => Origin + Tx * local.X + Ty * local.Y;

    public Point2D ToLocal(Point3D world)
    {
        var d = world - Origin;
        return new Point2D(Vector3D.Dot(d, Tx), Vector3D.Dot(d, Ty));
    }
}

public static class Geometry3D
{
    public static IntersectionResult Intersect(Ray3D ray, Plane3D plane, ToleranceProfile? tolerance = null)
    {
        tolerance ??= ToleranceProfile.G0;
        var denominator = Vector3D.Dot(ray.Direction, plane.Normal);
        if (!double.IsFinite(denominator) || Math.Abs(denominator) <= tolerance.ParallelEpsilon)
            return IntersectionResult.Miss("parallel-or-degenerate");

        var numerator = Vector3D.Dot(plane.Point - ray.Origin, plane.Normal);
        var t = numerator / denominator;
        if (!double.IsFinite(t))
            return IntersectionResult.Miss("non-finite-intersection");
        if (t <= tolerance.ForwardEpsilon)
            return IntersectionResult.Miss("behind-viewer-or-at-origin");

        var point = ray.At(t);
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y) || !double.IsFinite(point.Z))
            return IntersectionResult.Miss("non-finite-point");

        return IntersectionResult.Hit(t, point);
    }

    public static IntersectionResult Intersect(Ray3D ray, RectSurface3D surface, ToleranceProfile? tolerance = null)
    {
        tolerance ??= ToleranceProfile.G0;
        var planeHit = Intersect(ray, surface.Plane, tolerance);
        if (!planeHit.IsHit) return planeHit;

        var local = surface.ToLocal(planeHit.Point);
        return surface.ContainsLocal(local, tolerance.BoundaryToleranceMm)
            ? planeHit
            : IntersectionResult.Miss("outside-finite-surface");
    }

    public static IReadOnlyList<Point2D> ProjectSurfaceFootprint(
        Point3D viewer,
        RectSurface3D surface,
        TargetPlane target,
        ToleranceProfile? tolerance = null)
    {
        tolerance ??= ToleranceProfile.G0;
        var result = new List<Point2D>(4);
        foreach (var corner in surface.Corners)
        {
            var direction = corner - viewer;
            var hit = Intersect(new Ray3D(viewer, direction), target.Plane, tolerance);
            if (!hit.IsHit)
                throw new InvalidOperationException($"Surface {surface.Id} cannot be projected to target plane: {hit.Diagnostic}.");
            result.Add(target.ToLocal(hit.Point));
        }
        return result;
    }
}
