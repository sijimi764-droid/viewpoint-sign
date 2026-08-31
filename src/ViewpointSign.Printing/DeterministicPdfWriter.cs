using System.Globalization;
using System.Text;
using ViewpointSign.Geometry;

namespace ViewpointSign.Printing;

public sealed record PrintJob(
    string ProjectId,
    string SurfaceId,
    MultiPolygon2D Artwork,
    PrintProfile Profile);

public sealed record PrintedPage(
    int Index,
    PrintTile Tile,
    double WidthMm,
    double HeightMm,
    MultiPolygon2D ClippedArtwork,
    double CalibrationLengthMm,
    string OrientationLabel);

public sealed record GeneratedPdf(byte[] Bytes, IReadOnlyList<PrintedPage> Pages);

public static class PdfUnits
{
    public const double PointsPerInch = 72.0;
    public const double MillimetresPerInch = 25.4;
    public static double MmToPoints(double mm) => mm * PointsPerInch / MillimetresPerInch;
    public static double PointsToMm(double points) => points * MillimetresPerInch / PointsPerInch;
}

public static class DeterministicPdfWriter
{
    private const double MetadataLeftMm = 20.0;
    private const double FooterYmm = 7.0;
    private const double CalibrationXmm = 20.0;
    private const double CalibrationYmm = 2.5;
    private const double CalibrationLabelGapMm = 2.0;

    public static GeneratedPdf Generate(PrintJob job)
    {
        job.Profile.Validate();
        ValidateAscii(job.ProjectId, nameof(job.ProjectId));
        ValidateAscii(job.SurfaceId, nameof(job.SurfaceId));

        var bounds = ArtworkBounds.FromGeometry(job.Artwork);
        var tiles = TilingPlanner.Plan(bounds, job.Profile);
        var pages = tiles.Select(tile => BuildPage(job, tile)).ToArray();
        return new GeneratedPdf(Serialize(job, pages), pages);
    }

    private static PrintedPage BuildPage(PrintJob job, PrintTile tile)
    {
        var tilePolygon = new MultiPolygon2D(new[]
        {
            new Polygon2D(new[]
            {
                new Point2D(tile.MinX, tile.MinY),
                new Point2D(tile.MaxX, tile.MinY),
                new Point2D(tile.MaxX, tile.MaxY),
                new Point2D(tile.MinX, tile.MaxY)
            })
        });
        var clipped = job.Artwork.Intersect(tilePolygon);
        var orientation = job.Profile.Paper.Width >= job.Profile.Paper.Height ? "LANDSCAPE" : "PORTRAIT";
        return new PrintedPage(
            tile.Index,
            tile,
            job.Profile.Paper.Width,
            job.Profile.Paper.Height,
            clipped,
            job.Profile.CalibrationLengthMm,
            orientation);
    }

    private static byte[] Serialize(PrintJob job, IReadOnlyList<PrintedPage> pages)
    {
        var objects = new SortedDictionary<int, string>();
        const int catalogId = 1;
        const int pagesId = 2;
        const int fontId = 3;

        var pageIds = new List<int>();
        var nextId = 4;
        foreach (var page in pages)
        {
            var pageId = nextId++;
            var contentId = nextId++;
            pageIds.Add(pageId);

            var content = BuildContent(job, page);
            var contentBytes = Encoding.ASCII.GetByteCount(content);
            objects[contentId] = $"<< /Length {contentBytes} >>\nstream\n{content}endstream";

            var widthPt = F(PdfUnits.MmToPoints(page.WidthMm));
            var heightPt = F(PdfUnits.MmToPoints(page.HeightMm));
            objects[pageId] = $"<< /Type /Page /Parent {pagesId} 0 R /MediaBox [0 0 {widthPt} {heightPt}] /Resources << /Font << /F1 {fontId} 0 R >> >> /Contents {contentId} 0 R >>";
        }

        objects[catalogId] = $"<< /Type /Catalog /Pages {pagesId} 0 R >>";
        objects[pagesId] = $"<< /Type /Pages /Count {pages.Count} /Kids [{string.Join(' ', pageIds.Select(id => $"{id} 0 R"))}] >>";
        objects[fontId] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";

        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n%VPS1\n");
        var offsets = new Dictionary<int, long>();
        foreach (var pair in objects)
        {
            offsets[pair.Key] = stream.Position;
            WriteAscii(stream, $"{pair.Key} 0 obj\n{pair.Value}\nendobj\n");
        }

        var xrefOffset = stream.Position;
        var maxId = objects.Keys.Max();
        WriteAscii(stream, $"xref\n0 {maxId + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        for (var id = 1; id <= maxId; id++)
        {
            if (offsets.TryGetValue(id, out var offset)) WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
            else WriteAscii(stream, "0000000000 00000 f \n");
        }
        WriteAscii(stream, $"trailer\n<< /Size {maxId + 1} /Root {catalogId} 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static string BuildContent(PrintJob job, PrintedPage page)
    {
        var profile = job.Profile;
        var sb = new StringBuilder();
        sb.AppendLine("q");
        sb.AppendLine("1 1 1 rg");
        sb.AppendLine($"0 0 {F(PdfUnits.MmToPoints(page.WidthMm))} {F(PdfUnits.MmToPoints(page.HeightMm))} re f");
        sb.AppendLine("0 0 0 rg");
        sb.AppendLine("0 0 0 RG");
        sb.AppendLine("0.5 w");

        var pageCount = TilingPlanner.Plan(ArtworkBounds.FromGeometry(job.Artwork), profile).Count;
        AppendText(sb, MetadataLeftMm, page.HeightMm - 5.0, 8, $"PROJECT {job.ProjectId} | SURFACE {job.SurfaceId} | PAGE {page.Index}/{pageCount}");
        AppendText(sb, MetadataLeftMm, FooterYmm, 6, $"ORIENTATION {page.OrientationLabel} | TILE C{page.Tile.Column + 1} R{page.Tile.Row + 1} | PRINT 1:1");

        // Keep the physical calibration ruler isolated below the printable artwork and away from corner registration marks.
        AppendLine(sb, CalibrationXmm, CalibrationYmm, CalibrationXmm + page.CalibrationLengthMm, CalibrationYmm, 0.35);
        AppendText(sb, CalibrationXmm + page.CalibrationLengthMm + CalibrationLabelGapMm, 1.5, 5.5, $"CAL {F(page.CalibrationLengthMm)} mm");

        AppendRegistrationMarks(sb, profile);

        foreach (var polygon in page.ClippedArtwork.Polygons)
        {
            AppendRing(sb, polygon.Shell, page.Tile, profile.MarginMm);
            foreach (var hole in polygon.Holes) AppendRing(sb, hole, page.Tile, profile.MarginMm);
            sb.AppendLine("f*");
        }

        // Machine-readable comments for deterministic acceptance inspection.
        sb.AppendLine($"%VPS PROJECT={job.ProjectId}");
        sb.AppendLine($"%VPS SURFACE={job.SurfaceId}");
        sb.AppendLine($"%VPS PAGE={page.Index}");
        sb.AppendLine($"%VPS TILE={F(page.Tile.MinX)},{F(page.Tile.MinY)},{F(page.Tile.MaxX)},{F(page.Tile.MaxY)} mm");
        sb.AppendLine($"%VPS OVERLAP={F(profile.OverlapMm)} mm");
        sb.AppendLine($"%VPS CALIBRATION={F(page.CalibrationLengthMm)} mm");
        sb.AppendLine("Q");
        return sb.ToString();
    }

    private static void AppendRing(StringBuilder sb, IReadOnlyList<Point2D> ring, PrintTile tile, double marginMm)
    {
        if (ring.Count < 3) return;
        var first = ToPage(ring[0], tile, marginMm);
        sb.AppendLine($"{F(PdfUnits.MmToPoints(first.X))} {F(PdfUnits.MmToPoints(first.Y))} m");
        for (var i = 1; i < ring.Count; i++)
        {
            var p = ToPage(ring[i], tile, marginMm);
            sb.AppendLine($"{F(PdfUnits.MmToPoints(p.X))} {F(PdfUnits.MmToPoints(p.Y))} l");
        }
        sb.AppendLine("h");
    }

    private static Point2D ToPage(Point2D local, PrintTile tile, double marginMm) =>
        new(marginMm + local.X - tile.MinX, marginMm + local.Y - tile.MinY);

    private static void AppendRegistrationMarks(StringBuilder sb, PrintProfile profile)
    {
        var left = profile.MarginMm;
        var right = profile.Paper.Width - profile.MarginMm;
        var bottom = profile.MarginMm;
        var top = profile.Paper.Height - profile.MarginMm;
        const double arm = 4.0;
        foreach (var (x, y) in new[] { (left, bottom), (right, bottom), (left, top), (right, top) })
        {
            AppendLine(sb, x - arm, y, x + arm, y, 0.25);
            AppendLine(sb, x, y - arm, x, y + arm, 0.25);
        }
    }

    private static void AppendLine(StringBuilder sb, double x1Mm, double y1Mm, double x2Mm, double y2Mm, double widthPt)
    {
        sb.AppendLine($"{F(widthPt)} w");
        sb.AppendLine($"{F(PdfUnits.MmToPoints(x1Mm))} {F(PdfUnits.MmToPoints(y1Mm))} m {F(PdfUnits.MmToPoints(x2Mm))} {F(PdfUnits.MmToPoints(y2Mm))} l S");
    }

    private static void AppendText(StringBuilder sb, double xMm, double yMm, double sizePt, string text)
    {
        sb.AppendLine("BT");
        sb.AppendLine($"/F1 {F(sizePt)} Tf");
        sb.AppendLine($"{F(PdfUnits.MmToPoints(xMm))} {F(PdfUnits.MmToPoints(yMm))} Td");
        sb.AppendLine($"({EscapePdfString(text)}) Tj");
        sb.AppendLine("ET");
    }

    private static string EscapePdfString(string text) => text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string F(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void ValidateAscii(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
        if (value.Any(c => c < 32 || c > 126)) throw new ArgumentException("PoC PDF metadata is limited to printable ASCII.", name);
    }
}
