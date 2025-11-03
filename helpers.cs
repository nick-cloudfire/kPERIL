namespace kperil
{
    class Helpers
    {
        private static float[][] ToJagged(float[,] source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            int rows = source.GetLength(0);
            int cols = source.GetLength(1);
            var result = new float[rows][];

            for (int r = 0; r < rows; r++)
            {
                var row = new float[cols];
                for (int c = 0; c < cols; c++)
                {
                    row[c] = source[r, c];
                }
                result[r] = row;
            }

            return result;
        }

        public static float[][] CopyJagged(float[][] src)
        {
            var copy = new float[src.Length][];
            for (int i = 0; i < src.Length; i++)
            {
                copy[i] = (float[])src[i].Clone();
            }
            return copy;
        }
    }
}