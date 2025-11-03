// See https://aka.ms/new-console-template for more information
using kperil;
using System.IO;
using OSGeo.GDAL;
using OSGeo.OSR;
using kPERIL;
using System.Security.Cryptography;


namespace kPERIL {

    public class program {
        public static void Main(string[] args)
        {
            GdalConfiguration.ConfigureGdal();
            Gdal.AllRegister();

            kPERIL kPERIL = new kPERIL();

            float aset = 8;

            kPERIL.perilData.importFireRastersByFileName(@"D:\nPERIL\kPERIL\rateOfSpreadMagnitude.asc", @"D:\nPERIL\kPERIL\rateOfSpreadDirection.asc");
            kPERIL.perilData.importWuiRastersByFileName(@"D:\nPERIL\kPERIL\wuiArea.asc");
            kPERIL.perilData.importTopographyRastersByFileName(@"D:\nPERIL\kPERIL\elevation.asc");
            kPERIL.perilData.importWeatherRastersByFileName(@"D:\nPERIL\kPERIL\windMagnitude.asc", @"D:\nPERIL\kPERIL\windDirection.asc");
            
            var brokenDownROS = kPERIL.breakdownRateOfSpread();
            var travelTime = kPERIL.getTravelTimeFromBrokenDownRos(brokenDownROS);
            var result = kPERIL.getTriggerBoundary(travelTime, aset);
            
            perilInputOutput.WriteRasterByCopy(result, @"D:\nPERIL\kPERIL/elevation.asc", @"D:\nPERIL\kPERIL/output.asc");
        }
    }
}