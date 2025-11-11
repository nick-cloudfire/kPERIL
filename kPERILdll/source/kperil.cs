using kperil;
using OSGeo.GDAL;

namespace kPERIL
{
    public class kPERIL
    {
        public perilData perilData;

        public kPERIL()
        {
            this.perilData = new perilData();
            Gdal.AllRegister();
        }

        public float[][][] breakdownRateOfSpread()
        {
            perilData.VerifyAllRastersImported();

            int width = perilData.totalX;
            int height = perilData.totalY;

            float a, b, c, rosX, rosY;
            float[][][] brokenDownRateOfSpread = new float[width][][];

            //set the size of the broken down ROS raster and at the same time calculate each value.
            for (int x = 0; x < width; x++)
            {
                brokenDownRateOfSpread[x] = new float[height][];
                for (int y = 0; y < height; y++)
                {
                    brokenDownRateOfSpread[x][y] = new float[8];

                    if (perilData.rateOfSpreadMagnitudeRaster[x][y] >= 0 && perilData.rateOfSpreadMagnitudeRaster[x][y] != perilData.noDataValue)
                    {


                        double lb = 0.936 * Math.Exp(0.2566 * perilData.effectiveMidflameWindspeed[x][y]) + 0.461 * Math.Exp(-0.1548 * perilData.effectiveMidflameWindspeed[x][y]) - 0.397; //Calculate length to Breadth ratio of Huygens ellipse
                        double hb = (lb + (float)Math.Sqrt(lb * lb - 1)) / (lb - (float)Math.Sqrt(lb * lb - 1));    //calculate head to back ratio

                        a = (perilData.rateOfSpreadMagnitudeRaster[x][y] / (2 * (float)lb)) * (1 + 1 / (float)hb);
                        b = (perilData.rateOfSpreadMagnitudeRaster[x][y] * 0.5f) * (1 + 1 / (float)hb);
                        c = (perilData.rateOfSpreadMagnitudeRaster[x][y] * 0.5f) * (1 - 1 / (float)hb);

                        for (int dir = 0; dir < 8; dir++)
                        {
                            rosX = a * (float)Math.Sin((Math.PI * dir / 4) - perilData.rateOfSpreadDirectionRaster[x][y] * 2 * Math.PI / 360);
                            rosY = c + b * (float)Math.Cos((Math.PI * dir / 4) - perilData.rateOfSpreadDirectionRaster[x][y] * 2 * Math.PI / 360);
                            brokenDownRateOfSpread[x][y][dir] = (float)Math.Sqrt(Math.Pow(rosX, 2) + Math.Pow(rosY, 2));
                        }
                    }
                    else
                    {
                        for (int dir = 0; dir < 8; dir++)
                        {
                            brokenDownRateOfSpread[x][y][dir] = 0;
                        }
                    }
                }
            }
            return brokenDownRateOfSpread;
        }

        /// <summary>
        /// Calculate time taken for the fire to travel FROM cell x,y TO its neighbor in direction dir. 
        /// </summary>
        /// <param name="brokenDownRateOfSpread"></param>
        /// <returns></returns>
        public float[][][] getTravelTimeFromBrokenDownRos(float[][][] brokenDownRateOfSpread)
        {
            int width = perilData.totalX;
            int height = perilData.totalY;

            float[][][] travelTimeToNextCell = new float[width][][];

            //set the size of the broken down ROS raster and at the same time calculate each value.
            for (int x = 1; x < width - 1; x++)
            {
                travelTimeToNextCell[x] = new float[height][];
                for (int y = 1; y < height - 1; y++)
                {
                    travelTimeToNextCell[x][y] = new float[8];

                    if (perilData.rateOfSpreadMagnitudeRaster[x][y] >= 0 && perilData.rateOfSpreadMagnitudeRaster[x][y] != perilData.noDataValue)
                    {

                        for (int dir = 0; dir < 8; dir++)
                        {
                            float additionalEdgeTravel = (dir % 2)==0 ? 1 : 1.4142f;
                            (int xn, int yn) = getNeighborDims(x, y, dir);
                            travelTimeToNextCell[x][y][dir] = 2 * perilData.cellSize * additionalEdgeTravel / (brokenDownRateOfSpread[x][y][dir] + brokenDownRateOfSpread[xn][yn][dir]);
                        }
                    }
                    else
                    {
                        for (int dir = 0; dir < 8; dir++)
                        {
                            travelTimeToNextCell[x][y][dir] = float.NaN;
                        }
                    }
                }
            }
            travelTimeToNextCell = fillRasterEdgesByCopy(travelTimeToNextCell);
            
            return travelTimeToNextCell;
        }

        private float[][][] fillRasterEdgesByCopy(float[][][] raster)
        {
            int width = raster.Length;
            if (width == 0)
            {
                return raster;
            }

            int height = 0;
            for (int x = 0; x < width; x++)
            {
                if (raster[x] != null)
                {
                    height = raster[x].Length;
                    break;
                }
            }

            if (height == 0)
            {
                throw new InvalidOperationException("Raster has no rows populated; nothing to copy.");
            }

            const int directions = 8;

            void EnsureCell(int x, int y)
            {
                raster[x] ??= new float[height][];
                raster[x][y] ??= new float[directions];
            }

            void CopyCell(int dstX, int dstY, int srcX, int srcY)
            {
                EnsureCell(srcX, srcY);
                EnsureCell(dstX, dstY);
                Array.Copy(raster[srcX][srcY], raster[dstX][dstY], directions);
            }

            // Top and bottom edges (copy from the first inner row)
            for (int x = 1; x < width - 1; x++)
            {
                CopyCell(x, 0, x, 1);
                CopyCell(x, height - 1, x, height - 2);
            }

            // Left and right edges (copy from the first inner column)
            for (int y = 0; y < height; y++)
            {
                CopyCell(0, y, 1, y);
                CopyCell(width - 1, y, width - 2, y);
            }

            return raster;
        }

        public float[][] breadthFirstSearchSingleCell(
            int x_start,
            int y_start,
            float aset,
            float[][][] travelTimeToNextCell)
        {
            int width = perilData.totalX;
            int height = perilData.totalY;

            if (x_start < 0 || x_start >= width || y_start < 0 || y_start >= height)
            {
                throw new ArgumentOutOfRangeException("Start cell is outside the raster bounds.");
            }

            if (perilData.wuiAreaRaster[x_start][y_start] == 2)
            {
                throw new InvalidOperationException("Start cell is internal to the WUI area.");
            }

            float[][] singleCellBfs = perilData.initiateJaggedArray();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    singleCellBfs[x][y] = float.PositiveInfinity;
                }
            }

            var frontier = new PriorityQueue<(int x, int y), float>();
            singleCellBfs[x_start][y_start] = 0f;
            frontier.Enqueue((x_start, y_start), 0f);

            while (frontier.TryDequeue(out var current, out float currentCost))
            {
                if (currentCost > singleCellBfs[current.x][current.y])
                {
                    continue; // already found a cheaper path
                }

                for (int dir = 0; dir < 8; dir++)
                {
                    (int nx, int ny) = getNeighborDims(current.x, current.y, dir);

                    if (nx < 0 || ny < 0 || nx >= perilData.totalX || ny >= perilData.totalY) continue; //point out of range
                    if ((perilData.wuiAreaRaster != null && perilData.wuiAreaRaster[nx][ny] != 0)) continue; // blocked or unreached cell

                    float stepCost = travelTimeToNextCell[nx][ny][(dir + 4) % 8];

                    if (stepCost <= 0f || float.IsNaN(stepCost) || float.IsInfinity(stepCost)) continue; // invalid travel time

                    float newCost = currentCost + stepCost;
                    if (newCost < singleCellBfs[nx][ny])
                    {
                        singleCellBfs[nx][ny] = newCost;
                        if (newCost <= aset) frontier.Enqueue((nx, ny), newCost);
                    }
                }
            }
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (singleCellBfs[x][y] == float.PositiveInfinity) singleCellBfs[x][y] = float.NaN;
                }
            }

            return singleCellBfs;
        }


        public float[][] getTriggerBoundary(float[][][] travelTimeToNextCell, float aset)
        {
            float[][] triggerBoundary = perilData.initiateJaggedArray();
            float[][] wuiArea = wuiAreaAdjusted();

            for (int x = 0; x < perilData.totalX; x++)
            {
                for (int y = 0; y < perilData.totalY; y++)
                {
                    if (wuiArea[x][y] == 1)
                    {
                        var singleCellTriggerBoundary = breadthFirstSearchSingleCell(x, y, aset, travelTimeToNextCell);
                        for (int xb = 0; xb < perilData.totalX; xb++)
                        {
                            for (int yb = 0; yb < perilData.totalY; yb++)
                            {
                                if (singleCellTriggerBoundary[xb][yb] >= 0) triggerBoundary[xb][yb] = 1;
                            }
                        }
                    }
                }
            }

            for (int x = 0; x < perilData.totalX; x++)
            {
                for (int y = 0; y < perilData.totalY; y++)
                {
                    if (wuiArea[x][y] > 0)
                    {
                        triggerBoundary[x][y] = wuiArea[x][y] + 1;
                    }
                }
            }
            return triggerBoundary;
        }
        private float[][] wuiAreaAdjusted()
        {
            float[][] wuiAreaAdjusted = Helpers.CopyJagged(perilData.wuiAreaRaster);

            for (int x = 1; x < perilData.totalX - 1; x++)
            {
                for (int y = 1; y < perilData.totalY - 1; y++)
                {
                    if (perilData.wuiAreaRaster[x + 1][y] == 0) continue;
                    if (perilData.wuiAreaRaster[x - 1][y] == 0) continue;
                    if (perilData.wuiAreaRaster[x][y + 1] == 0) continue;
                    if (perilData.wuiAreaRaster[x][y - 1] == 0) continue;
                    wuiAreaAdjusted[x][y] = 2;
                }
            }
            return wuiAreaAdjusted;
        }
        private (int xn, int yn) getNeighborDims(int x, int y, int dir)
        {
            switch (dir)
            {
                case 0: return (x - 1, y);
                case 1: return (x - 1, y + 1);
                case 2: return (x, y + 1);
                case 3: return (x + 1, y + 1);
                case 4: return (x + 1, y);
                case 5: return (x + 1, y - 1);
                case 6: return (x, y - 1);
                case 7: return (x - 1, y - 1);
                default: throw new IndexOutOfRangeException("Direction out of 0-7 range");
            }

        }
    }
}