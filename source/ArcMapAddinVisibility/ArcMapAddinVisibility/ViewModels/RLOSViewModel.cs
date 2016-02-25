// Copyright 2016 Esri 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using ArcMapAddinVisibility.Helpers;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.GeoAnalyst;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcMapAddinVisibility.ViewModels
{
    public class RLOSViewModel : LOSBaseViewModel
    {
        #region Properties

        public double SurfaceOffset { get; set; }
        public double MinDistance { get; set; }
        public double MaxDistance { get; set; }
        public double LeftHorizontalFOV { get; set; }
        public double RightHorizontalFOV { get; set; }
        public double BottomVerticalFOV { get; set; }
        public double TopVerticalFOV { get; set; }

        #endregion

        #region Commands

        public RelayCommand SubmitCommand { get; set; }

        private void OnSubmitCommand(object obj)
        {
            // add wait cursor
            var savedCursor = System.Windows.Forms.Cursor.Current;
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            System.Windows.Forms.Application.DoEvents();

            CreateMapElement();

            // set back to initial cursor
            System.Windows.Forms.Cursor.Current = savedCursor;
        }

        #endregion

        public RLOSViewModel()
        {
            SurfaceOffset = 0.0;
            MinDistance = 0.0;
            MaxDistance = 1000;
            LeftHorizontalFOV = 0.0;
            RightHorizontalFOV = 360.0;
            BottomVerticalFOV = -90.0;
            TopVerticalFOV = 90.0;

            // commands
            SubmitCommand = new RelayCommand(OnSubmitCommand);
        }

        public override bool CanCreateElement
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(SelectedSurfaceName)
                    && ObserverPoints.Any());
            }
        }

        internal override void CreateMapElement()
        {
            if (!CanCreateElement || ArcMap.Document == null || ArcMap.Document.FocusMap == null || string.IsNullOrWhiteSpace(SelectedSurfaceName))
                return;

            //base.CreateMapElement();

            var surface = GetSurfaceFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName);

            if (surface == null)
                return;

            // Create feature workspace
            IFeatureWorkspace workspace = CreateFeatureWorkspace("tempWorkspace");

            StartEditOperation((IWorkspace)workspace);

            // Create feature class
            IFeatureClass pointFc = CreateFeatureClass(workspace, "tempfc");

            double finalObserverOffset = GetOffsetInZUnits(ArcMap.Document.FocusMap, ObserverOffset.Value, surface.ZFactor, OffsetUnitType);
            double finalSurfaceOffset = GetOffsetInZUnits(ArcMap.Document.FocusMap, SurfaceOffset, surface.ZFactor, OffsetUnitType);
            double finalMinDistance = GetOffsetInZUnits(ArcMap.Document.FocusMap, MinDistance, 1, OffsetUnitType);
            double finalMaxDistance = GetOffsetInZUnits(ArcMap.Document.FocusMap, MaxDistance, 1, OffsetUnitType);

            foreach (var observerPoint in ObserverPoints)
            {
                double z1 = surface.GetElevation(observerPoint) + finalObserverOffset;

                //create a new point feature
                IFeature ipFeature = pointFc.CreateFeature();

                // Observer Offset
                SetDatabaseFieldValue(ipFeature, "OFFSETA", finalObserverOffset);
                // Surface Offset
                SetDatabaseFieldValue(ipFeature, "OFFSETB", finalSurfaceOffset);
                // Horizontal FOV
                SetDatabaseFieldValue(ipFeature, "AZIMUTH1", LeftHorizontalFOV);
                SetDatabaseFieldValue(ipFeature, "AZIMUTH2", RightHorizontalFOV);
                // Distance
                SetDatabaseFieldValue(ipFeature, "RADIUS1", finalMinDistance);
                SetDatabaseFieldValue(ipFeature, "RADIUS2", finalMaxDistance);
                // Vertical FOV
                SetDatabaseFieldValue(ipFeature, "VERT1", BottomVerticalFOV);
                SetDatabaseFieldValue(ipFeature, "VERT1", TopVerticalFOV);

                //Create shape 
                IPoint point = new PointClass() { Z = z1, X = observerPoint.X, Y = observerPoint.Y, ZAware = true };
                ipFeature.Shape = point;
                ipFeature.Store();
            }

            IFeatureClassDescriptor fd = new FeatureClassDescriptorClass();
            fd.Create(pointFc, null, "OBJECTID"); 


            StopEditOperation((IWorkspace)workspace);

            var rasterLayer = GetLayerFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName);

            ISurfaceOp2 rasterSurfaceOp = new RasterSurfaceOpClass();
            if (rasterSurfaceOp == null)
                return;

            object Missing = Type.Missing;
            IGeoDataset gds = rasterSurfaceOp.Visibility(rasterLayer as IGeoDataset, pointFc as IGeoDataset, esriGeoAnalysisVisibilityEnum.esriGeoAnalysisVisibilityObservers, ref Missing, ref Missing);
            IRaster raster = gds as IRaster;

            ESRI.ArcGIS.Carto.IRasterLayer rLayer = new RasterLayerClass();
            rLayer.CreateFromRaster(raster);

            //Add it to a map if the layer is valid.
            if (rLayer != null)
            {
                // set the renderer
                IRasterRenderer rastRend = GenerateRasterRenderer(raster);
                rLayer.Renderer = rastRend;

                ESRI.ArcGIS.Carto.IMap map = ArcMap.Document.FocusMap;
                map.AddLayer((ILayer)rLayer);
            }

            //Reset(true);
        }

        internal override void Reset(bool toolReset)
        {
            base.Reset(toolReset);

            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return;

            // reset target points
            ObserverPoints.Clear();
        }

        public static void StartEditOperation(IWorkspace ipWorkspace)
        {
            IWorkspaceEdit ipWsEdit = ipWorkspace as IWorkspaceEdit;
            try
            {
                ipWsEdit.StartEditOperation();
            }
            catch (Exception ex)
            {
                ipWsEdit.AbortEditOperation();
                throw (ex);
            }
        }

        public static bool StopEditOperation(IWorkspace ipWorkspace)
        {
            bool blnWasSuccessful = false;
            IWorkspaceEdit ipWsEdit = ipWorkspace as IWorkspaceEdit;

            try
            {
                ipWsEdit.StopEditOperation();
                blnWasSuccessful = true;
            }
            catch (Exception ex)
            {
                ipWsEdit.AbortEditOperation();
            }

            return blnWasSuccessful;
        }

        /// <summary>
        /// Sets a database fields value
        /// 
        /// Throws an exception if the field is not found.
        /// </summary>
        /// <param name="ipRowBuffer">The tables row buffer</param>
        /// <param name="strFieldName">The fields name</param>
        /// <param name="oFieldValue">The value to set</param>
        public static void SetDatabaseFieldValue(IRowBuffer ipRowBuffer, string strFieldName, object oFieldValue)
        {
            int iFieldIndex = ipRowBuffer.Fields.FindField(strFieldName);
            if (iFieldIndex >= 0)
            {
                ipRowBuffer.set_Value(iFieldIndex, oFieldValue);
            }
            else
            {
                throw new Exception("Field index not found");
            }
        }

        /// <summary>
        /// Generates a raster renderer using the provided settings
        /// </summary>
        /// <returns>IRasterRenderer</returns>
        private static IRasterRenderer GenerateRasterRenderer(IRaster pRaster)
        {
             IRasterStretchColorRampRenderer pStretchRen = new RasterStretchColorRampRenderer(); 
             IRasterRenderer pRasRen = (IRasterRenderer)pStretchRen;

             pRasRen.Raster = pRaster;
             pRasRen.Update();

            IRgbColor pFromColor = new RgbColorClass(); 
             pFromColor.Red = 255; 
             pFromColor.Green = 0; 
             pFromColor.Blue = 0; 
 
             IRgbColor pToColor = new RgbColorClass(); 
             pToColor.Red = 0; 
             pToColor.Green = 255; 
             pToColor.Blue = 0; 

             IAlgorithmicColorRamp pRamp = new AlgorithmicColorRamp(); 
             pRamp.Size = 255; 
             pRamp.FromColor = pFromColor; 
             pRamp.ToColor = pToColor; 
             bool bOK; 
             pRamp.CreateRamp(out bOK); 
 
             pStretchRen.BandIndex = 0; 
             pStretchRen.ColorRamp = pRamp; 
 
             pRasRen.Update(); 
             
             pRaster = null; 
             pRasRen = null; 
             pRamp = null; 
             pToColor = null; 
             pFromColor = null;

             return (IRasterRenderer)pStretchRen;
        }

        public static IFeatureWorkspace CreateFeatureWorkspace(string workspaceNameString) 
         { 
             IWorkspaceFactory workspaceFactory = new InMemoryWorkspaceFactoryClass(); 
             // Create an InMemory geodatabase. 
             IWorkspaceName workspaceName = workspaceFactory.Create("", workspaceNameString, null, 0); 
 
             // Cast for IName. 
             IName name = (IName)workspaceName; 
 
             IWorkspace inmemWor = (IWorkspace)name.Open(); 
 
             IFeatureWorkspace featWork = (IFeatureWorkspace)inmemWor; 
 
             return featWork; 
         } 



        /// <summary> 
        /// Reference: http://forums.esri.com/Thread.asp?c=159&f=1707&t=294148 
        /// </summary> 
         /// <param name="featWorkspace"></param> 
        /// <param name="name"></param> 
         /// <returns></returns> 
         //public static IFeatureClass CreateFeatureClass(IFeatureWorkspace featWorkspace, string name, ISet<CSVField> fields) 
         public static IFeatureClass CreateFeatureClass(IFeatureWorkspace featWorkspace, string name) 
         { 
 
             IFieldsEdit pFldsEdt = new FieldsClass(); 
             IFieldEdit pFldEdt = new FieldClass(); 
 
             pFldEdt = new FieldClass(); 
             pFldEdt.Type_2 = esriFieldType.esriFieldTypeOID; 
             pFldEdt.Name_2 = "OBJECTID"; 
             pFldEdt.AliasName_2 = "OBJECTID"; 
             pFldsEdt.AddField(pFldEdt); 
 
             IGeometryDefEdit pGeoDef; 
             pGeoDef = new GeometryDefClass(); 
             pGeoDef.GeometryType_2 = esriGeometryType.esriGeometryPoint; 
             //pGeoDef.SpatialReference_2 = pSpaRef; 
             pGeoDef.SpatialReference_2 = ArcMap.Document.FocusMap.SpatialReference;
             pGeoDef.HasZ_2 = true;
 
             pFldEdt = new FieldClass(); 
             pFldEdt.Name_2 = "SHAPE"; 
             pFldEdt.AliasName_2 = "SHAPE"; 
             pFldEdt.Type_2 = esriFieldType.esriFieldTypeGeometry; 
             pFldEdt.GeometryDef_2 = pGeoDef; 
             pFldsEdt.AddField(pFldEdt);

             pFldEdt = new FieldClass();
             pFldEdt.Name_2 = "OFFSETA";
             pFldEdt.AliasName_2 = "OFFSETA";
             pFldEdt.Type_2 = esriFieldType.esriFieldTypeDouble;
             pFldsEdt.AddField(pFldEdt);

             pFldEdt = new FieldClass();
             pFldEdt.Name_2 = "OFFSETB";
             pFldEdt.AliasName_2 = "OFFSETB";
             pFldEdt.Type_2 = esriFieldType.esriFieldTypeDouble;
             pFldsEdt.AddField(pFldEdt);

             pFldEdt = new FieldClass();
             pFldEdt.Name_2 = "AZIMUTH1";
             pFldEdt.AliasName_2 = "AZIMUTH1";
             pFldEdt.Type_2 = esriFieldType.esriFieldTypeDouble;
             pFldsEdt.AddField(pFldEdt);

             pFldEdt = new FieldClass();
             pFldEdt.Name_2 = "AZIMUTH2";
             pFldEdt.AliasName_2 = "AZIMUTH2";
             pFldEdt.Type_2 = esriFieldType.esriFieldTypeDouble;
             pFldsEdt.AddField(pFldEdt); 

             pFldEdt = new FieldClass();
             pFldEdt.Name_2 = "RADIUS1";
             pFldEdt.AliasName_2 = "RADIUS1";
             pFldEdt.Type_2 = esriFieldType.esriFieldTypeDouble;
             pFldsEdt.AddField(pFldEdt);

             pFldEdt = new FieldClass();
             pFldEdt.Name_2 = "RADIUS2";
             pFldEdt.AliasName_2 = "RADIUS2";
             pFldEdt.Type_2 = esriFieldType.esriFieldTypeDouble;
             pFldsEdt.AddField(pFldEdt);

             pFldEdt = new FieldClass();
             pFldEdt.Name_2 = "VERT1";
             pFldEdt.AliasName_2 = "VERT1";
             pFldEdt.Type_2 = esriFieldType.esriFieldTypeDouble;
             pFldsEdt.AddField(pFldEdt);

             pFldEdt = new FieldClass();
             pFldEdt.Name_2 = "VERT2";
             pFldEdt.AliasName_2 = "VERT2";
             pFldEdt.Type_2 = esriFieldType.esriFieldTypeDouble;
             pFldsEdt.AddField(pFldEdt); 
 
             //Now add each field: 
             //foreach (CSVField field in fields) 
             //{ 
             //    pFldEdt = new FieldClass(); 
             //    pFldEdt.Name_2 = field.FieldName; 
             //    pFldEdt.AliasName_2 = field.FieldName; 
             //    switch (field.FieldType) 
             //    { 
             //        case CSVFieldType.INT: 
             //            pFldEdt.Type_2 = esriFieldType.esriFieldTypeInteger; 
             //            break; 
             //        case CSVFieldType.DOUBLE: 
             //            pFldEdt.Type_2 = esriFieldType.esriFieldTypeDouble; 
             //            break; 
             //        case CSVFieldType.STRING: 
             //            pFldEdt.Type_2 = esriFieldType.esriFieldTypeString; 
             //            break; 
             //        default: 
             //            throw new InvalidProgramException(); 
             //    } 
             //    pFldsEdt.AddField(pFldEdt); 
             //} 
 
             IFeatureClass pFClass = featWorkspace.CreateFeatureClass(name, pFldsEdt, null, null, esriFeatureType.esriFTSimple, "SHAPE", ""); 
 
             return pFClass; 
         } 
    }
}
