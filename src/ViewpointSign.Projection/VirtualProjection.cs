using ViewpointSign.Geometry;

namespace ViewpointSign.Projection;

public sealed record GlyphGeometry(string GlyphId, MultiPolygon2D TargetGeometry);

public sealed record SurfaceArtworkFragment(
    string GlyphId,
    string SurfaceId,
    MultiPolygon2D SourceTargetGeometry,
    MultiPolygon2D LocalArtwork,
    MultiPolygon2D ReconstructedTargetGeometry);

public sealed record VirtualProjectionResult(
    IReadOnlyList<SurfaceArtworkFragment> Fragments,
    IReadOnlyDictionary<string, MultiPolygon2D> OwnershipRegions,
    MultiPolygon2D TargetGeometry,
    MultiPolygon2D ReconstructedGeometry,
    MultiPolygon2D GapGeometry,
    double MaxRoundTripErrorMm);

public static class VirtualProjector
{
    public static VirtualProjectionResult Project(
        Point3D viewer,
        TargetPlane target,
        IReadOnlyList<GlyphGeometry> glyphs,
        IReadOnlyList<RectSurface3D> surfaces,
        ToleranceProfile? tolerance = null)
    {
        tolerance ??= ToleranceProfile.G0;
        if (glyphs.Count == 0) throw new ArgumentException("At least one glyph is required.", nameof(glyphs));
        if (surfaces.Count == 0) throw new ArgumentException("At least one installation surface is required.", nameof(surfaces));

        var targetGeometry = UnionAll(glyphs.Select(g => g.TargetGeometry));
        var surfaceFootprints = surfaces.Select(surface => new SurfaceFootprint(
            surface,
            new Polygon2D(Geometry3D.ProjectSurfaceFootprint(viewer, surface, target, tolerance)))).ToArray();
        var ownership = OwnershipResolver.Resolve(viewer, target, surfaceFootprints, tolerance);

        var fragments = new List<SurfaceArtworkFragment>();
        var assignedTarget = MultiPolygon2D.Empty;
        var reconstructed = MultiPolygon2D.Empty;
        var maxRoundTripError = 0.0;

        foreach (var surfaceFootprint in surfaceFootprints.OrderBy(s => s.Surface.Id, StringComparer.Ordinal))
        {
            var surface = surfaceFootprint.Surface;
            var targetCorners = surfaceFootprint.Footprint.Shell;
            var localCorners = new[]
            {
                new Point2D(0, 0),
                new Point2D(surface.Width, 0),
                new Point2D(surface.Width, surface.Height),
                new Point2D(0, surface.Height)
            };
            var targetToSurface = Homography2D.FromFourPointPairs(targetCorners, localCorners);
            var surfaceToTarget = Homography2D.FromFourPointPairs(localCorners, targetCorners);
            var ownedRegion = ownership[surface.Id];

            foreach (var glyph in glyphs.OrderBy(g => g.GlyphId, StringComparer.Ordinal))
            {
                var sourceFragment = glyph.TargetGeometry.Intersect(ownedRegion);
                if (sourceFragment.Polygons.Count == 0 || sourceFragment.Area <= tolerance.BoundaryToleranceMm * tolerance.BoundaryToleranceMm)
                    continue;

                var localArtwork = Map(sourceFragment, targetToSurface);
                var roundTrip = Map(localArtwork, surfaceToTarget);
                maxRoundTripError = Math.Max(maxRoundTripError, MaxVertexRoundTripError(sourceFragment, targetToSurface, surfaceToTarget));

                fragments.Add(new SurfaceArtworkFragment(
                    glyph.GlyphId,
                    surface.Id,
                    sourceFragment,
                    localArtwork,
                    roundTrip));

                assignedTarget = assignedTarget.Union(sourceFragment);
                reconstructed = reconstructed.Union(roundTrip);
            }
        }

        var gap = targetGeometry.Difference(assignedTarget);
        return new VirtualProjectionResult(
            fragments,
            ownership,
            targetGeometry,
            reconstructed,
            gap,
            maxRoundTripError);
    }

    private static MultiPolygon2D Map(MultiPolygon2D geometry, Homography2D transform) => new(
        geometry.Polygons.Select(p => new Polygon2D(
            p.Shell.Select(transform.Map).ToArray(),
            p.Holes.Select(h => (IReadOnlyList<Point2D>)h.Select(transform.Map).ToArray()).ToArray())).ToArray());

    private static double MaxVertexRoundTripError(
        MultiPolygon2D source,
        Homography2D forward,
        Homography2D reverse)
    {
        var max = 0.0;
        foreach (var polygon in source.Polygons)
        {
            foreach (var ring in new[] { polygon.Shell }.Concat(polygon.Holes))
            {
                foreach (var p in ring)
                {
                    var q = reverse.Map(forward.Map(p));
                    var dx = q.X - p.X;
                    var dy = q.Y - p.Y;
                    max = Math.Max(max, Math.Sqrt(dx * dx + dy * dy));
                }
            }
        }
        return max;
    }

    private static MultiPolygon2D UnionAll(IEnumerable<MultiPolygon2D> geometries)
    {
        var result = MultiPolygon2D.Empty;
        foreach (var geometry in geometries) result = result.Union(geometry);
        return result;
    }
}
