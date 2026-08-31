using ViewpointSign.Geometry;

namespace ViewpointSign.Printing;

public enum PaperOrientation
{
    Portrait,
    Landscape
}

public sealed record PaperSizeMm(double Width, double Height)
{
    public static PaperSizeMm A4(PaperOrientation orientation) => orientation switch
    {
        PaperOrientation.Portrait => new PaperSizeMm(210.0, 297.0),
        PaperOrientation.Landscape => new PaperSizeMm(297.0, 210.0),
        _ => throw new ArgumentOutOfRangeException(nameof(orientation))
    };
}

public sealed record PrintProfile(
    PaperSizeMm Paper,
    double MarginMm,
    double OverlapMm,
    double CalibrationLengthMm = 100.0)
{
    public static PrintProfile A4(PaperOrientation orientation, double marginMm = 10.0, double overlapMm = 10.0) =>
        new(PaperSizeMm.A4(orientation), marginMm, overlapMm);

    public void Validate()
    {
        if (Paper.Width <= 0 || Paper.Height <= 0) throw new ArgumentOutOfRangeException(nameof(Paper));
        if (MarginMm < 0) throw new ArgumentOutOfRangeException(nameof(MarginMm));
        var usableWidth = Paper.Width - 2 * MarginMm;
        var usableHeight = Paper.Height - 2 * MarginMm;
        if (usableWidth <= 0 || usableHeight <= 0) throw new ArgumentException("Margins consume the whole page.");
        if (OverlapMm < 0 || OverlapMm >= usableWidth || OverlapMm >= usableHeight)
            throw new ArgumentOutOfRangeException(nameof(OverlapMm), "Overlap must be non-negative and smaller than both printable dimensions.");
        if (CalibrationLengthMm <= 0 || CalibrationLengthMm > usableWidth)
            throw new ArgumentOutOfRangeException(nameof(CalibrationLengthMm));
    }
}

public sealed record ArtworkBounds(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;

    public static ArtworkBounds FromGeometry(MultiPolygon2D geometry)
    {
        if (geometry.Polygons.Count == 0) throw new ArgumentException("Artwork must not be empty.", nameof(geometry));
        var points = geometry.Polygons.SelectMany(p => p.Shell.Concat(p.Holes.SelectMany(h => h))).ToArray();
        if (points.Length == 0) throw new ArgumentException("Artwork must contain coordinates.", nameof(geometry));
        return new ArtworkBounds(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
    }
}

public sealed record PrintTile(
    int Column,
    int Row,
    int Index,
    double MinX,
    double MinY,
    double MaxX,
    double MaxY)
{
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
}

public static class TilingPlanner
{
    public static IReadOnlyList<PrintTile> Plan(ArtworkBounds bounds, PrintProfile profile)
    {
        profile.Validate();
        if (bounds.Width <= 0 || bounds.Height <= 0) throw new ArgumentException("Artwork bounds must have positive area.", nameof(bounds));

        var usableWidth = profile.Paper.Width - 2 * profile.MarginMm;
        var usableHeight = profile.Paper.Height - 2 * profile.MarginMm;
        var xStarts = Starts(bounds.MinX, bounds.MaxX, usableWidth, profile.OverlapMm);
        var yStarts = Starts(bounds.MinY, bounds.MaxY, usableHeight, profile.OverlapMm);

        var result = new List<PrintTile>(xStarts.Count * yStarts.Count);
        var index = 1;
        for (var row = 0; row < yStarts.Count; row++)
        {
            for (var column = 0; column < xStarts.Count; column++)
            {
                var x = xStarts[column];
                var y = yStarts[row];
                result.Add(new PrintTile(column, row, index++, x, y, x + usableWidth, y + usableHeight));
            }
        }
        return result;
    }

    private static IReadOnlyList<double> Starts(double min, double max, double span, double overlap)
    {
        var starts = new List<double> { min };
        if (max - min <= span) return starts;
        var step = span - overlap;
        while (starts[^1] + span < max)
        {
            var next = starts[^1] + step;
            if (next <= starts[^1]) throw new InvalidOperationException("Tiling step must be positive.");
            starts.Add(next);
        }
        return starts;
    }
}
