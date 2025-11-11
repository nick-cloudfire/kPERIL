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

            float aset = 8;

            kPERIL.perilData.importFireRastersByFileName(@"D:\nPERIL\kPERIL\kPERIL\files\rateOfSpreadMagnitude.asc", @"D:\nPERIL\kPERIL\kPERIL\files\rateOfSpreadDirection.asc");
            kPERIL.perilData.importWuiRastersByFileName(@"D:\nPERIL\kPERIL\kPERIL\files\wuiArea.asc");
            kPERIL.perilData.importTopographyRastersByFileName(@"D:\nPERIL\kPERIL\kPERIL\files\elevation.asc");
            kPERIL.perilData.importWeatherRastersByFileName(@"D:\nPERIL\kPERIL\kPERIL\files\windMagnitude.asc", @"D:\nPERIL\kPERIL\kPERIL\files\windDirection.asc");
             
            var brokenDownROS = kPERIL.breakdownRateOfSpread();
            var travelTime = kPERIL.getTravelTimeFromBrokenDownRos(brokenDownROS);
            var result = kPERIL.getTriggerBoundary(travelTime, aset);

            perilInputOutput.WriteRasterByCopy(result, @"D:\nPERIL\kPERIL\kPERIL\files\elevation.asc", @"D:\nPERIL\kPERIL\kPERIL\files\output.asc");
        }
    }
}