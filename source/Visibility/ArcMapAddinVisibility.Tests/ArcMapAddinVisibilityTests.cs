using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ArcMapAddinVisibility;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;
using ArcMapAddinVisibility.Models;
using ArcMapAddinVisibility.ViewModels;
using ESRI.ArcGIS.Geodatabase;


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
        public void PointToStringConverterTest()
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

        [TestMethod]
        public void CreateAMGraphicTest()
        {
            var amGraphic = new AMGraphic("tempGraphic", null);
            Assert.IsNotNull(amGraphic);
            Assert.IsTrue(amGraphic.UniqueId.Equals("tempGraphic"));
            Assert.IsTrue(amGraphic.IsTemp == false);
        }

        #region Test ViewModels

        #region LLOSViewModel
        [TestMethod]
        public void CreateLLOSViewModelTest()
        {
            var llosViewModel = new LLOSViewModel();
            Assert.IsNotNull(llosViewModel);
            Assert.IsNotNull(llosViewModel.TargetAddInPoints);
        }
        #endregion

        #region LOSBaseViewModel
        [TestMethod]
        public void CreateLOSBaseViewModelTest()
        {
            var losBaseViewModel = new LOSBaseViewModel();
            Assert.IsNotNull(losBaseViewModel);
           
        }
        #endregion

        #region RLOSViewModel
        [TestMethod]
        public void CreateRLOSViewModelTest()
        {
            var rlosViewModel = new RLOSViewModel();
            Assert.IsNotNull(rlosViewModel);
        }

        [TestMethod]
        public void CreateFeatureWorkspaceTest()
        {
            var workspace = CreateFeatureWorkspace();
            Assert.IsNotNull(workspace);
        }

        [TestMethod]
        public void CreateObserversFeatureClassTest()
        {
            var workspace = CreateFeatureWorkspace();
            Assert.IsNotNull(workspace);

            var featureClass = CreateObserversFeatureClass(workspace);
            Assert.IsNotNull(featureClass);

            var index = featureClass.FindField("OBJECTID");
            Assert.IsTrue(index >= 0);

            index = featureClass.FindField("OFFSETA");
            Assert.IsTrue(index >= 0);

            index = featureClass.FindField("OFFSETB");
            Assert.IsTrue(index >= 0);

            index = featureClass.FindField("AZIMUTH1");
            Assert.IsTrue(index >= 0);

            index = featureClass.FindField("AZIMUTH2");
            Assert.IsTrue(index >= 0);

            index = featureClass.FindField("RADIUS1");
            Assert.IsTrue(index >= 0);

            index = featureClass.FindField("RADIUS2");
            Assert.IsTrue(index >= 0);

            index = featureClass.FindField("VERT1");
            Assert.IsTrue(index >= 0);

            index = featureClass.FindField("VERT2");
            Assert.IsTrue(index >= 0);
        }

        [TestMethod]
        public void StartStopEditTest()
        {
            IWorkspace workspace = CreateFeatureWorkspace() as IWorkspace;
            Assert.IsNotNull(workspace);

            bool success = RLOSViewModel.StartEditOperation(workspace);
            Assert.IsTrue(success);

            success = RLOSViewModel.StopEditOperation(workspace);
            Assert.IsTrue(success);

            success = RLOSViewModel.StartEditOperation(null);
            Assert.IsFalse(success);

        }

        #endregion

        #endregion 

        #region Private
        private IFeatureWorkspace CreateFeatureWorkspace()
        {
            IFeatureWorkspace workspace = null;

            // Create feature workspace
            workspace = RLOSViewModel.CreateFeatureWorkspace("tempWorkspace");

            return workspace;
        }

        private IFeatureClass CreateObserversFeatureClass(IFeatureWorkspace workspace)
        {
            // Create Srs 
            ISpatialReferenceFactory spatialrefFactory = new SpatialReferenceEnvironmentClass(); 
            ISpatialReference sr = spatialrefFactory.CreateProjectedCoordinateSystem( 
                (int)(esriSRProjCSType.esriSRProjCS_World_Mercator));

            IFeatureClass pointFc = RLOSViewModel.CreateObserversFeatureClass(workspace, sr, "tempFC");
            return pointFc;
        }
                
        #endregion
    }
}
