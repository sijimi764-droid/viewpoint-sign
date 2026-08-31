using System.Text;
using ViewpointSign.Geometry;
using ViewpointSign.Printing;
using Xunit;

namespace ViewpointSign.Tests;

public sealed class G2PrintIntegrityTests
{
    [Theory]
    [InlineData(PaperOrientation.Portrait, 210.0, 297.0)]
    [InlineData(PaperOrientation.Landscape, 297.0, 210.0)]
    public void A4_PageDimensions_AreEncodedInPhysicalPdfPoints(PaperOrientation orientation, double widthMm, double heightMm)
    {
        var profile = PrintProfile.A4(orientation);
        var pdf = DeterministicPdfWriter.Generate(new PrintJob("P001", "S1", Rect(0, 0, 50, 50), profile));
        var text = Encoding.ASCII.GetString(pdf.Bytes);
        var expectedMediaBox = $"/MediaBox [0 0 {F(PdfUnits.MmToPoints(widthMm))} {F(PdfUnits.MmToPoints(heightMm))}]";

        Assert.Single(pdf.Pages);
        Assert.Equal(widthMm, pdf.Pages[0].WidthMm, 8);
        Assert.Equal(heightMm, pdf.Pages[0].HeightMm, 8);
        Assert.Contains(expectedMediaBox, text);
        Assert.StartsWith("%PDF-1.4", text);
        Assert.EndsWith("%%EOF\n", text);
    }

    [Fact]
    public void CalibrationLine_IsExactly100MillimetresInPdfCoordinates()
    {
        var profile = PrintProfile.A4(PaperOrientation.Portrait);
        var pdf = DeterministicPdfWriter.Generate(new PrintJob("P001", "S1", Rect(0, 0, 50, 50), profile));
        var text = Encoding.ASCII.GetString(pdf.Bytes);
        const double xMm = 20.0;
        const double yMm = 2.5;
        var expected = $"{F(PdfUnits.MmToPoints(xMm))} {F(PdfUnits.MmToPoints(yMm))} m {F(PdfUnits.MmToPoints(xMm + 100.0))} {F(PdfUnits.MmToPoints(yMm))} l S";

        Assert.Equal(100.0, pdf.Pages[0].CalibrationLengthMm, 8);
        Assert.Contains(expected, text);
        var encodedLengthPt = PdfUnits.MmToPoints(xMm + 100.0) - PdfUnits.MmToPoints(xMm);
        Assert.Equal(100.0, PdfUnits.PointsToMm(encodedLengthPt), 10);
    }

    [Fact]
    public void Artwork_IsPreservedAtOneToOneScale()
    {
        var artwork = Rect(0, 0, 100, 50);
        var profile = PrintProfile.A4(PaperOrientation.Landscape);
        var pdf = DeterministicPdfWriter.Generate(new PrintJob("P001", "S1", artwork, profile));

        Assert.Single(pdf.Pages);
        var bounds = ArtworkBounds.FromGeometry(pdf.Pages[0].ClippedArtwork);
        Assert.Equal(100.0, bounds.Width, 8);
        Assert.Equal(50.0, bounds.Height, 8);
        Assert.Equal(100.0, PdfUnits.PointsToMm(PdfUnits.MmToPoints(bounds.Width)), 10);
        Assert.Equal(50.0, PdfUnits.PointsToMm(PdfUnits.MmToPoints(bounds.Height)), 10);
    }

    [Fact]
    public void Tiling_CoversArtworkAndUsesConfiguredTenMillimetreOverlap()
    {
        var artwork = Rect(0, 0, 400, 300);
        var profile = PrintProfile.A4(PaperOrientation.Landscape, marginMm: 10.0, overlapMm: 10.0);
        var bounds = ArtworkBounds.FromGeometry(artwork);
        var tiles = TilingPlanner.Plan(bounds, profile);

        Assert.Equal(4, tiles.Count);
        var row0 = tiles.Where(t => t.Row == 0).OrderBy(t => t.Column).ToArray();
        var col0 = tiles.Where(t => t.Column == 0).OrderBy(t => t.Row).ToArray();
        Assert.Equal(10.0, row0[0].MaxX - row0[1].MinX, 8);
        Assert.Equal(10.0, col0[0].MaxY - col0[1].MinY, 8);

        var covered = MultiPolygon2D.Empty;
        foreach (var tile in tiles) covered = covered.Union(TileGeometry(tile).Intersect(artwork));
        Assert.InRange(artwork.Difference(covered).Area, 0, 1e-8);
    }

    [Fact]
    public void EveryPage_ContainsIdentificationOrientationRegistrationAndOverlapMetadata()
    {
        var profile = PrintProfile.A4(PaperOrientation.Landscape, overlapMm: 10.0);
        var pdf = DeterministicPdfWriter.Generate(new PrintJob("PROJECT-42", "SURFACE-A", Rect(0, 0, 400, 300), profile));
        var text = Encoding.ASCII.GetString(pdf.Bytes);

        Assert.True(pdf.Pages.Count > 1);
        foreach (var page in pdf.Pages)
        {
            Assert.Contains($"PROJECT PROJECT-42 | SURFACE SURFACE-A | PAGE {page.Index}/{pdf.Pages.Count}", text);
            Assert.Contains($"%VPS PAGE={page.Index}", text);
        }
        Assert.Contains("ORIENTATION LANDSCAPE", text);
        Assert.Contains("PRINT 1:1", text);
        Assert.Contains("%VPS OVERLAP=10 mm", text);

        // Four crosshair registration marks per page produce horizontal and vertical strokes.
        Assert.Contains(" l S", text);
    }

    [Fact]
    public void GeneratedPdf_IsDeterministicForIdenticalInput()
    {
        var job = new PrintJob("P001", "S1", Rect(0, 0, 250, 120), PrintProfile.A4(PaperOrientation.Landscape));
        var first = DeterministicPdfWriter.Generate(job).Bytes;
        var second = DeterministicPdfWriter.Generate(job).Bytes;
        Assert.Equal(first, second);
    }

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

    private static MultiPolygon2D TileGeometry(PrintTile tile) => Rect(tile.MinX, tile.MinY, tile.Width, tile.Height);

    private static string F(double value) => value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
}
