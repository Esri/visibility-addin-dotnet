/******************************************************************************* 
* Copyright 2016 Esri 
*  
*  Licensed under the Apache License, Version 2.0 (the "License"); 
*  you may not use this file except in compliance with the License. 
*  You may obtain a copy of the License at 
*  
*  http://www.apache.org/licenses/LICENSE-2.0 
*   
*   Unless required by applicable law or agreed to in writing, software 
*   distributed under the License is distributed on an "AS IS" BASIS, 
*   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
*   See the License for the specific language governing permissions and 
*   limitations under the License. 
*******************************************************************************/

// System
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ArcGIS
using ArcGIS.Core.Hosting;
using ArcGIS.Core.Geometry;

// Solution
using ProAppVisibilityModule.Models;
using ProAppVisibilityModule.ViewModels;

namespace ProAppVisibilityModule.Tests
{
    [TestClass]
    public class ProAppVisibilityModuleTests
    {

        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        [TestCategory("ArcGISPro")]
        public static void MyClassInitialize(TestContext testContext)
        {
            // TRICKY: Must be run as x64 processor (IntPtr.Size only obvious way to check)
            // Check here, otherwise you will get an error on Host.Initialize
            Assert.IsTrue(IntPtr.Size == 8,
                "The ArcGIS Pro tests must be run as x64 Architecture");
            // If the call above fails: 
            // In Studio: Test | Test Settings | Default Architecture | set to x64
            // VsTest: vstest.console.exe {TestDLL}.dll /InIsolation /platform:x64 
            // Note: MsTest does not have a platform option, so must use VsTest

            Host.Initialize();
        }

        [TestMethod, Description("Tests creating ProGraphic object")]
        [TestCategory("ArcGISPro")]
        public void ProCreateNullGraphicTest()
        {
            var guid = Guid.NewGuid().ToString();
            Geometry geom = null;
            var proGraphic = new ProGraphic(null, guid, geom);
            Assert.IsNotNull(proGraphic);
            Assert.IsTrue(proGraphic.IsTemp == false);
            Assert.IsTrue(proGraphic.GUID == guid);

        }

        [TestMethod, Description("Tests creating AddInPoint")]
        [TestCategory("ArcGISPro")]
        public void ProCreateAddInPointTest()
        {
            MapPoint point = MapPointBuilder.CreateMapPoint(1300757, 554219);
            Assert.IsNotNull(point);

            AddInPoint addinPoint = new AddInPoint();
            addinPoint.Point = point;

            string output = addinPoint.Text;
            Assert.IsFalse(output.Equals("NA"));
        }

        // NOTE: The Pro View Models could not be tested because they have dependencies
        // outside of ArcGIS.Core and can not be created
        // This is a known issue:
        // See: https://geonet.esri.com/groups/arcgis-pro-sdk/blog/2016/04/28/new-arcgis-pro-sdk-learning-resources-available
        // Question: "Can the Pro SDK be used to run console apps outside of Pro?"
        // Only objects from ArcGIS.Core can be created outside of Pro
        //[TestMethod, Description("Tests creating LLOSViewModel object")]
        //[TestCategory("ArcGISPro")]
        public void ProCreateLLOSViewModelTest()
        {
            var llosViewModel = new ProLLOSViewModel();

            Assert.IsNotNull(llosViewModel);
            Assert.IsNotNull(llosViewModel.TargetAddInPoints);
        }

        [TestMethod, Description("Tests creating RangeFans")]
        [TestCategory("ArcGISPro")]
        public void TestGeometryHelperConstructRangeFan()
        {
            SpatialReference sr = SpatialReferences.WGS84;

            MapPoint centerPoint1 = MapPointBuilder.CreateMapPoint(10.0, 20.0, sr);
            MapPoint centerPoint2 = MapPointBuilder.CreateMapPoint(-10.0, 20.0, sr);
            MapPoint centerPoint3 = MapPointBuilder.CreateMapPoint(10.0, -20.0, sr);
            MapPoint centerPoint4 = MapPointBuilder.CreateMapPoint(-10.0, -20.0, sr);
            MapPoint centerPoint5 = MapPointBuilder.CreateMapPoint(0.0, 0.0, sr);

            MapPoint[] mapPoints = { centerPoint1, centerPoint2, centerPoint3, centerPoint4, centerPoint5 };

            foreach (MapPoint centerPoint in mapPoints)
            {
                ///////////////////////////////////////////////
                // Basic Test
                double minDistanceInMapUnits = 1.0;
                double maxDistanceInMapUnits = 2.0;
                double horizontalStartAngleInDegrees = 45.0;
                double horizontalEndAngleInDegrees = 90.0;
                double stepAngle = 5.0;
                Geometry polygon = Helpers.GeometryHelper.ConstructRangeFan(centerPoint,
                    minDistanceInMapUnits, maxDistanceInMapUnits, horizontalStartAngleInDegrees,
                    horizontalEndAngleInDegrees, sr, stepAngle);
                Assert.IsNotNull(polygon);
                Assert.IsTrue(polygon.PointCount > 20);
                ///////////////////////////////////////////////

                ///////////////////////////////////////////////
                // range fan, crossing 360
                horizontalStartAngleInDegrees = 315.0;
                horizontalEndAngleInDegrees = 45.0;
                minDistanceInMapUnits = 1.0;
                maxDistanceInMapUnits = 2.0;
                stepAngle = 5.0;
                polygon = Helpers.GeometryHelper.ConstructRangeFan(centerPoint,
                    minDistanceInMapUnits, maxDistanceInMapUnits, horizontalStartAngleInDegrees,
                    horizontalEndAngleInDegrees, sr, stepAngle);
                Assert.IsNotNull(polygon);
                Assert.IsTrue(polygon.PointCount > 38);
                ///////////////////////////////////////////////

                ///////////////////////////////////////////////
                // full circle/360 degrees, 2 circles
                horizontalStartAngleInDegrees = 0.0;
                horizontalEndAngleInDegrees = 360.0;
                minDistanceInMapUnits = 1.0;
                maxDistanceInMapUnits = 2.0;
                polygon = Helpers.GeometryHelper.ConstructRangeFan(centerPoint,
                    minDistanceInMapUnits, maxDistanceInMapUnits, horizontalStartAngleInDegrees,
                    horizontalEndAngleInDegrees, sr, stepAngle);
                Assert.IsNotNull(polygon);
                Assert.IsTrue(GeometryEngine.Instance.Length(polygon) >= minDistanceInMapUnits * 4 * Math.PI);
                ///////////////////////////////////////////////

                ///////////////////////////////////////////////
                // full circle/360 degrees, 1 circle
                horizontalStartAngleInDegrees = 0.0;
                horizontalEndAngleInDegrees = 360.0;
                minDistanceInMapUnits = 0.0;
                maxDistanceInMapUnits = 2.0;
                polygon = Helpers.GeometryHelper.ConstructRangeFan(centerPoint,
                    minDistanceInMapUnits, maxDistanceInMapUnits, horizontalStartAngleInDegrees,
                    horizontalEndAngleInDegrees, sr, stepAngle);
                Assert.IsNotNull(polygon);
                Assert.IsTrue(GeometryEngine.Instance.Length(polygon) >= maxDistanceInMapUnits * 2 * Math.PI);
                ///////////////////////////////////////////////

                ///////////////////////////////////////////////
                // full circle/360 degrees, 1 circle - start/stop angle != 0,360
                horizontalStartAngleInDegrees = 10.0;
                horizontalEndAngleInDegrees = 10.0;
                minDistanceInMapUnits = 0.0;
                maxDistanceInMapUnits = 2.0;
                polygon = Helpers.GeometryHelper.ConstructRangeFan(centerPoint,
                    minDistanceInMapUnits, maxDistanceInMapUnits, horizontalStartAngleInDegrees,
                    horizontalEndAngleInDegrees, sr, stepAngle);
                Assert.IsNotNull(polygon);
                Assert.IsTrue(GeometryEngine.Length(polygon) >= maxDistanceInMapUnits * 2 * Math.PI);
                ///////////////////////////////////////////////

                ///////////////////////////////////////////////
                // bad input, negative distance
                horizontalStartAngleInDegrees = 120.0;
                horizontalEndAngleInDegrees = 180.0;
                minDistanceInMapUnits = 0.0;
                maxDistanceInMapUnits = -2.0;
                polygon = Helpers.GeometryHelper.ConstructRangeFan(centerPoint,
                    minDistanceInMapUnits, maxDistanceInMapUnits, horizontalStartAngleInDegrees,
                    horizontalEndAngleInDegrees, sr, stepAngle);
                Assert.IsNull(polygon);
                ///////////////////////////////////////////////

                ///////////////////////////////////////////////
                // null center point
                polygon = Helpers.GeometryHelper.ConstructRangeFan(null,
                    minDistanceInMapUnits, maxDistanceInMapUnits, horizontalStartAngleInDegrees,
                    horizontalEndAngleInDegrees, sr);
                Assert.IsNull(polygon);
                ///////////////////////////////////////////////

                ///////////////////////////////////////////////
                // null SR
                polygon = Helpers.GeometryHelper.ConstructRangeFan(centerPoint,
                    minDistanceInMapUnits, maxDistanceInMapUnits, horizontalStartAngleInDegrees,
                    horizontalEndAngleInDegrees, null);
                Assert.IsNull(polygon);
                ///////////////////////////////////////////////
            }

        }

    }
}
