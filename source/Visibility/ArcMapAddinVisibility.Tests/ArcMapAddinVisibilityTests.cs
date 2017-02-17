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

// Esri
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;

// System
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Solution
using ArcMapAddinVisibility;
using ArcMapAddinVisibility.Models;
using ArcMapAddinVisibility.ViewModels;

namespace ArcMapAddinVisibility.Tests
{
    [TestClass]
    public class ArcMapAddinVisibilityTests
    {
        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        [TestCategory("ArcMapAddin")]
        public static void MyClassInitialize(TestContext testContext)
        {
            // TRICKY: Must be run as x86 processor (IntPtr.Size only obvious way to check)
            // Check here, otherwise you just get a cryptic error on license call below
            Assert.IsTrue(IntPtr.Size == 4, 
                "The ArcMap tests must be run as x86 Architecture");
            // If the call above fails: 
            // In Studio: Test | Test Settings | Default Architecture | set to x86 
            // MSTest: (This defaults to x86) 

            if (ESRI.ArcGIS.RuntimeManager.ActiveRuntime == null)
                ESRI.ArcGIS.RuntimeManager.BindLicense(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            Assert.IsTrue(ESRI.ArcGIS.RuntimeManager.ActiveRuntime != null, 
                "No ArcGIS Desktop Runtime available");

            IAoInitialize aoInitialize = new AoInitializeClass();
            aoInitialize.Initialize(esriLicenseProductCode.esriLicenseProductCodeStandard); 
        }

        [TestMethod, Description("Tests converting Point to string")]
        [TestCategory("ArcMapAddin")]
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

        [TestMethod, Description("Tests creating AMGraphic object")]
        [TestCategory("ArcMapAddin")]
        public void CreateAMGraphicTest()
        {
            var amGraphic = new AMGraphic("tempGraphic", null);
            Assert.IsNotNull(amGraphic);
            Assert.IsTrue(amGraphic.UniqueId.Equals("tempGraphic"));
            Assert.IsTrue(amGraphic.IsTemp == false);
        }

        #region Test ViewModels

        #region LLOSViewModel
        [TestMethod, Description("Tests creating LLOSViewModel object")]
        [TestCategory("ArcMapAddin")]
        public void CreateLLOSViewModelTest()
        {
            var llosViewModel = new LLOSViewModel();
            Assert.IsNotNull(llosViewModel);
            Assert.IsNotNull(llosViewModel.TargetAddInPoints);
        }
        #endregion

        #region LOSBaseViewModel
        [TestMethod, Description("Tests creating LOSBaseViewModel object")]
        [TestCategory("ArcMapAddin")]
        public void CreateLOSBaseViewModelTest()
        {
            var losBaseViewModel = new LOSBaseViewModel();
            Assert.IsNotNull(losBaseViewModel);         
        }
        #endregion

        #region RLOSViewModel
        [TestMethod, Description("Tests creating RLOSViewModel object")]
        [TestCategory("ArcMapAddin")]
        public void CreateRLOSViewModelTest()
        {
            var rlosViewModel = new RLOSViewModel();
            Assert.IsNotNull(rlosViewModel);
        }

        [TestMethod, Description("Tests creating a FeatureWorkspace")]
        [TestCategory("ArcMapAddin")]
        public void CreateFeatureWorkspaceTest()
        {
            var workspace = CreateFeatureWorkspace();
            Assert.IsNotNull(workspace);
        }

        [TestMethod, Description("Tests creating an observers feature class")]
        [TestCategory("ArcMapAddin")]
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

        [TestMethod, Description("Tests start/stop edit operations")]
        [TestCategory("ArcMapAddin")]
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
