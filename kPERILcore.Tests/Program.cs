using Core = kPERIL.kPERIL;

namespace kPERIL.Tests
{
    /// <summary>
    /// kPERILcore's modelling, checked on synthetic rasters in its own layout: [row from the top (north), column],
    /// directions as compass bearings (0 = north, clockwise), wind given as the direction it blows from, aspect as
    /// the downhill bearing.
    /// </summary>
    public static class Program
    {
        private static int _failed, _passed;

        public static int Main()
        {
            LengthToBreadthIsElmfires();
            EffectiveWindAddsUpslope();
            DerivedAspectIsACompassBearing();
            BoundaryLiesUpwind();
            Console.WriteLine($"{_passed} passed, {_failed} failed");
            return _failed == 0 ? 0 : 1;
        }

        private static void Check(bool ok, string what)
        {
            if (ok) { ++_passed; Console.WriteLine("PASS " + what); }
            else { ++_failed; Console.WriteLine("FAIL " + what); }
        }

        private static void Near(double measured, double expected, double tolerance, string what) =>
            Check(Math.Abs(measured - expected) <= tolerance, $"{what}: {measured:G6} vs {expected:G6} (+/- {tolerance:G3})");

        /// <summary>ELMFIRE's form with U in mi/h equals Anderson's coefficients applied to U in m/s.</summary>
        private static void LengthToBreadthIsElmfires()
        {
            Near(Core.LengthToBreadth(0.0), 1.0, 1e-12, "L/B at calm is 1");
            const double mph = 4.235; // case 1 of WUInity's verification: 6 m/s at 10 m over a 1 ft fuel bed
            double ms = mph * 0.44704;
            double anderson = 0.936 * Math.Exp(0.2566 * ms) + 0.461 * Math.Exp(-0.1548 * ms) - 0.397;
            Near(Core.LengthToBreadth(mph), anderson, 2e-4, "L/B at 4.235 mi/h is Anderson's with U in m/s");
            Near(Core.LengthToBreadth(mph), 1.468, 1e-3, "L/B at 4.235 mi/h");
            Near(Core.LengthToBreadth(40.0), Core.DefaultMaxLengthToBreadth, 0.0, "L/B is capped at 8 by default");
            Near(Core.LengthToBreadth(40.0, 3.0), 3.0, 0.0, "L/B is capped at the given maximum");
        }

        private static float[,] Fill(int rows, int cols, Func<int, int, float> f)
        {
            var r = new float[rows, cols];
            for (int i = 0; i < rows; ++i) for (int j = 0; j < cols; ++j) r[i, j] = f(i, j);
            return r;
        }

        /// <summary>A plane rising northward at <paramref name="slopeDeg"/>: its aspect is 180 and upslope is north.</summary>
        private static Core SlopeNorth(int n, float cell, float slopeDeg, float windMph, float windFrom)
        {
            var k = new Core();
            k.perilData.importFireRastersByVariable(Fill(n, n, (r, c) => 1f), Fill(n, n, (r, c) => 0f));
            k.perilData.cellSize = cell;
            float rise = (float)Math.Tan(slopeDeg * Math.PI / 180.0) * cell;
            k.perilData.importTopographyRastersByVariable(Fill(n, n, (r, c) => (n - 1 - r) * rise));
            k.perilData.importWeatherRastersByVariable(Fill(n, n, (r, c) => windMph), Fill(n, n, (r, c) => windFrom));
            return k;
        }

        /// <summary>An upslope wind and the slope push the fire the same way, so 0.06 x slope adds to the wind.</summary>
        private static void EffectiveWindAddsUpslope()
        {
            const int n = 9, mid = 4;
            Core up = SlopeNorth(n, 10f, 20f, 4.23f, 180f);     // from the south: blows upslope
            Near(up.perilData.slopeRaster[mid, mid], 20.0, 1e-3, "derived slope of a 20 degree plane");
            Near(up.perilData.effectiveMidflameWindspeed[mid, mid], 4.23 + 0.06 * 20, 1e-3, "upslope wind: 4.23 + 1.2 mi/h");
            Core down = SlopeNorth(n, 10f, 20f, 4.23f, 0f);     // from the north: blows downslope
            Near(down.perilData.effectiveMidflameWindspeed[mid, mid], 4.23 - 0.06 * 20, 1e-3, "downslope wind: 4.23 - 1.2 mi/h");
            Core cross = SlopeNorth(n, 10f, 20f, 4.23f, 90f);   // from the east: across the slope
            Near(cross.perilData.effectiveMidflameWindspeed[mid, mid], Math.Sqrt(4.23 * 4.23 + 1.2 * 1.2), 1e-3, "cross-slope wind: added at right angles");

            // The same, from a supplied slope and aspect rather than derived ones.
            var k = new Core();
            k.perilData.importFireRastersByVariable(Fill(n, n, (r, c) => 1f), Fill(n, n, (r, c) => 0f));
            k.perilData.cellSize = 10f;
            k.perilData.importTopographyRastersByVariable(Fill(n, n, (r, c) => 0f), Fill(n, n, (r, c) => 20f), Fill(n, n, (r, c) => 180f));
            k.perilData.importWeatherRastersByVariable(Fill(n, n, (r, c) => 4.23f), Fill(n, n, (r, c) => 180f));
            Near(k.perilData.effectiveMidflameWindspeed[mid, mid], 5.43, 1e-3, "upslope wind with a supplied aspect of 180");
        }

        private static void DerivedAspectIsACompassBearing()
        {
            const int n = 9, mid = 4;
            Near(SlopeNorth(n, 10f, 20f, 0f, 0f).perilData.aspectRaster[mid, mid], 180.0, 1e-3, "a plane falling south has aspect 180");
            var k = new Core();
            k.perilData.importFireRastersByVariable(Fill(n, n, (r, c) => 1f), Fill(n, n, (r, c) => 0f));
            k.perilData.cellSize = 10f;
            k.perilData.importTopographyRastersByVariable(Fill(n, n, (r, c) => (n - 1 - c) * 3f)); // falls eastward
            Near(k.perilData.aspectRaster[mid, mid], 90.0, 1e-3, "a plane falling east has aspect 90");
        }

        /// <summary>
        /// Uniform fire spreading east on flat ground, a 3x3 WUI area: the boundary reaches upwind (west) as far as the
        /// head fire travels in rset, further than downwind, and is symmetric about the wind axis.
        /// </summary>
        private static void BoundaryLiesUpwind()
        {
            const int n = 61, lo = 29, hi = 31;
            const float ros = 10f, cell = 10f, rset = 15f;
            var k = new Core();
            k.perilData.importFireRastersByVariable(Fill(n, n, (r, c) => ros), Fill(n, n, (r, c) => 90f));
            k.perilData.cellSize = cell;
            k.perilData.noDataValue = -9999f;
            k.perilData.importTopographyRastersByVariable(new float[n, n]);
            k.perilData.importWeatherRastersByVariable(Fill(n, n, (r, c) => 4f), Fill(n, n, (r, c) => 270f));
            k.perilData.importWuiRastersByVariable(Fill(n, n, (r, c) => r >= lo && r <= hi && c >= lo && c <= hi ? 1f : 0f));
            float[,] b = k.getTriggerBoundary(k.getTravelTimeFromBrokenDownRos(k.breakdownRateOfSpread()), rset);

            int west = 0, east = 0, north = 0, south = 0;
            bool symmetric = true;
            for (int r = 0; r < n; ++r)
            {
                for (int c = 0; c < n; ++c)
                {
                    if (b[r, c] != 1f) continue;
                    if (c < lo) west = Math.Max(west, lo - c);
                    if (c > hi) east = Math.Max(east, c - hi);
                    if (r < lo) north = Math.Max(north, lo - r);
                    if (r > hi) south = Math.Max(south, r - hi);
                    if (b[n - 1 - r, c] != 1f) symmetric = false;
                }
            }
            Check(west == (int)Math.Floor(rset * ros / cell), $"upwind reach is rset x ROS / cell = 15 cells: {west}");
            Check(west > 2 * east, $"the boundary lies upwind: west {west}, east {east} cells");
            Check(symmetric && north == south, $"symmetric about the wind axis: north {north}, south {south} cells");
        }
    }
}
