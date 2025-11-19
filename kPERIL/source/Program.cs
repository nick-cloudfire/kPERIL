// See https://aka.ms/new-console-template for more information
using kperil;
using kPERILdll;
using OSGeo.GDAL;

namespace kPERIL {

    public class Program {
        public static void Main(string[] args)
        {
            GdalConfiguration.ConfigureGdal();
            Gdal.AllRegister();

            kPERIL kPERIL = new kPERIL();

            float rset = 8;

            string mainFolder = @"D:\nPERIL\kPERIL\kPERIL\files\";

            kPERIL.perilData.importFireRastersByFileName(mainFolder + "rateOfSpreadMagnitude.asc", mainFolder + "rateOfSpreadDirection.asc");
            kPERIL.perilData.importWuiRastersByFileName(mainFolder + "wuiArea.asc");
            kPERIL.perilData.importTopographyRastersByFileName(mainFolder + "elevation.asc");
            kPERIL.perilData.importWeatherRastersByFileName(mainFolder + "windMagnitude.asc", mainFolder + "windDirection.asc");
             
            var brokenDownROS = kPERIL.breakdownRateOfSpread();
            var travelTime = kPERIL.getTravelTimeFromBrokenDownRos(brokenDownROS);
            var result = kPERIL.getTriggerBoundary(travelTime, rset);

            perilInputOutput.WriteRasterByCopy(result, mainFolder + "elevation.asc", mainFolder + "output.asc");
        }
    }
}