// See https://aka.ms/new-console-template for more information
using kperil;
using System.IO;
using OSGeo.GDAL;
using OSGeo.OSR;
using kPERIL;

GdalConfiguration.ConfigureGdal();
Gdal.AllRegister();


string file = @"D:\nPERIL\kPERIL/fbfm13.tif";

float[][] raster = perilInputOutput.ReadRaster(file);
perilInputOutput.WriteRasterByCopy(raster, @"D:\nPERIL\kPERIL/fbfm13.asc", @"D:\nPERIL\kPERIL/fmfb13_new.asc");
