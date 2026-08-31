namespace ViewpointSign.Geometry;

public sealed class Homography2D
{
    private readonly double[] _h; // row-major 3x3, h[8] = 1

    private Homography2D(double[] h) => _h = h;

    public static Homography2D FromFourPointPairs(
        IReadOnlyList<Point2D> source,
        IReadOnlyList<Point2D> destination,
        double singularEpsilon = 1e-12)
    {
        if (source.Count != 4 || destination.Count != 4)
            throw new ArgumentException("Exactly four point pairs are required.");

        var a = new double[8, 9];
        for (var i = 0; i < 4; i++)
        {
            var x = source[i].X;
            var y = source[i].Y;
            var u = destination[i].X;
            var v = destination[i].Y;
            if (!AllFinite(x, y, u, v)) throw new ArgumentException("Point coordinates must be finite.");

            var r = 2 * i;
            a[r, 0] = x; a[r, 1] = y; a[r, 2] = 1;
            a[r, 6] = -u * x; a[r, 7] = -u * y; a[r, 8] = u;

            a[r + 1, 3] = x; a[r + 1, 4] = y; a[r + 1, 5] = 1;
            a[r + 1, 6] = -v * x; a[r + 1, 7] = -v * y; a[r + 1, 8] = v;
        }

        var solution = Solve(a, singularEpsilon);
        return new Homography2D(new[]
        {
            solution[0], solution[1], solution[2],
            solution[3], solution[4], solution[5],
            solution[6], solution[7], 1.0
        });
    }

    public Point2D Map(Point2D p, double epsilon = 1e-12)
    {
        var w = _h[6] * p.X + _h[7] * p.Y + _h[8];
        if (!double.IsFinite(w) || Math.Abs(w) <= epsilon)
            throw new InvalidOperationException("Homography maps point to infinity or a non-finite location.");

        var x = (_h[0] * p.X + _h[1] * p.Y + _h[2]) / w;
        var y = (_h[3] * p.X + _h[4] * p.Y + _h[5]) / w;
        if (!AllFinite(x, y)) throw new InvalidOperationException("Homography produced non-finite coordinates.");
        return new Point2D(x, y);
    }

    private static double[] Solve(double[,] a, double epsilon)
    {
        const int n = 8;
        for (var col = 0; col < n; col++)
        {
            var pivot = col;
            var pivotAbs = Math.Abs(a[pivot, col]);
            for (var row = col + 1; row < n; row++)
            {
                var candidate = Math.Abs(a[row, col]);
                if (candidate > pivotAbs) { pivot = row; pivotAbs = candidate; }
            }

            if (!double.IsFinite(pivotAbs) || pivotAbs <= epsilon)
                throw new ArgumentException("Point pairs define a singular projective mapping.");

            if (pivot != col)
                for (var k = col; k <= n; k++) (a[col, k], a[pivot, k]) = (a[pivot, k], a[col, k]);

            var divisor = a[col, col];
            for (var k = col; k <= n; k++) a[col, k] /= divisor;

            for (var row = 0; row < n; row++)
            {
                if (row == col) continue;
                var factor = a[row, col];
                if (Math.Abs(factor) <= epsilon) continue;
                for (var k = col; k <= n; k++) a[row, k] -= factor * a[col, k];
            }
        }

        var x = new double[n];
        for (var i = 0; i < n; i++) x[i] = a[i, n];
        return x;
    }

    private static bool AllFinite(params double[] values) => values.All(double.IsFinite);
}
