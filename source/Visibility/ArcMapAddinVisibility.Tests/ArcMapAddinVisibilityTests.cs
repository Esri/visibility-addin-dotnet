using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ArcMapAddinVisibility;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;


namespace ArcMapAddinVisibility.Tests
{
    [TestClass]
    public class ArcMapAddinVisibilityTests
    {
        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            bool blnBoundToRuntime = ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            Assert.IsTrue(blnBoundToRuntime, "Not bound to runtime");

            IAoInitialize aoInitialize = new AoInitializeClass();
            aoInitialize.Initialize(esriLicenseProductCode.esriLicenseProductCodeAdvanced); 
        }

        [TestMethod]
        public void PointToStringConverter()
        {
            var pointConverter = new IPointToStringConverter();
            Assert.IsNotNull(pointConverter);

            IPoint point = new PointClass();
            Assert.IsNotNull(point);

            point.PutCoords(1300757, 554219);
            Models.AddInPoint addinPoint = new Models.AddInPoint();
            addinPoint.Point = point;

            string output = pointConverter.Convert(addinPoint.Point as object, typeof(string), null, null) as string; 
            Assert.IsFalse(output.Equals("NA"));
        }
    }
}
