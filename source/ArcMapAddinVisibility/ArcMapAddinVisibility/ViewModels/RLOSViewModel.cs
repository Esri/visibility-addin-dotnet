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
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.GeoAnalyst;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geoprocessor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Forms;
using System.Timers;
using System.Windows;

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
        public bool ShowNonVisibleData { get; set; }
        public int RunCount { get; set; }

        private bool isCancelEnabled;
        public bool IsCancelEnabled
        {
            get { return isCancelEnabled; }
            set
            {
                isCancelEnabled = value;
                RaisePropertyChanged(() => IsCancelEnabled);
            }
        }

        private bool isOkEnabled;
        public bool IsOkEnabled
        {
            get { return isOkEnabled; }
            set
            {
                isOkEnabled = value;
                RaisePropertyChanged(() => IsOkEnabled);
            }
        }

        private bool isClearEnabled;
        public bool IsClearEnabled
        {
            get { return isClearEnabled; }
            set
            {
                isClearEnabled = value;
                RaisePropertyChanged(() => IsClearEnabled);
            }
        }

        private Visibility _displayProgressBar;
        public Visibility DisplayProgressBar
        {
            get
            {
                return _displayProgressBar;
            }
            set
            {
                _displayProgressBar = value;
                RaisePropertyChanged(() => DisplayProgressBar);
            }
        }

        #endregion

        #region Commands

        public RelayCommand SubmitCommand { get; set; }

        private void OnSubmitCommand(object obj)
        {
            DisplayProgressBar = Visibility.Visible;
            CreateMapElement();
            DisplayProgressBar = Visibility.Hidden;
        }

        private void OnCancelCommand(object obj)
        {
            Reset(true);
        }

        private void OnClearCommand(object obj)
        {
            Reset(true);
        }



        #endregion

        /// <summary>
        /// One and only constructor
        /// </summary>
        public RLOSViewModel()
        {
            SurfaceOffset = 0.0;
            MinDistance = 0.0;
            MaxDistance = 1000;
            LeftHorizontalFOV = 0.0;
            RightHorizontalFOV = 360.0;
            BottomVerticalFOV = -90.0;
            TopVerticalFOV = 90.0;
            ShowNonVisibleData = false;
            RunCount = 1;
            IsClearEnabled = false;
            IsOkEnabled = false;
            IsCancelEnabled = false;
            DisplayProgressBar = Visibility.Hidden;

            // commands
            SubmitCommand = new RelayCommand(OnSubmitCommand);
            ClearGraphicsCommand = new RelayCommand(OnClearCommand);
            CancelCommand = new RelayCommand(OnCancelCommand);
        }

        #region override

        internal override void OnDeletePointCommand(object obj)
        {
            base.OnDeletePointCommand(obj);

            EnableOkCancelClearBtns(ObserverPoints.Any());
        }

        internal override void OnDeleteAllPointsCommand(object obj)
        {
            base.OnDeleteAllPointsCommand(obj);

            EnableOkCancelClearBtns(ObserverPoints.Any());
        }

        public override bool CanCreateElement
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(SelectedSurfaceName)
                    && ObserverPoints.Any());
            }
        }

        /// <summary>
        /// Where all of the work is done.  Override from TabBaseViewModel
        /// </summary>
        internal override void CreateMapElement()
        {
            if (!CanCreateElement || ArcMap.Document == null || ArcMap.Document.FocusMap == null || string.IsNullOrWhiteSpace(SelectedSurfaceName))
                return;

            //base.CreateMapElement();

            var surface = GetSurfaceFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName);

            if (surface == null)
                return;

            using (ComReleaser oComReleaser = new ComReleaser())
            {

                // Create feature workspace
                IFeatureWorkspace workspace = CreateFeatureWorkspace("tempWorkspace");

                StartEditOperation((IWorkspace)workspace);

                // Create feature class
                IFeatureClass pointFc = CreateObserversFeatureClass(workspace, "Output" + RunCount.ToString());

                double finalObserverOffset = GetOffsetInZUnits(ArcMap.Document.FocusMap, ObserverOffset.Value, surface.ZFactor, OffsetUnitType);
                double finalSurfaceOffset = GetOffsetInZUnits(ArcMap.Document.FocusMap, SurfaceOffset, surface.ZFactor, OffsetUnitType);

                double conversionFactor = GetConversionFactor(ArcMap.Document.FocusMap.SpatialReference);
                double convertedMinDistance = MinDistance * conversionFactor;
                double convertedMaxDistance = MaxDistance * conversionFactor;
                double finalMinDistance = GetLinearDistance(ArcMap.Document.FocusMap, convertedMinDistance, OffsetUnitType);
                double finalMaxDistance = GetLinearDistance(ArcMap.Document.FocusMap, convertedMaxDistance, OffsetUnitType);

                double finalLeftHorizontalFOV = GetAngularDistance(ArcMap.Document.FocusMap, LeftHorizontalFOV, AngularUnitType);
                double finalRightHorizontalFOV = GetAngularDistance(ArcMap.Document.FocusMap, RightHorizontalFOV, AngularUnitType);
                double finalBottomVerticalFOV = GetAngularDistance(ArcMap.Document.FocusMap, BottomVerticalFOV, AngularUnitType);
                double finalTopVerticalFOV = GetAngularDistance(ArcMap.Document.FocusMap, TopVerticalFOV, AngularUnitType);

                // Out radius geometries
                List<IGeometry> radius2GeomList = new List<IGeometry>();
                List<IGeometry> radius1_2GeomList = new List<IGeometry>();
                List<IGeometry> donutGeomList = new List<IGeometry>();

                foreach (IPoint observerPoint in ObserverPoints)
                {
                    // Create buffer geometries for final Min/Max distance
                    ITopologicalOperator topologicalOperator = observerPoint as ITopologicalOperator;
                    IGeometry geom = topologicalOperator.Buffer(finalMaxDistance);
                    radius2GeomList.Add(geom);
                    radius1_2GeomList.Add(geom);
                    if (finalMinDistance > 0)
                    {
                        IGeometry geom2 = topologicalOperator.Buffer(finalMinDistance);
                        
                        ITopologicalOperator eraseTopo = geom as ITopologicalOperator;
                        IGeometry erasedGeom = eraseTopo.Difference(geom2);
                        donutGeomList.Add(erasedGeom);
                    }    
                    else
                    {
                        radius1_2GeomList.Add(geom);
                    }

                    double z1 = surface.GetElevation(observerPoint) + finalObserverOffset;

                    //create a new point feature
                    IFeature ipFeature = pointFc.CreateFeature();

                    // Set the field values for the feature
                    SetFieldValues(finalObserverOffset, finalSurfaceOffset, finalMinDistance, finalMaxDistance, finalLeftHorizontalFOV, 
                        finalRightHorizontalFOV, finalBottomVerticalFOV, finalTopVerticalFOV, ipFeature);

                    //Create shape 
                    IPoint point = new PointClass() { Z = z1, X = observerPoint.X, Y = observerPoint.Y, ZAware = true };
                    ipFeature.Shape = point;
                    ipFeature.Store();
                }

                IFeatureClassDescriptor fd = new FeatureClassDescriptorClass();
                fd.Create(pointFc, null, "OBJECTID");

                StopEditOperation((IWorkspace)workspace);

                try
                {
                    ILayer layer = GetLayerFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName);
                    string layerPath = GetLayerPath(layer);

                IFeatureLayer ipFeatureLayer = new FeatureLayerClass();
                ipFeatureLayer.FeatureClass = pointFc;

                IDataset ipDataset = (IDataset)pointFc;
                string outputFcName = ipDataset.BrowseName + "_output";
                string strPath = ipDataset.Workspace.PathName + "\\" + ipDataset.BrowseName;
                string outPath = ipDataset.Workspace.PathName + "\\" + outputFcName;

                IVariantArray parameters = new VarArrayClass();
                    parameters.Add(layerPath);
                parameters.Add(strPath);
                parameters.Add(outPath);

                    esriLicenseStatus status = GetSpatialAnalystLicense();

                    IGeoProcessor2 gp = new GeoProcessorClass();

                    gp.AddOutputsToMap = false;

                    // Add a mask to buffer the output to selected distance
                    SetGPMask(workspace, radius2GeomList, gp, "radiusMask");

                    object oResult = gp.Execute("Visibility_sa", parameters, null);
                    IGeoProcessorResult ipResult = (IGeoProcessorResult)oResult;

                    ComReleaser.ReleaseCOMObject(gp);
                    gp = null;
                    GC.Collect();

                    // Add buffer geometries to the map
                    foreach (IGeometry geom in radius1_2GeomList)
                    {
                        var color = new RgbColorClass() { Blue = 255 } as IColor;
                        AddGraphicToMap(geom, color, true);
                    }

                    IRasterLayer outputRasterLayer = new RasterLayerClass();
                    outputRasterLayer.CreateFromFilePath(outPath);

                    string fcName = IntersectOutput(outputRasterLayer, ipDataset, workspace, donutGeomList);
          
                    IFeatureClass finalFc = workspace.OpenFeatureClass(fcName);

                    IFeatureLayer outputFeatureLayer = new FeatureLayerClass();
                    outputFeatureLayer.FeatureClass = finalFc;

                    //Add it to a map if the layer is valid.
                    if (outputFeatureLayer != null)
                    {
                        // set the renderer
                        IFeatureRenderer featRend = UnqueValueRenderer(workspace, finalFc);
                        IGeoFeatureLayer geoLayer = outputFeatureLayer as IGeoFeatureLayer;
                        geoLayer.Renderer = featRend;
                        geoLayer.Name = "VisibilityLayer_" + RunCount.ToString();

                        // Set the layer transparency
                        IDisplayFilterManager filterManager = (IDisplayFilterManager)outputFeatureLayer;
                        ITransparencyDisplayFilter filter = new TransparencyDisplayFilter(); 
 			            filter.Transparency = 80; 
 			            filterManager.DisplayFilter = filter; 

                        ESRI.ArcGIS.Carto.IMap map = ArcMap.Document.FocusMap;
                        map.AddLayer((ILayer)outputFeatureLayer);
                    }

                    RunCount += 1;
                }
                catch (Exception ex)
                {
                    string exception = ex.ToString();
                    System.Windows.MessageBox.Show("Results not calculated.  Check input parameters and try again.", "Calculation cancelled");
                }

                //Reset(true);
            }
        }

        internal override void Reset(bool toolReset)
        {
            base.Reset(toolReset);

            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return;

            // Disable buttons
            EnableOkCancelClearBtns(false);
        }

        /// <summary>
        /// Override this event to collect observer points based on tool mode
        /// Setting the observer point to blue since the output is green / red
        /// </summary>
        /// <param name="obj"></param>
        internal override void OnNewMapPointEvent(object obj)
        {
            base.OnNewMapPointEvent(obj);

            if (!IsActiveTab)
                return;

            var point = obj as IPoint;

            if (point == null)
                return;

            EnableOkCancelClearBtns(ObserverPoints.Any());
        }

        #endregion

        #region public

        public IFeatureRenderer UnqueValueRenderer(IFeatureWorkspace workspace, IFeatureClass fc)
        {
            try
            {
                string tablename = ((IDataset)fc).BrowseName;
                ITable ipTable = workspace.OpenTable(tablename);

                IFeatureCursor featureCursor = fc.Search(null, false);
                IDataStatistics dataStatistics = new DataStatisticsClass();
                dataStatistics.Cursor = featureCursor as ICursor;
                dataStatistics.Field = "gridcode";

                System.Collections.IEnumerator enumerator = dataStatistics.UniqueValues;
                enumerator.Reset();

                while (enumerator.MoveNext())
                {
                    object myObject = enumerator.Current;
                }

                int uniqueValues = dataStatistics.UniqueValueCount;

                //Create colors for each unique value.
                IRandomColorRamp colorRamp = new RandomColorRampClass();
                colorRamp.Size = uniqueValues;
                colorRamp.Seed = 100;
                bool createColorRamp;
                colorRamp.CreateRamp(out createColorRamp);
                if (createColorRamp == false)
                {
                    return null;
                }

                IUniqueValueRenderer uvRenderer = new UniqueValueRendererClass();
                IFeatureRenderer featRenderer = (IFeatureRenderer)uvRenderer;

                uvRenderer.FieldCount = 1;

                ISimpleFillSymbol fillSymbol = new SimpleFillSymbolClass();
                ISimpleFillSymbol fillSymbol2 = new SimpleFillSymbolClass();
                if (ShowNonVisibleData == true)
                {                  
                    fillSymbol.Color = new RgbColorClass() { Red = 255 } as IColor;
                    uvRenderer.AddValue("0", "", fillSymbol as ISymbol);
                    uvRenderer.set_Label("0", "Non-Visible");                
                }
                    fillSymbol2.Color = new RgbColorClass() { Green = 255 } as IColor;
                    uvRenderer.AddValue("1", "", fillSymbol2 as ISymbol);
                    uvRenderer.set_Label("1", "Visible by 1 Observer");

                    int field = ipTable.FindField("gridcode");
                    uvRenderer.set_Field(0, "gridcode");

                    for (int i = 2; i < uniqueValues; i++)
                    {
                        ISimpleFillSymbol newFillSymbol = new SimpleFillSymbolClass();
                        newFillSymbol.Color = colorRamp.get_Color(i);
                        uvRenderer.AddValue(i.ToString(), "", newFillSymbol as ISymbol);
                        string label = "Visible by " + i.ToString() + " Observers";
                        uvRenderer.set_Label(i.ToString(), label);
                    }

                return featRenderer;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Check out spatial analyst license
        /// </summary>
        /// <returns>esriLicenseStatus</returns>
        public esriLicenseStatus GetSpatialAnalystLicense()
        {
            //Check out a Spatial Analyst license with the ArcView product. 
            esriLicenseProductCode productCode =
                esriLicenseProductCode.esriLicenseProductCodeAdvanced;
            IAoInitialize pAoInitialize = new AoInitializeClass();
            esriLicenseStatus licenseStatus = esriLicenseStatus.esriLicenseUnavailable;
            //Check the productCode. 
            licenseStatus = pAoInitialize.IsProductCodeAvailable(productCode);
            if (licenseStatus == esriLicenseStatus.esriLicenseAvailable)
            {
                //Check the extensionCode. 
                licenseStatus = pAoInitialize.IsExtensionCodeAvailable(productCode,
                    esriLicenseExtensionCode.esriLicenseExtensionCodeSpatialAnalyst);
                if (licenseStatus == esriLicenseStatus.esriLicenseAvailable)
                {
                    //Initialize the license. 
                    licenseStatus = pAoInitialize.Initialize(productCode);
                    if ((licenseStatus == esriLicenseStatus.esriLicenseCheckedOut))
                    {
                        //Check out the Spatial Analyst extension. 
                        licenseStatus = pAoInitialize.CheckOutExtension
                            (esriLicenseExtensionCode.esriLicenseExtensionCodeSpatialAnalyst)
                            ;
                    }
                }
            }

            return licenseStatus;
        }

        /// <summary>
        /// Start Editing operation
        /// </summary>
        /// <param name="ipWorkspace">IWorkspace</param>
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

        /// <summary>
        /// Stop Editing operation
        /// </summary>
        /// <param name="ipWorkspace">IWorkspace</param>
        /// <returns>True if successful, false otherwise</returns>
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
        /// Create the feature workspace 
        /// </summary>
        /// <param name="workspaceNameString">Workspace name</param>
        /// <returns></returns>
        public static IFeatureWorkspace CreateFeatureWorkspace(string workspaceNameString)
        {

            IScratchWorkspaceFactory2 ipScWsFactory = new FileGDBScratchWorkspaceFactoryClass();
            IWorkspace ipScWorkspace = ipScWsFactory.CurrentScratchWorkspace;
            if (null == ipScWorkspace)
                ipScWorkspace = ipScWsFactory.CreateNewScratchWorkspace();

            IFeatureWorkspace featWork = (IFeatureWorkspace)ipScWorkspace;

            return featWork;

        }

        /// <summary> 
        /// Create the point feature class for observer locations
        /// </summary> 
        /// <param name="featWorkspace"></param> 
        /// <param name="name"></param> 
        /// <returns></returns> 
        public static IFeatureClass CreateObserversFeatureClass(IFeatureWorkspace featWorkspace, string name)
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

            IFeatureClass pFClass = featWorkspace.CreateFeatureClass(name, pFldsEdt, null, null, esriFeatureType.esriFTSimple, "SHAPE", "");

            return pFClass;
        }

        #endregion

        #region private

        /// <summary>
        /// Enable or disable the form buttons
        /// </summary>
        /// <param name="enable">true to enable</param>
        private void EnableOkCancelClearBtns(bool enable)
        {
            IsOkEnabled = enable;
            IsCancelEnabled = enable;
            IsClearEnabled = enable;
        }

        /// <summary>
        /// Run RasterToPoly tool to convert input raster to poly's.  Then run Intersect if input geomList has features
        /// </summary>
        /// <param name="rasterLayer"></param>
        /// <param name="ipDataset"></param>
        /// <param name="workspace"></param>
        /// <param name="geomList"></param>
        /// <returns>Featureclass name</returns>
        private string IntersectOutput(IRasterLayer rasterLayer, IDataset ipDataset, IFeatureWorkspace workspace, List<IGeometry> geomList)
        {
            IGeoProcessor2 gp = new GeoProcessorClass();
            gp.AddOutputsToMap = false;

            // Run RasterToPolygon
            string inRaster = rasterLayer.FilePath;
            string outRasterToPolyFcName = ipDataset.BrowseName + "_rasterToPoly";
            string outRasterToPolyPath = ipDataset.Workspace.PathName + "\\" + outRasterToPolyFcName;
            string field = "VALUE";

            IVariantArray rasterToPolyParams = new VarArrayClass();
            rasterToPolyParams.Add(inRaster);
            rasterToPolyParams.Add(outRasterToPolyPath);
            rasterToPolyParams.Add("NO_SIMPLIFY");
            rasterToPolyParams.Add(field);

            try
            {
                object oResult = gp.Execute("RasterToPolygon_conversion", rasterToPolyParams, null);
                IGeoProcessorResult ipResult = (IGeoProcessorResult)oResult;

                if (geomList.Count == 0)
                    return outRasterToPolyFcName;

                if (ipResult.Status == esriJobStatus.esriJobSucceeded)
                {
                    string outFcName = ipDataset.BrowseName + "_intersectRaster";
                    string outPath = ipDataset.Workspace.PathName + "\\" + outFcName;

                    // Add a mask to buffer the output to selected distance
                    string fcPath = SetGPMask(workspace, geomList, gp, "intersectMask");
                    string pathParam = outRasterToPolyPath + ";" + fcPath;

                    IVariantArray parameters = new VarArrayClass();
                    parameters.Add(pathParam);
                    parameters.Add(outPath);

                    object oResult2 = gp.Execute("Intersect_analysis", parameters, null);
                    IGeoProcessorResult ipResult2 = (IGeoProcessorResult)oResult2;
                    return outFcName;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        /// <summary>
        /// Set a mask on the gp object to buffer the output to the specified distance
        /// </summary>
        /// <param name="workspace">IFeatureWorkspace</param>
        /// <param name="geomList">List of geometries to create the mask</param>
        /// <param name="gp">IGeoProcessor2</param>
        private string SetGPMask(IFeatureWorkspace workspace, List<IGeometry> geomList, IGeoProcessor2 gp, string fcName)
        {
            IFeatureClass maskFc = CreateMaskFeatureClass(workspace, fcName + "_" + RunCount.ToString());

            foreach (IGeometry geom in geomList)
            {
                //create a new point feature
                IFeature ipFeature = maskFc.CreateFeature();
                ipFeature.Shape = geom;
                ipFeature.Store();
            }
            IDataset ds = (IDataset)maskFc;
            string path = ds.Workspace.PathName + "\\" + ds.BrowseName;
            gp.SetEnvironmentValue("mask", path);

            return path;
        }

        /// <summary>
        /// Set the values for the associated fields
        /// </summary>
        /// <param name="finalObserverOffset">Observer offset</param>
        /// <param name="finalSurfaceOffset">Surface offset</param>
        /// <param name="finalMinDistance">Minimum distance</param>
        /// <param name="finalMaxDistance">Maximum distance</param>
        /// <param name="finalLeftHorizontalFOV">Left horizontal field of view</param>
        /// <param name="finalRightHorizontalFOV">Right horizontal field of view</param>
        /// <param name="finalBottomVerticalFOV">Bottom vertical field of view</param>
        /// <param name="finalTopVerticalFOV">Top vertical field of view</param>
        /// <param name="ipFeature">IFeature</param>
        private static void SetFieldValues(double finalObserverOffset, double finalSurfaceOffset, double finalMinDistance, double finalMaxDistance,
            double finalLeftHorizontalFOV, double finalRightHorizontalFOV, double finalBottomVerticalFOV, double finalTopVerticalFOV, IFeature ipFeature)
        {
            // Observer Offset
            SetDatabaseFieldValue(ipFeature, "OFFSETA", finalObserverOffset);
            // Surface Offset
            SetDatabaseFieldValue(ipFeature, "OFFSETB", finalSurfaceOffset);
            // Horizontal FOV
            SetDatabaseFieldValue(ipFeature, "AZIMUTH1", finalLeftHorizontalFOV);
            SetDatabaseFieldValue(ipFeature, "AZIMUTH2", finalRightHorizontalFOV);
            // Distance
            SetDatabaseFieldValue(ipFeature, "RADIUS1", finalMinDistance);
            SetDatabaseFieldValue(ipFeature, "RADIUS2", finalMaxDistance);
            // Vertical FOV
            SetDatabaseFieldValue(ipFeature, "VERT1", finalBottomVerticalFOV);
            SetDatabaseFieldValue(ipFeature, "VERT1", finalTopVerticalFOV);
        }

        /// <summary>
        /// Generates a raster renderer using the provided settings
        /// </summary>
        /// <returns>IRasterRenderer</returns>
        private static IRasterRenderer GenerateRasterRenderer(IRaster pRaster)
        {
            IRasterStretchColorRampRenderer pStretchRen = new RasterStretchColorRampRenderer();
            //IRasterUniqueValueRenderer pStretchRen = new RasterUniqueValueRenderer();
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

        /// </summary>
        /// <param name="map">IMap</param>
        /// <param name="inputDistance">the input distance</param>
        /// <param name="distanceType">the "from" distance unit type</param>
        /// <returns></returns>
        private double GetLinearDistance(IMap map, double inputDistance, DistanceTypes distanceType)
        {
            if (map.SpatialReference == null)
                return inputDistance;

            DistanceTypes distanceTo = DistanceTypes.Meters; // default to meters

            var pcs = map.SpatialReference as IProjectedCoordinateSystem;

            if (pcs != null)
            {
                // need to convert the offset from the input distance type to the spatial reference linear type
                distanceTo = GetDistanceType(pcs.CoordinateUnit.FactoryCode);
            }
            else
            {
                var gcs = map.SpatialReference as IGeographicCoordinateSystem;
                if (gcs != null)
                {
                    distanceTo = GetDistanceType(gcs.CoordinateUnit.FactoryCode);

                }
            }

            var result = GetDistanceFromTo(distanceType, distanceTo, inputDistance);

            return result;
        }

        /// <summary>
        /// Create a polygon feature class used to store buffer geometries for observer points
        /// </summary>
        /// <param name="featWorkspace">IFeatureWorkspace</param>
        /// <param name="name">Name of the feature class</param>
        /// <returns>IFeatureClass</returns>
        private static IFeatureClass CreateMaskFeatureClass(IFeatureWorkspace featWorkspace, string name)
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
            pGeoDef.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            pGeoDef.SpatialReference_2 = ArcMap.Document.FocusMap.SpatialReference;

            pFldEdt = new FieldClass();
            pFldEdt.Name_2 = "SHAPE";
            pFldEdt.AliasName_2 = "SHAPE";
            pFldEdt.Type_2 = esriFieldType.esriFieldTypeGeometry;
            pFldEdt.GeometryDef_2 = pGeoDef;
            pFldsEdt.AddField(pFldEdt);

            IFeatureClass pFClass = featWorkspace.CreateFeatureClass(name, pFldsEdt, null, null, esriFeatureType.esriFTSimple, "SHAPE", "");

            return pFClass;
        }

        /// <summary>
        /// Gets native units to meters conversion factor
        /// </summary>
        /// <param name="ipSpatialReference">The spatial reference</param>
        /// <returns>The meters conversion factor</returns>
        private static double GetConversionFactor(ISpatialReference ipSpatialReference)
        {
            double dConversionFactor = 0.0;

            if (ipSpatialReference is IGeographicCoordinateSystem)
            {
                IAngularUnit ipAngularUnit = ((IGeographicCoordinateSystem)ipSpatialReference).CoordinateUnit;
                dConversionFactor = ipAngularUnit.ConversionFactor;
            }
            else
            {
                ILinearUnit ipLinearUnit = ((IProjectedCoordinateSystem)ipSpatialReference).CoordinateUnit;
                dConversionFactor = ipLinearUnit.ConversionFactor;
            }
            return dConversionFactor;
        }

        /// <summary>
        /// Retrieves the angular type based on the esriSRUnit type
        /// </summary>
        /// <param name="angularUnitFactoryCode">factory unit code for the map</param>
        /// <returns>AngularTypes unit type</returns>
        private AngularTypes GetAngularType(int angularUnitFactoryCode)
        {
            // default to degrees
            AngularTypes angularType = AngularTypes.DEGREES;
            switch (angularUnitFactoryCode)
            {
                case (int)esriSRUnitType.esriSRUnit_Degree:
                    angularType = AngularTypes.DEGREES;
                    break;
                case (int)esriSRUnitType.esriSRUnit_Grad:
                    angularType = AngularTypes.GRADS;
                    break;
                case (int)esriSRUnitType.esriSRUnit_Mil6400:
                    angularType = AngularTypes.MILS;
                    break;
                default:
                    angularType = AngularTypes.DEGREES;
                    break;
            }

            return angularType;
        }

        /// <summary>
        /// Method to get an angular distance in the correct units for the map
        /// </summary>
        /// <param name="map">IMap</param>
        /// <param name="inputDistance">the input distance</param>
        /// <param name="distanceType">the "from" distance unit type</param>
        /// <returns></returns>
        private double GetAngularDistance(IMap map, double inputDistance, AngularTypes angularType)
        {
            if (map.SpatialReference == null)
                return inputDistance;

            AngularTypes angularDistanceTo = AngularTypes.DEGREES; // default to degrees

            var gcs = map.SpatialReference as IGeographicCoordinateSystem;

            if (gcs != null)
            {
                angularDistanceTo = GetAngularType(gcs.CoordinateUnit.FactoryCode);
            }

            var result = GetAngularDistanceFromTo(angularType, angularDistanceTo, inputDistance);

            return result;
        }

        /// <summary>
        /// Method to convert to/from different types of angular units
        /// </summary>
        /// <param name="fromType">DistanceTypes</param>
        /// <param name="toType">DistanceTypes</param>
        private double GetAngularDistanceFromTo(AngularTypes fromType, AngularTypes toType, double input)
        {
            double angularDistance = input;

            try
            {
                if (fromType == AngularTypes.DEGREES && toType == AngularTypes.GRADS)
                    angularDistance *= 1.11111;
                else if (fromType == AngularTypes.DEGREES && toType == AngularTypes.MILS)
                    angularDistance *= 17.777777777778;
                else if (fromType == AngularTypes.GRADS && toType == AngularTypes.DEGREES)
                    angularDistance /= 1.11111;
                else if (fromType == AngularTypes.GRADS && toType == AngularTypes.MILS)
                    angularDistance *= 16;
                else if (fromType == AngularTypes.MILS && toType == AngularTypes.DEGREES)
                    angularDistance /= 17.777777777778;
                else if (fromType == AngularTypes.MILS && toType == AngularTypes.GRADS)
                    angularDistance /= 16;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return angularDistance;
        }  

        /// <summary>
        /// Return the layer file path of the provided layer
        /// </summary>
        /// <param name="layer">ILayer</param>
        /// <returns>file path of layer</returns>
        private static string GetLayerPath(ILayer layer)
        {
            if (layer is IRasterLayer)
            {
                IRasterLayer rlayer = layer as IRasterLayer;
                return rlayer.FilePath;
            }
            else if (layer is IMosaicLayer)
            {
                IMosaicLayer mlayer = layer as IMosaicLayer;
                return mlayer.FilePath;
            }

            return null;
        }

        #endregion
    }
}
