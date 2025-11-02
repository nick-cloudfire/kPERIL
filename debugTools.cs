namespace kperil
{
    class DebugTools
    {
        public static void PrintRaster(float[][] raster)
        {
            if (raster == null || raster.Length == 0)
            {
                Console.WriteLine("Raster is empty or null.");
                return;
            }

            int nrows = raster.Length;
            int ncols = raster[0].Length;

            Console.WriteLine($"Raster size: {nrows} x {ncols}\n");

            for (int i = 0; i < nrows; i++)
            {
                for (int j = 0; j < ncols; j++)
                {
                    Console.Write($"{raster[i][j],8:F2} ");
                }
                Console.WriteLine();
            }
        }
    }
}