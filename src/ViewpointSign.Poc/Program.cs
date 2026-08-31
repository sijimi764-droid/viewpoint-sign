using System.Globalization;
using System.Text;
using ViewpointSign.Geometry;
using ViewpointSign.Printing;
using ViewpointSign.Projection;

namespace ViewpointSign.Poc;

public static class Program
{
    public static int Main(string[] args)
    {
        var outputDirectory = args.Length > 0 ? args[0] : Path.Combine("artifacts", "g3-physical");
        Directory.CreateDirectory(outputDirectory);

        var surfaces = G3Fixture.Surfaces();
        var result = VirtualProjector.Project(
            G3Fixture.Viewer,
            G3Fixture.Target,
            G3Fixture.Glyphs(),
            surfaces,
            G3Fixture.Tolerance);

        if (result.GapGeometry.Area > 1e-6 || result.MaxRoundTripErrorMm > G3Fixture.Tolerance.ReconstructedContourToleranceMm)
            throw new InvalidOperationException($"G3 fixture does not pass virtual acceptance. Gap={result.GapGeometry.Area:R}, error={result.MaxRoundTripErrorMm:R} mm.");

        var profile = PrintProfile.A4(PaperOrientation.Portrait, marginMm: 10.0, overlapMm: 10.0);
        foreach (var surface in surfaces)
        {
            var artwork = MultiPolygon2D.Empty;
            foreach (var fragment in result.Fragments.Where(f => f.SurfaceId == surface.Id))
                artwork = artwork.Union(fragment.LocalArtwork);

            if (artwork.Polygons.Count == 0)
                throw new InvalidOperationException($"Surface {surface.Id} received no artwork.");

            var pdf = DeterministicPdfWriter.Generate(new PrintJob(G3Fixture.ProjectId, surface.Id, artwork, profile));
            File.WriteAllBytes(Path.Combine(outputDirectory, $"{surface.Id}.pdf"), pdf.Bytes);
        }

        File.WriteAllText(Path.Combine(outputDirectory, "SETUP.csv"), BuildSetupCsv(surfaces), Encoding.UTF8);
        File.WriteAllText(Path.Combine(outputDirectory, "MANIFEST.txt"), BuildManifest(result, surfaces), Encoding.UTF8);
        return 0;
    }

    private static string BuildSetupCsv(IReadOnlyList<RectSurface3D> surfaces)
    {
        var sb = new StringBuilder();
        sb.AppendLine("surface_id,origin_x_mm,origin_y_mm,origin_z_mm,right_dx,right_dy,right_dz,width_mm,height_mm");
        foreach (var s in surfaces)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                s.Id,
                F(s.Origin.X), F(s.Origin.Y), F(s.Origin.Z),
                F(s.Eu.X), F(s.Eu.Y), F(s.Eu.Z),
                F(s.Width), F(s.Height)
            }));
        }
        return sb.ToString();
    }

    private static string BuildManifest(VirtualProjectionResult result, IReadOnlyList<RectSurface3D> surfaces)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"PROJECT={G3Fixture.ProjectId}");
        sb.AppendLine($"VIEWER_X_MM={F(G3Fixture.Viewer.X)}");
        sb.AppendLine($"VIEWER_Y_MM={F(G3Fixture.Viewer.Y)}");
        sb.AppendLine($"VIEWER_Z_MM={F(G3Fixture.Viewer.Z)}");
        sb.AppendLine("REFERENCE_VIEW_DIRECTION=+Y");
        sb.AppendLine($"TARGET_PLANE_Y_MM={F(G3Fixture.Target.Origin.Y)}");
        sb.AppendLine($"VIRTUAL_GAP_AREA_MM2={F(result.GapGeometry.Area)}");
        sb.AppendLine($"MAX_ROUNDTRIP_ERROR_MM={F(result.MaxRoundTripErrorMm)}");
        sb.AppendLine($"SURFACE_COUNT={surfaces.Count}");
        sb.AppendLine("PRINT=Actual size / 100% / no fit-to-page");
        sb.AppendLine("CALIBRATION=Measure the printed 100 mm line before installation");
        return sb.ToString();
    }

    private static string F(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
