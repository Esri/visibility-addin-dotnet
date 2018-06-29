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

// System
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

// Esri
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.GeoAnalyst;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geoprocessing;

// Solution
using VisibilityLibrary.Helpers;
using VisibilityLibrary;

namespace ArcMapAddinVisibility.ViewModels
{

    public class RLOSViewModel : LOSBaseViewModel
    {

        #region Properties

        private double _SurfaceOffset = 0.0;
        public double SurfaceOffset
        {
            get { return _SurfaceOffset; }
            set
            {
                if (value < 0.0)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEMustBePositive);

                _SurfaceOffset = value;
                RaisePropertyChanged(() => SurfaceOffset);
            }
        }

        private double _MinDistance = 0.0;
        public double MinDistance
        {
            get { return _MinDistance; }
            set
            {
                if (value < 0.0)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEMustBePositive);

                if (value > MaxDistance)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AENumMustBeLess);

                _MinDistance = value;
                RaisePropertyChanged(() => MinDistance);
            }
        }

        private double _MaxDistance = 1000.0;
        public double MaxDistance
        {
            get { return _MaxDistance; }
            set
            {
                if (value < 0.0)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEMustBePositive);

                if (value < MinDistance)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AENumMustBeGreater);

                _MaxDistance = value;
                RaisePropertyChanged(() => MaxDistance);
            }
        }

        private double _LeftHorizontalFOV = 0.0;
        public double LeftHorizontalFOV
        {
            get { return _LeftHorizontalFOV; }
            set
            {
                var checkAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, value);

                if (checkAngleInDegrees < 0.0 || checkAngleInDegrees > 360.0)
                    throw new ArgumentException(string.Format(VisibilityLibrary.Properties.Resources.AENumRange, 0, GetAngularDistanceFromTo(AngularTypes.DEGREES, AngularUnitType, 360.0)));

                _LeftHorizontalFOV = value;
                RaisePropertyChanged(() => LeftHorizontalFOV);
            }
        }
        private double _RightHorizontalFOV = 360.0;
        public double RightHorizontalFOV
        {
            get { return _RightHorizontalFOV; }
            set
            {
                var checkAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, value);

                if (checkAngleInDegrees < 0.0 || checkAngleInDegrees > 360.0)
                    throw new ArgumentException(string.Format(VisibilityLibrary.Properties.Resources.AENumRange, 0, GetAngularDistanceFromTo(AngularTypes.DEGREES, AngularUnitType, 360.0)));

                _RightHorizontalFOV = value;
                RaisePropertyChanged(() => RightHorizontalFOV);
            }
        }
        private double _BottomVerticalFOV = -90.0;
        public double BottomVerticalFOV
        {
            get { return _BottomVerticalFOV; }
            set
            {
                var checkAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, value);

                if (checkAngleInDegrees < -90.0 || checkAngleInDegrees > 0.0)
                    throw new ArgumentException(string.Format(VisibilityLibrary.Properties.Resources.AENumRange, GetAngularDistanceFromTo(AngularTypes.DEGREES, AngularUnitType, -90.0), 0.0));

                _BottomVerticalFOV = value;
                RaisePropertyChanged(() => BottomVerticalFOV);
            }
        }

        private double _TopVerticalFOV = 90.0;
        public double TopVerticalFOV
        {
            get { return _TopVerticalFOV; }
            set
            {
                var checkAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, value);

                if (checkAngleInDegrees < -0.0 || checkAngleInDegrees > 90.0)
                    throw new ArgumentException(string.Format(VisibilityLibrary.Properties.Resources.AENumRange, 0.0, GetAngularDistanceFromTo(AngularTypes.DEGREES, AngularUnitType, 90.0)));

                _TopVerticalFOV = value;
                RaisePropertyChanged(() => TopVerticalFOV);
            }
        }

        public bool ShowNonVisibleData { get; set; }
        public int RunCount { get; set; }

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
        }

        internal override void OnDeleteAllPointsCommand(object obj)
        {
            base.OnDeleteAllPointsCommand(obj);
        }

        public override bool CanCreateElement
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(SelectedSurfaceName)
                    && ObserverAddInPoints.Any());
            }
        }

        /// <summary>
        /// Returns a polygon with a range fan(circular ring sector - like a donut wedge or wiper blade swipe with inner and outer radius)
        /// from the input parameters
        /// </summary>
        public static IGeometry ConstructRangeFan(IPoint centerPoint,
            double innerDistanceInMapUnits, double outerDistanceInMapUnits,
            double horizontalStartAngleInBearing, double horizontalEndAngleInBearing,
            ISpatialReference sr, double incrementAngleStep = 1.0)
        {
            if ((centerPoint == null) || (sr == null) ||
                (innerDistanceInMapUnits < 0.0) || (outerDistanceInMapUnits < 0.0) ||
                (horizontalStartAngleInBearing < 0.0) || (horizontalStartAngleInBearing > 360.0) ||
                (horizontalEndAngleInBearing < 0.0) || (horizontalEndAngleInBearing > 360.0))
                return null;

            // Tricky - if angle cuts across 360, need to adjust for this case (ex.Angle: 270->90)
            if (horizontalStartAngleInBearing > horizontalEndAngleInBearing)
                horizontalStartAngleInBearing = -(360.0 - horizontalStartAngleInBearing);

            double deltaAngle = Math.Abs(horizontalStartAngleInBearing - horizontalEndAngleInBearing);

            // Create Polygon to store points
            IPointCollection points = new PolygonClass();

            // if full circle(or greater), return donut section with inner/outer rings
            if ((deltaAngle == 0.0) || (deltaAngle >= 360.0))
            {
                IGeometryCollection geometryCollection = (IGeometryCollection)points;

                ICircularArc circularArcOuter = new CircularArcClass();
                ISegmentCollection ringOuter = new RingClass();
                circularArcOuter.PutCoordsByAngle(centerPoint, 0.0, 2 * Math.PI, outerDistanceInMapUnits);
                ringOuter.AddSegment((ISegment)circularArcOuter);
                geometryCollection.AddGeometry((IGeometry)ringOuter);

                if (innerDistanceInMapUnits > 0.0)
                {
                    ICircularArc circularArcInner = new CircularArcClass();
                    ISegmentCollection ringInner = new RingClass();
                    circularArcInner.PutCoordsByAngle(centerPoint, 0.0, 2 * Math.PI, innerDistanceInMapUnits);
                    ringInner.AddSegment((ISegment)circularArcInner);
                    geometryCollection.AddGeometry((IGeometry)ringInner);
                }

                ITopologicalOperator topOp = points as ITopologicalOperator;
                if (topOp != null)
                    topOp.Simplify();

                return (IGeometry)points;
            }

            // Otherwise if range fan, construct that
            IPoint startPoint = null;

            if (innerDistanceInMapUnits == 0.0)
            {
                startPoint = centerPoint;
                points.AddPoint(startPoint);
            }

            double minAngle = Math.Min(horizontalStartAngleInBearing, horizontalEndAngleInBearing);
            double maxAngle = Math.Max(horizontalStartAngleInBearing, horizontalEndAngleInBearing);

            // Draw Outer Arc of Ring
            // Implementation Note: because of the unique shape of this ring, 
            // it was easier to manually create these points than use IConstructCircularArc
            for (double angle = minAngle; angle <= maxAngle; angle += incrementAngleStep)
            {
                double cartesianAngle = (450 - angle) % 360;
                double angleInRadians = cartesianAngle * (Math.PI / 180.0);
                double x = centerPoint.X + (outerDistanceInMapUnits * Math.Cos(angleInRadians));
                double y = centerPoint.Y + (outerDistanceInMapUnits * Math.Sin(angleInRadians));

                IPoint pointToAdd = new PointClass();
                pointToAdd.PutCoords(x, y);
                pointToAdd.SpatialReference = sr;
                points.AddPoint(pointToAdd);

                if (startPoint == null)
                    startPoint = pointToAdd;
            }

            if (innerDistanceInMapUnits > 0.0)
            {
                // Draw Inner Arc of Ring - if inner distance set
                for (double angle = maxAngle; angle >= minAngle; angle -= incrementAngleStep)
                {
                    double cartesianAngle = (450 - angle) % 360;
                    double angleInRadians = cartesianAngle * (Math.PI / 180.0);
                    double x = centerPoint.X + (innerDistanceInMapUnits * Math.Cos(angleInRadians));
                    double y = centerPoint.Y + (innerDistanceInMapUnits * Math.Sin(angleInRadians));

                    IPoint pointToAdd = new PointClass();
                    pointToAdd.PutCoords(x, y);
                    pointToAdd.SpatialReference = sr;
                    points.AddPoint(pointToAdd);
                }
            }

            // close Polygon
            points.AddPoint(startPoint);

            return (IGeometry)points;
        }

        /// <summary>
        /// Where all of the work is done.  Override from TabBaseViewModel
        /// </summary>
        internal override void CreateMapElement()
        {
            try
            {
                IsRunning = true;

                if (!CanCreateElement || ArcMap.Document == null || ArcMap.Document.FocusMap == null 
                    || string.IsNullOrWhiteSpace(SelectedSurfaceName))
                    return;

                var surface = GetSurfaceFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName);
                if (surface == null)
                {
                    System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgTryAgain, VisibilityLibrary.Properties.Resources.CaptionError);
                    return;
                }

                bool spatialAnalystAvailable = IsSpatialAnalystAvailable();
                if (!spatialAnalystAvailable)
                {
                    System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.LOSSpatialAnalystLicenseInvalid, VisibilityLibrary.Properties.Resources.MsgCalcCancelled);
                    return;
                }

                ILayer surfaceLayer = GetLayerFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName);
                // Issue warning if layer is ImageServerLayer
                if (surfaceLayer is IImageServerLayer)
                {
                    MessageBoxResult mbr = MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgLayerIsImageService,
                        VisibilityLibrary.Properties.Resources.CaptionLayerIsImageService, MessageBoxButton.YesNo);

                    if (mbr == MessageBoxResult.No)
                    {
                        System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgTryAgain, VisibilityLibrary.Properties.Resources.MsgCalcCancelled);
                        return;
                    }
                }

                // Determine if selected surface is projected or geographic
                var geoDataset = surfaceLayer as IGeoDataset;
                if (geoDataset == null)
                {
                    System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgTryAgain, VisibilityLibrary.Properties.Resources.CaptionError);
                    return;
                }

                SelectedSurfaceSpatialRef = geoDataset.SpatialReference;

                if (SelectedSurfaceSpatialRef is IGeographicCoordinateSystem)
                {
                    MessageBox.Show(VisibilityLibrary.Properties.Resources.RLOSUserPrompt, VisibilityLibrary.Properties.Resources.RLOSUserPromptCaption);
                    return;
                }

                if (ArcMap.Document.FocusMap.SpatialReference.FactoryCode != geoDataset.SpatialReference.FactoryCode)
                {
                    MessageBox.Show(VisibilityLibrary.Properties.Resources.LOSDataFrameMatch, VisibilityLibrary.Properties.Resources.LOSSpatialReferenceCaption);
                    return;
                }

                using (ComReleaser oComReleaser = new ComReleaser())
                {
                    // Create feature workspace
                    IFeatureWorkspace workspace = CreateFeatureWorkspace("tempWorkspace");
                    if (workspace == null)
                    {
                        System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgTryAgain, VisibilityLibrary.Properties.Resources.CaptionError);
                        return;
                    }

                    StartEditOperation((IWorkspace)workspace);

                    // Create feature class
                    IFeatureClass pointFc = CreateObserversFeatureClass(workspace, SelectedSurfaceSpatialRef, "Output" + RunCount.ToString());
                    if (pointFc == null)
                    {
                        System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgTryAgain, VisibilityLibrary.Properties.Resources.CaptionError);
                        return;
                    }

                    double finalObserverOffset = GetOffsetInZUnits(ObserverOffset.Value, surface.ZFactor, OffsetUnitType);
                    double finalSurfaceOffset = GetOffsetInZUnits(SurfaceOffset, surface.ZFactor, OffsetUnitType);

                    double conversionFactor = GetConversionFactor(SelectedSurfaceSpatialRef);
                    string unitString = GetUnitString(SelectedSurfaceSpatialRef);
                    //unit of raster
                    DistanceTypes srUnit = GetMTUnitFromEsriUnit(unitString);
                    //get distance in map units
                    double muMaxDist = GetDistanceFromTo(OffsetUnitType, srUnit, MaxDistance);
                    double muMinDist = GetDistanceFromTo(OffsetUnitType, srUnit, MinDistance);
                    //Distance in meters
                    double convertedMinDistance = MinDistance * conversionFactor;
                    double convertedMaxDistance = MaxDistance * conversionFactor;

                    double finalMinDistance;
                    double finalMaxDistance;
                    if (srUnit.ToString() != OffsetUnitType.ToString())
                    {
                        finalMinDistance = GetLinearDistance(ArcMap.Document.FocusMap, convertedMinDistance, OffsetUnitType);
                        finalMaxDistance = GetLinearDistance(ArcMap.Document.FocusMap, convertedMaxDistance, OffsetUnitType);
                    }
                    else
                    {
                        finalMinDistance = GetDistanceFromTo(DistanceTypes.Meters, srUnit, convertedMinDistance);
                        finalMaxDistance = GetDistanceFromTo(DistanceTypes.Meters, srUnit, convertedMaxDistance);
                    }

                    double finalLeftHorizontalFOV = GetAngularDistance(ArcMap.Document.FocusMap, LeftHorizontalFOV, AngularUnitType);
                    double finalRightHorizontalFOV = GetAngularDistance(ArcMap.Document.FocusMap, RightHorizontalFOV, AngularUnitType);
                    double finalBottomVerticalFOV = GetAngularDistance(ArcMap.Document.FocusMap, BottomVerticalFOV, AngularUnitType);
                    double finalTopVerticalFOV = GetAngularDistance(ArcMap.Document.FocusMap, TopVerticalFOV, AngularUnitType);

                    // Output radius geometries
                    List<IGeometry> maxRangeBufferGeomList = new List<IGeometry>();
                    List<IGeometry> rangeFanGeomList = new List<IGeometry>();

                    foreach (var observerPoint in ObserverAddInPoints)
                    {
                        if ((observerPoint == null) || (observerPoint.Point == null))
                            continue;

                        // Create 2 clipping geometries:
                        // 1. maxRangeBufferGeomList - is used to clip the viz GP output because 2. doesn't work directly
                        // 2. rangeFanGeomList - this is the range fan input by the user
                        ITopologicalOperator topologicalOperator = observerPoint.Point as ITopologicalOperator;
                        if (topologicalOperator == null)
                            continue;

                        IGeometry geomBuffer = topologicalOperator.Buffer(muMaxDist);

                        maxRangeBufferGeomList.Add(geomBuffer);      

                        IGeometry geomRangeFan = ConstructRangeFan(observerPoint.Point, muMinDist, muMaxDist,
                            finalLeftHorizontalFOV, finalRightHorizontalFOV, SelectedSurfaceSpatialRef);
                        if (geomRangeFan != null)
                            rangeFanGeomList.Add(geomRangeFan);

                        double z1 = surface.GetElevation(observerPoint.Point) + finalObserverOffset;

                        //create a new point feature
                        IFeature ipFeature = pointFc.CreateFeature();

                        // Set the field values for the feature
                        SetFieldValues(finalObserverOffset, finalSurfaceOffset,muMinDist, muMaxDist, finalLeftHorizontalFOV,
                            finalRightHorizontalFOV, finalBottomVerticalFOV, finalTopVerticalFOV, ipFeature);

                        if (double.IsNaN(z1))
                        {
                            System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.RLOSPointsOutsideOfSurfaceExtent, VisibilityLibrary.Properties.Resources.MsgCalcCancelled);
                            return;
                        }

                        //Create shape 
                        IPoint point = new PointClass() { Z = z1, X = observerPoint.Point.X, Y = observerPoint.Point.Y, ZAware = true };
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

                        if (string.IsNullOrEmpty(layerPath))
                        {
                            // if layer path didn't resolve, issue error and stop
                            System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgSurfaceLayerNotFound, VisibilityLibrary.Properties.Resources.AEInvalidInput);
                            throw new Exception(VisibilityLibrary.Properties.Resources.MsgSurfaceLayerNotFound);
                        }

                        IFeatureLayer ipFeatureLayer = new FeatureLayerClass();
                        ipFeatureLayer.FeatureClass = pointFc;

                        IDataset ipDataset = (IDataset)pointFc;
                        string outputFcName = ipDataset.BrowseName + "_output";
                        string strPath = ipDataset.Workspace.PathName + System.IO.Path.DirectorySeparatorChar + ipDataset.BrowseName;
                        string outPath = ipDataset.Workspace.PathName + System.IO.Path.DirectorySeparatorChar + outputFcName;

                        IVariantArray parameters = new VarArrayClass();
                        parameters.Add(layerPath);
                        parameters.Add(strPath);
                        parameters.Add(outPath);

                        IGeoProcessor2 gp = new GeoProcessorClass();

                        gp.AddOutputsToMap = false;

                        // Add a mask to buffer the output to selected distance
                        SetGPMask(workspace, maxRangeBufferGeomList, gp, "radiusMask");

                        object oResult = gp.Execute("Visibility_sa", parameters, null);
                        IGeoProcessorResult ipResult = (IGeoProcessorResult)oResult;

                        ComReleaser.ReleaseCOMObject(gp);
                        gp = null;
                        GC.Collect();

                        // Add the range fan geometries to the map
                        foreach (IGeometry geom in rangeFanGeomList)
                        {
                            var color = new RgbColorClass() { Blue = 255 } as IColor;
                            AddGraphicToMap(geom, color, true);
                        }

                        IRasterLayer outputRasterLayer = new RasterLayerClass();
                        outputRasterLayer.CreateFromFilePath(outPath);

                        string fcName = IntersectOutput(outputRasterLayer, ipDataset, workspace, rangeFanGeomList);

                        IFeatureClass finalFc = workspace.OpenFeatureClass(fcName);

                        IFeatureLayer outputFeatureLayer = new FeatureLayerClass();
                        outputFeatureLayer.FeatureClass = finalFc;

                        //Add it to a map if the layer is valid.
                        if (outputFeatureLayer != null)
                        {
                            // set the renderer
                            IFeatureRenderer featRend = UniqueValueRenderer(workspace, finalFc);
                            IGeoFeatureLayer geoLayer = (IGeoFeatureLayer)outputFeatureLayer;
                            geoLayer.Renderer = featRend;
                            geoLayer.Name = "RLOS_Visibility_" + RunCount.ToString();

                            // Set the layer transparency
                            IDisplayFilterManager filterManager = (IDisplayFilterManager)outputFeatureLayer;
                            ITransparencyDisplayFilter filter = new TransparencyDisplayFilter();
                            filter.Transparency = 80;
                            filterManager.DisplayFilter = filter;

                            ESRI.ArcGIS.Carto.IMap map = ArcMap.Document.FocusMap;
                            map.AddLayer((ILayer)outputFeatureLayer);

                            IEnvelope envelope = outputFeatureLayer.AreaOfInterest.Envelope;
                            ZoomToExtent(envelope);
                        }

                        RunCount += 1;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgTryAgain, VisibilityLibrary.Properties.Resources.MsgCalcCancelled);
                    }
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgTryAgain, VisibilityLibrary.Properties.Resources.MsgCalcCancelled);
            }
            finally
            {
                IsRunning = false;
            }
        }

        #endregion

        #region public

        public IFeatureRenderer UniqueValueRenderer(IFeatureWorkspace workspace, IFeatureClass fc)
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

                ISimpleLineSymbol outlineSymbol = new SimpleLineSymbolClass();
                outlineSymbol.Color = new RgbColorClass() { NullColor = true } as IColor;
                outlineSymbol.Style = esriSimpleLineStyle.esriSLSSolid;

                if (ShowNonVisibleData == true)
                {                  
                    fillSymbol.Color = new RgbColorClass() { Red = 255 } as IColor;
                    fillSymbol.Outline = outlineSymbol;
                    uvRenderer.AddValue("0", "", fillSymbol as ISymbol);
                    uvRenderer.set_Label("0", "Non-Visible");    
                }
                    fillSymbol2.Color = new RgbColorClass() { Green = 255 } as IColor;
                    fillSymbol2.Outline = outlineSymbol;
                    uvRenderer.AddValue("1", "", fillSymbol2 as ISymbol);
                    uvRenderer.set_Label("1", "Visible by 1 Observer");

                    int field = ipTable.FindField("gridcode");
                    uvRenderer.set_Field(0, "gridcode");

                    for (int i = 2; i < uniqueValues; i++)
                    {
                        ISimpleFillSymbol newFillSymbol = new SimpleFillSymbolClass();
                        newFillSymbol.Color = colorRamp.get_Color(i);
                        newFillSymbol.Outline = outlineSymbol;
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
        /// Checks to see if spatial analyst license available (it has been checked out by ArcMap)
        /// </summary>
        /// <returns>true/false</returns>
        public bool IsSpatialAnalystAvailable()
        {
            IAoInitialize aoInitializer = new AoInitializeClass();

            return aoInitializer.IsExtensionCheckedOut(esriLicenseExtensionCode.esriLicenseExtensionCodeSpatialAnalyst);
        }

        /// <summary>
        /// Start Editing operation
        /// </summary>
        /// <param name="ipWorkspace">IWorkspace</param>
        public static bool StartEditOperation(IWorkspace ipWorkspace)
        {
            bool blnWasSuccessful = false;
            IWorkspaceEdit ipWsEdit = ipWorkspace as IWorkspaceEdit;

            if (ipWsEdit != null)
            {
                try
                {
                    ipWsEdit.StartEditOperation();
                    blnWasSuccessful = true;
                }
                catch (Exception ex)
                {
                    ipWsEdit.AbortEditOperation();
                    throw (ex);
                }
            }
            
            return blnWasSuccessful;
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
            if (ipWsEdit != null)
            {
                try
                {
                    ipWsEdit.StopEditOperation();
                    blnWasSuccessful = true;
                }
                catch (Exception ex)
                {
                    ipWsEdit.AbortEditOperation();
                    throw (ex);
                }
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
                throw new Exception(VisibilityLibrary.Properties.Resources.ExceptionFieldIndexNotFound);
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
        /// <param name="spatialRef">Spatial Reference of the surface</param>
        /// <param name="name"></param> 
        /// <returns></returns> 
        public static IFeatureClass CreateObserversFeatureClass(IFeatureWorkspace featWorkspace, ISpatialReference spatialRef, string name)
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
            pGeoDef.SpatialReference_2 = spatialRef;
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
            string outRasterToPolyPath = ipDataset.Workspace.PathName + System.IO.Path.DirectorySeparatorChar + outRasterToPolyFcName;
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
                    string outPath = ipDataset.Workspace.PathName + System.IO.Path.DirectorySeparatorChar + outFcName;

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
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return null;
            }

        }

        /// <summary>
        /// Set a mask on the gp object to buffer the output to the specified distance
        /// </summary>
        /// <param name="workspace">IFeatureWorkspace</param>
        /// <param name="geomList">List of geometries to create the mask</param>
        /// <param name="gp">IGeoProcessor2</param>
        /// <param name="fcName">Name of feature class</param>
        private string SetGPMask(IFeatureWorkspace workspace, List<IGeometry> geomList, IGeoProcessor2 gp, string fcName)
        {
            IFeatureClass maskFc = CreateMaskFeatureClass(workspace, SelectedSurfaceSpatialRef, fcName + "_" + RunCount.ToString());

            foreach (IGeometry geom in geomList)
            {
                //create a new point feature
                IFeature ipFeature = maskFc.CreateFeature();
                ipFeature.Shape = geom;
                ipFeature.Store();
            }
            IDataset ds = (IDataset)maskFc;
            string path = ds.Workspace.PathName + System.IO.Path.DirectorySeparatorChar + ds.BrowseName;
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
            if (SelectedSurfaceSpatialRef == null)
                return inputDistance;

            DistanceTypes distanceTo = DistanceTypes.Meters; // default to meters

            var pcs = SelectedSurfaceSpatialRef as IProjectedCoordinateSystem;

            if (pcs != null)
            {
                // need to convert the offset from the input distance type to the spatial reference linear type
                distanceTo = GetDistanceType(pcs.CoordinateUnit.FactoryCode);
            }
            else
            {
                var gcs = SelectedSurfaceSpatialRef as IGeographicCoordinateSystem;
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
        /// <param name="spatialRef">ISpatialReference of selected surface</param>
        /// <param name="name">Name of the feature class</param>
        /// <returns>IFeatureClass</returns>
        private static IFeatureClass CreateMaskFeatureClass(IFeatureWorkspace featWorkspace, ISpatialReference spatialRef, string name)
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
            pGeoDef.SpatialReference_2 = spatialRef;

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
                String name= ipAngularUnit.Name;
                dConversionFactor = ipAngularUnit.ConversionFactor;
            }
            else
            {
                ILinearUnit ipLinearUnit = ((IProjectedCoordinateSystem)ipSpatialReference).CoordinateUnit;
                String name = ipLinearUnit.Name;
                dConversionFactor = ipLinearUnit.ConversionFactor;
            }
            return dConversionFactor;
        }

        private static String GetUnitString(ISpatialReference ipSpatialReference)
        {
            String name = "";
            if (ipSpatialReference is IGeographicCoordinateSystem)
            {
                IAngularUnit ipAngularUnit = ((IGeographicCoordinateSystem)ipSpatialReference).CoordinateUnit;
                name = ipAngularUnit.Name;
                
            }
            else
            {
                ILinearUnit ipLinearUnit = ((IProjectedCoordinateSystem)ipSpatialReference).CoordinateUnit;
                name = ipLinearUnit.Name;
                
            }
            return name;
        }

        private static DistanceTypes GetMTUnitFromEsriUnit(String esriUnit)
        {
            DistanceTypes outUnit = DistanceTypes.Meters; ;
            switch(esriUnit)
            {
                
                case "Foot_US":
                case "Foot":
                    outUnit = DistanceTypes.Feet;
                    break;
                case "Kilometer":
                    outUnit = DistanceTypes.Kilometers;
                    break;
                case "Meter":
                    outUnit = DistanceTypes.Meters;
                    break;
                case "Mile_US":
                case "Mile_Statute":
                    outUnit = DistanceTypes.Miles;
                    break;
                case "Nautical_Mile":
                    outUnit = DistanceTypes.NauticalMile;
                    break;
                case "Yard":
                case "Yard_US":
                    outUnit = DistanceTypes.Yards;
                    break;
            }
            return outUnit;
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
            if (SelectedSurfaceSpatialRef == null)
                return inputDistance;

            AngularTypes angularDistanceTo = AngularTypes.DEGREES; // default to degrees

            var gcs = SelectedSurfaceSpatialRef as IGeographicCoordinateSystem;

            if (gcs != null)
            {
                angularDistanceTo = GetAngularType(gcs.CoordinateUnit.FactoryCode);
            }

            var result = GetAngularDistanceFromTo(angularType, angularDistanceTo, inputDistance);

            return result;
        }

        // TODO: Move repeated method GetAngularDistanceFromTo to common visibility library
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
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return Math.Round(angularDistance, 1);
        }

        /// <summary>
        /// Return the layer file path of the provided layer
        /// </summary>
        /// <param name="layer">ILayer</param>
        /// <returns>file path of layer</returns>
        private static string GetLayerPath(ILayer layer)
        {
            if (layer is IImageServerLayer)
            {
                IImageServerLayer mlayer = layer as IImageServerLayer;
                return mlayer.Name;
            } 
            else if (layer is IRasterLayer)
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
