using System;

namespace kPERIL
{
    public class perilData
    {
        public float[,] rateOfSpreadMagnitudeRaster;
        public float[,] windMagnitudeRaster;
        public float[,] rateOfSpreadDirectionRaster;
        public float[,] windDirectionRaster;
        public float[,] elevationRaster;
        public float[,] slopeRaster;
        public float[,] aspectRaster;
        public float[,] effectiveMidflameWindspeed;
        public float[,] wuiAreaRaster;
        public float cellSize;
        public float noDataValue;
        public float xllCorner;
        public float yllCorner;
        public int totalX;
        public int totalY;

        public void importFireRastersByFileName(string rateOfSpreadMagnitudeFile, string rateOfSpreadDirectionFile)
        {
            rateOfSpreadMagnitudeRaster = perilInputOutput.ReadRaster(rateOfSpreadMagnitudeFile);
            rateOfSpreadDirectionRaster = perilInputOutput.ReadRaster(rateOfSpreadDirectionFile);

            totalX = rateOfSpreadMagnitudeRaster.GetLength(0);
            totalY = rateOfSpreadMagnitudeRaster.GetLength(1);

            checkRasterSize(rateOfSpreadDirectionRaster);

            var metadata = perilInputOutput.GetRasterMetadata(rateOfSpreadMagnitudeFile);
            cellSize = metadata.CellSize;
            noDataValue = metadata.NoDataValue;
            xllCorner = metadata.XllCorner;
            yllCorner = metadata.YllCorner;
        }

        public void importWeatherRastersByFileName(string windMagnitudeFile, string windDirectionFile)
        {
            windMagnitudeRaster = perilInputOutput.ReadRaster(windMagnitudeFile);
            windDirectionRaster = perilInputOutput.ReadRaster(windDirectionFile);

            checkRasterSize(windMagnitudeRaster);
            checkRasterSize(windDirectionRaster);

            GetEffectiveWindWithSlope();
        }

        public void importWuiRastersByFileName(string wuiAreaFile)
        {
            wuiAreaRaster = perilInputOutput.ReadRaster(wuiAreaFile);

            checkRasterSize(wuiAreaRaster);
        }

        public void importTopographyRastersByFileName(string elevationFile)
        {
            elevationRaster = perilInputOutput.ReadRaster(elevationFile);

            checkRasterSize(elevationRaster);
            interpolateSlope();
        }

        public void importTopographyRastersByFileName(string elevationFile, string slopeFile, string aspectFile)
        {
            elevationRaster = perilInputOutput.ReadRaster(elevationFile);
            slopeRaster = perilInputOutput.ReadRaster(slopeFile);
            aspectRaster = perilInputOutput.ReadRaster(aspectFile);

            checkRasterSize(elevationRaster);
            checkRasterSize(slopeRaster);
            checkRasterSize(aspectRaster);
        }

        public void importFireRastersByVariable(float[,] rateOfSpreadMagnitude, float[,] rateOfSpreadDirection)
        {
            rateOfSpreadMagnitudeRaster = rateOfSpreadMagnitude;
            rateOfSpreadDirectionRaster = rateOfSpreadDirection;

            totalX = rateOfSpreadMagnitudeRaster.GetLength(0);
            totalY = rateOfSpreadMagnitudeRaster.GetLength(1);

            checkRasterSize(rateOfSpreadDirectionRaster);
        }

        public void importWeatherRastersByVariable(float[,] windMagnitude, float[,] windDirection)
        {
            windMagnitudeRaster = windMagnitude;
            windDirectionRaster = windDirection;

            checkRasterSize(windMagnitudeRaster);
            checkRasterSize(windDirectionRaster);

            GetEffectiveWindWithSlope();
        }

        public void importTopographyRastersByVariable(float[,] elevation)
        {
            elevationRaster = elevation;

            checkRasterSize(elevationRaster);

            interpolateSlope();
        }

        public void importWuiRastersByVariable(float[,] wuiArea)
        {
            wuiAreaRaster = wuiArea;

            checkRasterSize(wuiAreaRaster);
        }

        public void importTopographyRastersByFileName(float[,] elevation, float[,] slope, float[,] aspect)
        {
            elevationRaster = elevation;
            slopeRaster = slope;
            aspectRaster = aspect;

            checkRasterSize(elevationRaster);
            checkRasterSize(slopeRaster);
            checkRasterSize(aspectRaster);
        }
        public void rasteriseWindFromScalars(float windMagnitude, float windDirection)
        {
            windMagnitudeRaster = new float[totalX, totalY];
            windDirectionRaster = new float[totalX, totalY];

            for (int i = 0; i < totalX; i++)
            {
                for (int j = 0; j < totalY; j++)
                {
                    windMagnitudeRaster[i,j] = windMagnitude;
                    windDirectionRaster[i,j] = windDirection;
                }
            }
            GetEffectiveWindWithSlope();
        }
        private void checkRasterSize(float[,] raster)
        {
            if (raster.GetLength(0) != totalX || raster.GetLength(1) != totalY) throw new Exception($"Error: raster size mismatch");
        }

        //todo: verify all raster values have been imported.
        private void interpolateSlope()
        {
            if (elevationRaster == null)
            {
                throw new InvalidOperationException("Elevation raster must be loaded before calling interpolateSlope().");
            }

            slopeRaster = new float[totalX, totalY];
            aspectRaster = new float[totalX, totalY];

            const float toDegrees = 180f / (float)Math.PI;

            const float radToDeg = 180f / (float)Math.PI;

            float SampleElevation(int x, int y)
            {
                x = Math.Clamp(x, 0, totalX - 1);
                y = Math.Clamp(y, 0, totalY - 1);
                return elevationRaster[x,y];
            }

            for (int x = 0; x < totalX; x++)
            {
                for (int y = 0; y < totalY; y++)
                {
                    float z1 = SampleElevation(x - 1, y - 1);
                    float z2 = SampleElevation(x - 1, y);
                    float z3 = SampleElevation(x - 1, y + 1);
                    float z4 = SampleElevation(x, y - 1);
                    float z5 = SampleElevation(x, y);
                    float z6 = SampleElevation(x, y + 1);
                    float z7 = SampleElevation(x + 1, y - 1);
                    float z8 = SampleElevation(x + 1, y);
                    float z9 = SampleElevation(x + 1, y + 1);

                    float dzdx = (z3 + 2f * z6 + z9 - (z1 + 2f * z4 + z7)) / (8f * cellSize);
                    float dzdy = (z7 + 2f * z8 + z9 - (z1 + 2f * z2 + z3)) / (8f * cellSize);

                    double gradient = Math.Sqrt(dzdx * dzdx + dzdy * dzdy);
                    slopeRaster[x,y] = (float)(Math.Atan(gradient) * radToDeg);

                    if (Math.Abs(dzdx) < 1e-6f && Math.Abs(dzdy) < 1e-6f)
                    {
                        aspectRaster[x,y] = 0f; // flat terrain
                        continue;
                    }

                    double aspectRadians = Math.Atan2(dzdy, -dzdx);
                    if (aspectRadians < 0)
                    {
                        aspectRadians += 2 * Math.PI;
                    }

                    aspectRaster[x,y] = (float)(aspectRadians * radToDeg);
                }
            }
        }

        private void GetEffectiveWindWithSlope()
        {
            float[,] effectiveMidflameWindspeed = new float [totalX, totalY];

            for (int i = 0; i < totalX; i++)
            {
                for (int j = 0; j < totalY; j++)
                {
                    double effectiveSlopeWind = 0.06f * slopeRaster[i,j];

                    // Convert degrees to radians
                    double radWindDir = windDirectionRaster[i,j] * Math.PI / 180.0;

                    // Convert polar coordinates to Cartesian (x, y)
                    double x2 = windMagnitudeRaster[i,j] * Math.Cos(radWindDir);
                    double y2 = windMagnitudeRaster[i,j] * Math.Sin(radWindDir);

                    double radSlopeUpDir = (aspectRaster[i,j] + 180) * Math.PI / 180.0;
                    double x1 = effectiveSlopeWind * Math.Cos(radSlopeUpDir);
                    double y1 = effectiveSlopeWind * Math.Sin(radSlopeUpDir);

                    double xResult = x1 + x2;
                    double yResult = y1 + y2;

                    effectiveMidflameWindspeed[i,j] = (float)Math.Sqrt(xResult * xResult + yResult * yResult);
                    //double resultDirection = Math.Atan2(yResult, xResult) * 180.0 / Math.PI;
                }
            }
            this.effectiveMidflameWindspeed = effectiveMidflameWindspeed;
        }

        public float[][] initiateJaggedArray()
        {
            var jaggedArray = new float[totalX][];
            for (int x = 0; x < totalX; x++)
            {
                jaggedArray[x] = new float[totalY];
            }
            return jaggedArray;
        }
        // Verifies that all required input rasters are imported and correctly sized.
        public void VerifyAllRastersImported()
        {
            if (totalX <= 0 || totalY <= 0)
            {
                throw new InvalidOperationException("Raster dimensions are not initialized.");
            }

            var missingKinds = new System.Collections.Generic.List<string>();

            // Fire: ROS magnitude + direction
            if (rateOfSpreadMagnitudeRaster == null || rateOfSpreadDirectionRaster == null) missingKinds.Add("Fire");

            // Weather: wind magnitude + direction
            if (windMagnitudeRaster == null || windDirectionRaster == null) missingKinds.Add("Weather");

            // Elevation (base topo)
            if (elevationRaster == null) missingKinds.Add("Elevation");

            // Topography derivatives (slope/aspect) normally computed from elevation
            if (slopeRaster == null || aspectRaster == null) missingKinds.Add("Topography");

            // WUI layer
            if (wuiAreaRaster == null) missingKinds.Add("WUI");

            if (missingKinds.Count > 0)
            {
                throw new InvalidOperationException($"Missing raster inputs: {string.Join(", ", missingKinds)}");
            }
        }
    }
}


