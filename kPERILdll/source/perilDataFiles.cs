namespace kperil
{
    /// <summary>
    /// perilData's file-reading imports: each reads its rasters with <see cref="perilInputOutput"/> (ASCII grids, or
    /// GeoTIFFs through GDAL) and hands the arrays to the matching ...ByVariable import of kPERILcore, which holds
    /// the algorithm and has no file I/O. Extension methods, so callers keep writing
    /// <c>peril.perilData.importFireRastersByFileName(...)</c>.
    /// </summary>
    public static class perilDataFiles
    {
        /// <summary>Also takes the cell size, nodata value and lower-left corner from the magnitude raster.</summary>
        public static void importFireRastersByFileName(this perilData data, string rateOfSpreadMagnitudeFile, string rateOfSpreadDirectionFile)
        {
            data.importFireRastersByVariable(perilInputOutput.ReadRaster(rateOfSpreadMagnitudeFile),
                perilInputOutput.ReadRaster(rateOfSpreadDirectionFile));

            var metadata = perilInputOutput.GetRasterMetadata(rateOfSpreadMagnitudeFile);
            data.cellSize = metadata.CellSize;
            data.noDataValue = metadata.NoDataValue;
            data.xllCorner = metadata.XllCorner;
            data.yllCorner = metadata.YllCorner;
        }

        public static void importWeatherRastersByFileName(this perilData data, string windMagnitudeFile, string windDirectionFile)
        {
            data.importWeatherRastersByVariable(perilInputOutput.ReadRaster(windMagnitudeFile),
                perilInputOutput.ReadRaster(windDirectionFile));
        }

        public static void importWuiRastersByFileName(this perilData data, string wuiAreaFile)
        {
            data.importWuiRastersByVariable(perilInputOutput.ReadRaster(wuiAreaFile));
        }

        /// <summary>Slope and aspect are derived from the elevation.</summary>
        public static void importTopographyRastersByFileName(this perilData data, string elevationFile)
        {
            data.importTopographyRastersByVariable(perilInputOutput.ReadRaster(elevationFile));
        }

        public static void importTopographyRastersByFileName(this perilData data, string elevationFile, string slopeFile, string aspectFile)
        {
            data.importTopographyRastersByVariable(perilInputOutput.ReadRaster(elevationFile),
                perilInputOutput.ReadRaster(slopeFile), perilInputOutput.ReadRaster(aspectFile));
        }
    }
}
