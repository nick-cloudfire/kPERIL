namespace kperil
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

        public void importFireRastersByVariable(float[,] rateOfSpreadMagnitude, float[,] rateOfSpreadDirection)
        {
            this.rateOfSpreadMagnitudeRaster = rateOfSpreadMagnitude;
            this.rateOfSpreadDirectionRaster = rateOfSpreadDirection;

            totalX = this.rateOfSpreadMagnitudeRaster.GetLength(0);
            totalY = this.rateOfSpreadMagnitudeRaster.GetLength(1);

            checkRasterSize(this.rateOfSpreadDirectionRaster);
        }

        public void importWeatherRastersByVariable(float[,] windMagnitude, float[,] windDirection)
        {
            this.windMagnitudeRaster = windMagnitude;
            this.windDirectionRaster = windDirection;

            checkRasterSize(this.windMagnitudeRaster);
            checkRasterSize(this.windDirectionRaster);

            GetEffectiveWindWithSlope();
        }

        public void importTopographyRastersByVariable(float[,] elevation)
        {
            this.elevationRaster = elevation;

            checkRasterSize(this.elevationRaster);

            interpolateSlope();
        }

        public void importWuiRastersByVariable(float[,] wuiArea)
        {
            this.wuiAreaRaster = wuiArea;

            checkRasterSize(this.wuiAreaRaster);
        }

        /// <summary>
        /// Elevation with its slope (degrees) and aspect (degrees, compass bearing of the downhill direction, 0 =
        /// north, clockwise - as GDAL's gdaldem, ELMFIRE's asp raster and LANDFIRE write it).
        /// </summary>
        public void importTopographyRastersByVariable(float[,] elevation, float[,] slope, float[,] aspect)
        {
            this.elevationRaster = elevation;
            this.slopeRaster = slope;
            this.aspectRaster = aspect;

            checkRasterSize(this.elevationRaster);
            checkRasterSize(this.slopeRaster);
            checkRasterSize(this.aspectRaster);
        }

        /// <summary>The same as <see cref="importTopographyRastersByVariable(float[,], float[,], float[,])"/>; the
        /// name it had before the file-reading overloads moved to kPERILdll, kept for existing callers.</summary>
        public void importTopographyRastersByFileName(float[,] elevation, float[,] slope, float[,] aspect)
        {
            importTopographyRastersByVariable(elevation, slope, aspect);
        }

        public void rasteriseWindFromScalars(float windMagnitude, float windDirection)
        {
            this.windMagnitudeRaster = new float[this.totalX,this.totalY];
            this.windDirectionRaster = new float[this.totalX,this.totalY];

            for (int i = 0; i < totalX; i++)
            {
                for (int j = 0; j < totalY; j++)
                {
                    this.windMagnitudeRaster[i,j] = windMagnitude;
                    this.windDirectionRaster[i,j] = windDirection;
                }
            }
            GetEffectiveWindWithSlope();
        }
        private void checkRasterSize(float[,] raster)
        {
            if (raster.GetLength(0) != this.totalX || raster.GetLength(1) != this.totalY) throw new Exception($"Error: raster size mismatch");
        }

        //todo: verify all raster values have been imported.
        private void interpolateSlope()
        {
            if (elevationRaster == null)
            {
                throw new InvalidOperationException("Elevation raster must be loaded before calling interpolateSlope().");
            }

            slopeRaster = new float[this.totalX,this.totalY];
            aspectRaster = new float[this.totalX,this.totalY];

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

                    float dzdx = ((z3 + 2f * z6 + z9) - (z1 + 2f * z4 + z7)) / (8f * cellSize);
                    float dzdy = ((z7 + 2f * z8 + z9) - (z1 + 2f * z2 + z3)) / (8f * cellSize);

                    double gradient = Math.Sqrt(dzdx * dzdx + dzdy * dzdy);
                    slopeRaster[x,y] = (float)(Math.Atan(gradient) * radToDeg);

                    if (Math.Abs(dzdx) < 1e-6f && Math.Abs(dzdy) < 1e-6f)
                    {
                        aspectRaster[x,y] = 0f; // flat terrain
                        continue;
                    }

                    //The downhill direction as a compass bearing (0 = north, clockwise), the convention of a
                    //supplied aspect raster (gdaldem, ELMFIRE's asp, LANDFIRE) and of the wind direction.
                    //Atan2(dzdy, -dzdx) - x being the row from the top, so dzdy is the rise southward and
                    //dzdx the rise eastward - is the downhill direction's angle counter-clockwise from east;
                    //it used to be stored as it was, so a slope falling south read 270 instead of 180.
                    double aspectDegrees = 90.0 - Math.Atan2(dzdy, -dzdx) * radToDeg;
                    if (aspectDegrees < 0)
                    {
                        aspectDegrees += 360.0;
                    }
                    if (aspectDegrees >= 360.0)
                    {
                        aspectDegrees -= 360.0;
                    }

                    aspectRaster[x,y] = (float)aspectDegrees;
                }
            }
        }

        /// <summary>
        /// The effective midflame wind of each cell: the wind and 0.06 x the slope (degrees) as vectors in the
        /// direction each pushes the fire, added.
        /// </summary>
        /// <remarks>
        /// Both vectors point where they push: the wind towards its 'from' direction + 180, the slope upslope,
        /// aspect (the downhill bearing) + 180. The wind used to go in towards its 'from' direction while the
        /// slope pointed upslope, so an upslope wind and the slope subtracted (4.23 and 1.2 mi/h on a 20 degree
        /// slope gave 3.03 where 5.43 is meant) and a downslope wind added.
        /// </remarks>
        private void GetEffectiveWindWithSlope()
        {
            float[,] effectiveMidflameWindspeed = new float [this.totalX,this.totalY];

            for (int i = 0; i < totalX; i++)
            {
                for (int j = 0; j < totalY; j++)
                {
                    double effectiveSlopeWind = 0.06f * slopeRaster[i,j];

                    // Where the wind pushes the fire: away from where it blows from.
                    double radWindTowards = (windDirectionRaster[i,j] + 180) * Math.PI / 180.0;

                    // Convert polar coordinates to Cartesian (x, y)
                    double x2 = windMagnitudeRaster[i,j] * Math.Cos(radWindTowards);
                    double y2 = windMagnitudeRaster[i,j] * Math.Sin(radWindTowards);

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


