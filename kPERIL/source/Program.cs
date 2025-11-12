// See https://aka.ms/new-console-template for more information
using OSGeo.GDAL;

namespace kPERIL 
{
    public class Program {
        public static void Main(string[] args)
        {
            GdalConfiguration.ConfigureGdal();
            Gdal.AllRegister();

            kPERILcore kPERIL = new kPERILcore();

            float aset = 8;

            string mainFolder = @"D:\nPERIL\kPERIL\kPERIL\files\";

            kPERIL.perilData.importFireRastersByFileName(mainFolder + "rateOfSpreadMagnitude.asc", mainFolder + "rateOfSpreadDirection.asc");
            kPERIL.perilData.importWuiRastersByFileName(mainFolder + "wuiArea.asc");
            kPERIL.perilData.importTopographyRastersByFileName(mainFolder + "elevation.asc");
            kPERIL.perilData.importWeatherRastersByFileName(mainFolder + "windMagnitude.asc", mainFolder + "windDirection.asc");

            float[,] result = kPERIL.Run(aset);

            perilInputOutput.WriteRasterByCopy(result, mainFolder + "elevation.asc", mainFolder + "output.asc");
        }
    }
}