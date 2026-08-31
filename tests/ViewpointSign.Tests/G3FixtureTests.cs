using ViewpointSign.Geometry;
using ViewpointSign.Poc;
using ViewpointSign.Printing;
using ViewpointSign.Projection;
using Xunit;

namespace ViewpointSign.Tests;

public sealed class G3FixtureTests
{
    [Fact]
    public void PhysicalFixture_PassesVirtualReprojectionAndUsesAllThreeSurfaces()
    {
        var surfaces = G3Fixture.Surfaces();
        var result = VirtualProjector.Project(
            G3Fixture.Viewer,
            G3Fixture.Target,
            G3Fixture.Glyphs(),
            surfaces,
            G3Fixture.Tolerance);

        Assert.InRange(result.GapGeometry.Area, 0, 1e-6);
        Assert.InRange(result.MaxRoundTripErrorMm, 0, G3Fixture.Tolerance.ReconstructedContourToleranceMm);
        Assert.Equal(3, result.Fragments.Select(f => f.SurfaceId).Distinct().Count());

        foreach (var surface in surfaces)
        {
            var artwork = MultiPolygon2D.Empty;
            foreach (var fragment in result.Fragments.Where(f => f.SurfaceId == surface.Id))
                artwork = artwork.Union(fragment.LocalArtwork);
            Assert.True(artwork.Area > 0);

            var pdf = DeterministicPdfWriter.Generate(new PrintJob(
                G3Fixture.ProjectId,
                surface.Id,
                artwork,
                PrintProfile.A4(PaperOrientation.Portrait, marginMm: 10.0, overlapMm: 10.0)));
            Assert.Single(pdf.Pages);
        }
    }
}
