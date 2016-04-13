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

using System;
using System.Collections.Generic;
using System.Linq;
using VisibilityLibrary.Helpers;
using VisibilityLibrary;
using VisibilityLibrary.ViewModels;
using ArcGIS.Desktop.Mapping;
using ProAppVisibilityModule.Models;
using ArcGIS.Desktop.Framework;
using ArcGIS.Core.Geometry;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace ProAppVisibilityModule.ViewModels
{
    /// <summary>
    /// Base class for all the common properties, commands and events for tab items
    /// </summary>
    public class ProTabBaseViewModel : BaseViewModel
    {
        public ProTabBaseViewModel()
        {
            //properties
            //LineType = LineTypes.Geodesic;
            LineDistanceType = DistanceTypes.Meters;

            //commands
            ClearGraphicsCommand = new VisibilityLibrary.Helpers.RelayCommand(OnClearGraphics);
            //ActivateToolCommand = new RelayCommand(OnActivateTool);
            EnterKeyCommand = new VisibilityLibrary.Helpers.RelayCommand(OnEnterKeyCommand);
            CancelCommand = new VisibilityLibrary.Helpers.RelayCommand(OnCancelCommand);

            // Mediator
            Mediator.Register(VisibilityLibrary.Constants.NEW_MAP_POINT, OnNewMapPointEvent);
            Mediator.Register(VisibilityLibrary.Constants.MOUSE_MOVE_POINT, OnMouseMoveEvent);
            Mediator.Register(VisibilityLibrary.Constants.TAB_ITEM_SELECTED, OnTabItemSelected);

            ActivateToolCommand = new VisibilityLibrary.Helpers.RelayCommand(OnActivateTool);

        }

        //private System.Threading.Tasks.Task OnActivateTool()
        //{
        //    await FrameworkApplication.SetCurrentToolAsync("ProAppVisibilityModule_MapTool");
        //}

        internal virtual void OnActivateTool(object obj)
        { 
            FrameworkApplication.SetCurrentToolAsync("ProAppVisibilityModule_MapTool");
        }

        #region Properties

        // lists to store GUIDs of graphics, temp feedback and map graphics
        private static List<string> TempGraphicsList = new List<string>();
        private static List<string> MapGraphicsList = new List<string>();

        //internal bool HasPoint1 = false;
        //internal bool HasPoint2 = false;
        //internal INewLineFeedback feedback = null;

        private List<IDisposable> overlayObjects = new List<IDisposable>();
        // lists to store GUIDs of graphics, temp feedback and map graphics
        private static List<ProGraphic> GraphicsList = new List<ProGraphic>();

        public bool HasMapGraphics
        {
            get
            {
                return MapGraphicsList.Any();
            }
        }

        //private IPoint point1 = null;
        ///// <summary>
        ///// Property for the first IPoint
        ///// </summary>
        //public virtual IPoint Point1
        //{
        //    get
        //    {
        //        return point1;
        //    }
        //    set
        //    {
        //        // do not add anything to the map from here
        //        point1 = value;
        //        RaisePropertyChanged(() => Point1);
        //        RaisePropertyChanged(() => Point1Formatted);
        //    }
        //}

        //private IPoint point2 = null;
        ///// <summary>
        ///// Property for the second IPoint
        ///// Not all tools need a second point
        ///// </summary>
        //public virtual IPoint Point2
        //{
        //    get
        //    {
        //        return point2;
        //    }
        //    set
        //    {
        //        point2 = value;
        //        RaisePropertyChanged(() => Point2);
        //        RaisePropertyChanged(() => Point2Formatted);
        //    }
        //}
        //string point1Formatted = string.Empty;
        ///// <summary>
        ///// String property for the first IPoint
        ///// This is used to format the point for the UI and allow string input of different types of coordinates
        ///// </summary>
        //public string Point1Formatted
        //{
        //    get
        //    {
        //        // return a formatted first point depending on how it was entered, manually or via map point tool
        //        if (string.IsNullOrWhiteSpace(point1Formatted))
        //        {
        //            if (Point1 == null)
        //                return string.Empty;

        //            // only format if the Point1 data was generated from a mouse click
        //            return string.Format("{0:0.0#####} {1:0.0#####}", Point1.Y, Point1.X);
        //        }
        //        else
        //        {
        //            // this was user inputed so just return the inputed string
        //            return point1Formatted;
        //        }
        //    }

        //    set
        //    {
        //        if (string.IsNullOrWhiteSpace(value))
        //        {
        //            point1Formatted = string.Empty;
        //            RaisePropertyChanged(() => Point1Formatted);
        //            return;
        //        }
        //        // try to convert string to an IPoint
        //        var point = GetPointFromString(value);
        //        if (point != null)
        //        {
        //            // clear temp graphics
        //            ClearTempGraphics();
        //            point1Formatted = value;
        //            HasPoint1 = true;
        //            Point1 = point;
        //            AddGraphicToMap(Point1, true);
        //            // lets try feedback
        //            var mxdoc = ArcMap.Application.Document as IMxDocument;
        //            var av = mxdoc.FocusMap as IActiveView;
        //            point.Project(mxdoc.FocusMap.SpatialReference);
        //            CreateFeedback(point, av);
        //            feedback.Start(point);
        //            if (Point2 != null)
        //            {
        //                UpdateDistance(GetPolylineFromFeedback(Point1, Point2));
        //                FeedbackMoveTo(Point2);
        //            }
        //        }
        //        else
        //        {
        //            // invalid coordinate, reset and throw exception
        //            Point1 = null;
        //            HasPoint1 = false;
        //            throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEInvalidCoordinate);
        //        }
        //    }
        //}

        //string point2Formatted = string.Empty;
        ///// <summary>
        ///// String property for the second IPoint
        ///// This is used to format the point for the UI and allow string input of different types of coordinates
        ///// Input types like GARS, MGRS, USNG, UTM
        ///// </summary>
        //public string Point2Formatted
        //{
        //    get
        //    {
        //        // return a formatted second point depending on how it was entered, manually or via map point tool
        //        if (string.IsNullOrWhiteSpace(point2Formatted))
        //        {
        //            if (Point2 == null)
        //                return string.Empty;

        //            // only format if the Point2 data was generated from a mouse click
        //            return string.Format("{0:0.0#####} {1:0.0#####}", Point2.Y, Point2.X);
        //        }
        //        else
        //        {
        //            // this was user inputed so just return the inputed string
        //            return point2Formatted;
        //        }
        //    }
        //    set
        //    {
        //        if (string.IsNullOrWhiteSpace(value))
        //        {
        //            point2Formatted = string.Empty;
        //            RaisePropertyChanged(() => Point2Formatted);
        //            return;
        //        }
        //        // try to convert string to an IPoint
        //        var point = GetPointFromString(value);
        //        if (point != null)
        //        {
        //            point2Formatted = value;
        //            //HasPoint2 = true;
        //            Point2 = point;
        //            var mxdoc = ArcMap.Application.Document as IMxDocument;
        //            var av = mxdoc.FocusMap as IActiveView;
        //            Point2.Project(mxdoc.FocusMap.SpatialReference);

        //            //if (feedback != null)
        //            //{
        //            //    // I have to create a new point here, otherwise "MoveTo" will change the spatial reference to world mercator
        //            //    FeedbackMoveTo(point);
        //            //}
        //            if (HasPoint1)
        //            {
        //                // lets try feedback
        //                CreateFeedback(Point1, av);
        //                feedback.Start(Point1);
        //                UpdateDistance(GetPolylineFromFeedback(Point1, Point2));
        //                // I have to create a new point here, otherwise "MoveTo" will change the spatial reference to world mercator
        //                FeedbackMoveTo(point);
        //            }

        //        }
        //        else
        //        {
        //            // invalid coordinate, reset and throw exception
        //            Point2 = null;
        //            HasPoint2 = false;
        //            throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEInvalidCoordinate);
        //        }
        //    }
        //}

        private bool isActiveTab = false;
        /// <summary>
        /// Property to keep track of which tab/viewmodel is the active item
        /// </summary>
        public bool IsActiveTab
        {
            get
            {
                return isActiveTab;
            }
            set
            {
                Reset(true);
                isActiveTab = value;
                RaisePropertyChanged(() => IsActiveTab);
            }
        }

        DistanceTypes lineDistanceType = DistanceTypes.Meters;
        /// <summary>
        /// Property for the distance type
        /// </summary>
        public DistanceTypes LineDistanceType
        {
            get { return lineDistanceType; }
            set
            {
                var before = lineDistanceType;
                lineDistanceType = value;
                //UpdateDistanceFromTo(before, value);
                //TODO update for Pro
            }
        }

        //double distance = 0.0;
        ///// <summary>
        ///// Property for the distance/length
        ///// </summary>
        //public virtual double Distance
        //{
        //    get { return distance; }
        //    set
        //    {
        //        if (value < 0.0)
        //            throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEMustBePositive);

        //        distance = value;
        //        DistanceString = distance.ToString("N"); // use current culture number format
        //        RaisePropertyChanged(() => Distance);
        //        RaisePropertyChanged(() => DistanceString);
        //    }
        //}

        //string distanceString = String.Empty;
        ///// <summary>
        ///// Distance property as a string
        ///// </summary>
        //public virtual string DistanceString
        //{
        //    get
        //    {
        //        return Distance.ToString("N"); // use current culture number format
        //    }
        //    set
        //    {
        //        // lets avoid an infinite loop here
        //        if (string.Equals(distanceString, value))
        //            return;

        //        distanceString = value;

        //        // update distance
        //        double d = 0.0;
        //        if (double.TryParse(distanceString, out d))
        //        {
        //            Distance = d;
        //        }
        //        else
        //        {
        //            throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEInvalidInput);
        //        }
        //    }
        //}

        /// <summary>
        /// Property for the type of geodesy line
        /// </summary>
//        public LineTypes LineType { get; set; }

        /// <summary>
        /// Property used to test if there is enough info to create a line map element
        /// </summary>
        public virtual bool CanCreateElement
        {
            get
            {
                //TODO update for Pro
                return true; //(Point1 != null && Point2 != null);
            }
        }

        #endregion

        #region Commands

        public VisibilityLibrary.Helpers.RelayCommand ClearGraphicsCommand { get; set; }
        //public RelayCommand ActivateToolCommand { get; set; }
        public VisibilityLibrary.Helpers.RelayCommand EnterKeyCommand { get; set; }
        public VisibilityLibrary.Helpers.RelayCommand CancelCommand { get; set; }
        public VisibilityLibrary.Helpers.RelayCommand ActivateToolCommand { get; set; }

        private void OnCancelCommand(object obj)
        {
            Reset(true);
        }

        #endregion

        /// <summary>
        /// Method is called when a user pressed the "Enter" key or when a second point is created for a line from mouse clicks
        /// Derived class must override this method in order to create map elements
        /// Clears temp graphics by default
        /// </summary>
        internal virtual void CreateMapElement()
        {
            ClearTempGraphics();
        }
        #region Private Event Functions

        /// <summary>
        /// Clears all the graphics from the maps graphic container
        /// Inlucdes temp and map graphics
        /// Only removes temp and map graphics that were created by this add-in
        /// </summary>
        /// <param name="obj"></param>
        private void OnClearGraphics(object obj)
        {
            if (MapView.Active == null)
                return;

            foreach(var item in GraphicsList)
            {
                item.Disposable.Dispose();
            }

            GraphicsList.Clear();

            RaisePropertyChanged(() => HasMapGraphics);
        }

        /// <summary>
        /// Method to clear all temp graphics
        /// </summary>
        internal void ClearTempGraphics()
        {
            var list = GraphicsList.Where(g => g.IsTemp == true).ToList();

            foreach (var item in list)
            {
                item.Disposable.Dispose();
                GraphicsList.Remove(item);
            }

            RaisePropertyChanged(() => HasMapGraphics);
        }

        /// <summary>
        /// Method used to move temp graphics to map graphics
        /// Tools use this to make temp graphics permanent on completion
        /// otherwise temp graphics get cleared on reset/cancel
        /// </summary>
        //internal void MoveTempGraphicsToMapGraphics()
        //{
        //    MapGraphicsList.AddRange(TempGraphicsList);
        //    TempGraphicsList.Clear();
        //    RaisePropertyChanged(() => HasMapGraphics);
        //}

        /// <summary>
        /// Handler for the "Enter"key command
        /// Calls CreateMapElement
        /// </summary>
        /// <param name="obj"></param>
        internal virtual void OnEnterKeyCommand(object obj)
        {
            if (!CanCreateElement)
                return;

            CreateMapElement();
        }
        /// <summary>
        /// Handler for the new map point click event
        /// </summary>
        /// <param name="obj">IPoint</param>
        internal virtual void OnNewMapPointEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            //var mxdoc = ArcMap.Application.Document as IMxDocument;
            //var av = mxdoc.FocusMap as IActiveView;
            //var point = obj as IPoint;

            //if (point == null)
            //    return;

            //if (!HasPoint1)
            //{
            //    // clear temp graphics
            //    ClearTempGraphics();
            //    Point1 = point;
            //    HasPoint1 = true;
            //    Point1Formatted = string.Empty;

            //    AddGraphicToMap(Point1, true);

            //    // lets try feedback
            //    CreateFeedback(point, av);
            //    feedback.Start(point);
            //}
            //else if (!HasPoint2)
            //{
            //    ResetFeedback();
            //    Point2 = point;
            //    HasPoint2 = true;
            //    point2Formatted = string.Empty;
            //    RaisePropertyChanged(() => Point2Formatted);
            //}

            //if (HasPoint1 && HasPoint2)
            //{
            //    CreateMapElement();
            //    ResetPoints();
            //}
        }

        #endregion
        #region Public Functions
        /// <summary>
        /// Method used to deactivate tool
        /// </summary>
        public void DeactivateTool(string toolname)
        {
            if (FrameworkApplication.CurrentTool != null &&
                FrameworkApplication.CurrentTool.Equals(toolname))
            {
                FrameworkApplication.SetCurrentToolAsync(String.Empty);
            }
        }
        #endregion
        #region Private Functions
        /// <summary>
        /// Method used to totally reset the tool
        /// reset points, feedback
        /// clear out textboxes
        /// </summary>
        internal virtual void Reset(bool toolReset)
        {
            if (toolReset)
            {
                DeactivateTool("Esri_ArcMapAddinVisibility_MapPointTool");
            }

            ResetPoints();
            //Point1 = null;
            //Point2 = null;
            //Point1Formatted = string.Empty;
            //Point2Formatted = string.Empty;

            //Distance = 0.0;
        }
        /// <summary>
        /// Resets Points 1 and 2
        /// </summary>
        internal virtual void ResetPoints()
        {
            //HasPoint1 = HasPoint2 = false;
        }

        /// <summary>
        /// Handler for the tab item selected event
        /// Helps keep track of which tab item/viewmodel is active
        /// </summary>
        /// <param name="obj">bool if selected or not</param>
        private void OnTabItemSelected(object obj)
        {
            if (obj == null)
                return;

            IsActiveTab = (obj == this);
        }

        //internal string AddTextToMap(string text, IGeometry geom, IColor color, bool IsTempGraphic = false, int size = 12)
        //{
        //    if (geom == null || ArcMap.Document == null || ArcMap.Document.FocusMap == null)
        //        return string.Empty;

        //    IElement element = null;

        //    geom.Project(ArcMap.Document.FocusMap.SpatialReference);

        //    if (geom.GeometryType == esriGeometryType.esriGeometryPoint)
        //    {
        //        var te = new TextElementClass() as ITextElement;
        //        te.Text = text;

        //        var ts = new TextSymbolClass();
        //        ts.Size = size;
        //        ts.VerticalAlignment = esriTextVerticalAlignment.esriTVACenter;
        //        ts.HorizontalAlignment = esriTextHorizontalAlignment.esriTHACenter;

        //        te.Symbol = ts;

        //        element = te as IElement;
        //    }

        //    if (element == null)
        //        return string.Empty;

        //    element.Geometry = geom;

        //    var mxdoc = ArcMap.Application.Document as IMxDocument;
        //    var av = mxdoc.FocusMap as IActiveView;
        //    var gc = av as IGraphicsContainer;

        //    // store guid
        //    var eprop = element as IElementProperties;
        //    eprop.Name = Guid.NewGuid().ToString();

        //    if (IsTempGraphic)
        //        TempGraphicsList.Add(eprop.Name);
        //    else
        //        MapGraphicsList.Add(eprop.Name);

        //    gc.AddElement(element, 0);

        //    av.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);

        //    RaisePropertyChanged(() => HasMapGraphics);

        //    return eprop.Name;
        //}


        internal async void AddGraphicToMap(Geometry geom, bool IsTempGraphic = false, double size = 1.0)
        {
            // default color Red
            await AddGraphicToMap(geom, ColorFactory.Red, IsTempGraphic, size);
        }

        internal async Task AddGraphicToMap(Geometry geom, CIMColor color, bool IsTempGraphic = false, double size = 1.0, string text = "")
        {
            if (geom == null || MapView.Active == null)
                return;

            CIMSymbolReference symbol = null;

            if(!string.IsNullOrWhiteSpace(text) && geom.GeometryType == GeometryType.Point)
            {
                await QueuedTask.Run(() =>
                    {
                        //var tg = new CIMTextGraphic() { Placement = Anchor.CenterPoint, Text = text};
                    });
            }
            else if (geom.GeometryType == GeometryType.Point)
            {
                await QueuedTask.Run(() =>
                {
                    var s = SymbolFactory.ConstructPointSymbol(color, size, SimpleMarkerStyle.Circle);
                    symbol = new CIMSymbolReference() { Symbol = s };
                });
            }
            else if (geom.GeometryType == GeometryType.Polyline)
            {
                await QueuedTask.Run(() =>
                {
                    var s = SymbolFactory.ConstructLineSymbol(color, size);
                    symbol = new CIMSymbolReference() { Symbol = s };
                });
            }
            else if (geom.GeometryType == GeometryType.Polygon)
            {
                await QueuedTask.Run(() =>
                {
                    var outline = SymbolFactory.ConstructStroke(ColorFactory.Black, 1.0, SimpleLineStyle.Solid);
                    var s = SymbolFactory.ConstructPolygonSymbol(color, SimpleFillStyle.Solid, outline);
                    symbol = new CIMSymbolReference() { Symbol = s };
                });
            }

            await QueuedTask.Run(() =>
            {
                var disposable = MapView.Active.AddOverlay(geom, symbol);
                overlayObjects.Add(disposable);

                GraphicsList.Add(new ProGraphic(disposable, geom, IsTempGraphic));
            });
        }


        //internal DistanceTypes GetDistanceType(int linearUnitFactoryCode)
        //{ 
        //    DistanceTypes distanceType = DistanceTypes.Meters;
        //    switch (linearUnitFactoryCode)
        //    {
        //        case (int)esriSRUnitType.esriSRUnit_Foot:
        //            distanceType = DistanceTypes.Feet;
        //            break;
        //        case (int)esriSRUnitType.esriSRUnit_Kilometer:
        //            distanceType = DistanceTypes.Kilometers;
        //            break;
        //        case (int)esriSRUnitType.esriSRUnit_Meter:
        //            distanceType = DistanceTypes.Meters;
        //            break;
        //        case (int)esriSRUnitType.esriSRUnit_NauticalMile:
        //            distanceType = DistanceTypes.NauticalMile;
        //            break;
        //        case (int)esriSRUnitType.esriSRUnit_SurveyFoot:
        //            distanceType = DistanceTypes.SurveyFoot;
        //            break;
        //        default:
        //            distanceType = DistanceTypes.Meters;
        //            break;
        //    }

        //    return distanceType;
        //}

        //internal ISpatialReferenceFactory3 srf3 = null;
        //internal ILinearUnit GetLinearUnit()
        //{
        //    return GetLinearUnit(LineDistanceType);
        //}
        /// <summary>
        /// Gets the linear unit from the esri constants for linear units
        /// </summary>
        /// <returns>ILinearUnit</returns>
        //internal ILinearUnit GetLinearUnit(DistanceTypes distanceType)
        //{
        //    int unitType = (int)esriSRUnitType.esriSRUnit_Meter;
        //    if (srf3 == null)
        //    {
        //        Type srType = Type.GetTypeFromProgID("esriGeometry.SpatialReferenceEnvironment");
        //        srf3 = Activator.CreateInstance(srType) as ISpatialReferenceFactory3;
        //    }

        //    switch (distanceType)
        //    {
        //        case DistanceTypes.Feet:
        //            unitType = (int)esriSRUnitType.esriSRUnit_Foot;
        //            break;
        //        case DistanceTypes.Kilometers:
        //            unitType = (int)esriSRUnitType.esriSRUnit_Kilometer;
        //            break;
        //        case DistanceTypes.Meters:
        //            unitType = (int)esriSRUnitType.esriSRUnit_Meter;
        //            break;
        //        case DistanceTypes.NauticalMile:
        //            unitType = (int)esriSRUnitType.esriSRUnit_NauticalMile;
        //            break;
        //        case DistanceTypes.SurveyFoot:
        //            unitType = (int)esriSRUnitType.esriSRUnit_SurveyFoot;
        //            break;
        //        default:
        //            unitType = (int)esriSRUnitType.esriSRUnit_Meter;
        //            break;
        //    }

        //    return srf3.CreateUnit(unitType) as ILinearUnit;
        //}

        //private void UpdateDistanceFromTo(DistanceTypes fromType, DistanceTypes toType)
        //{
        //    Distance = GetDistanceFromTo(fromType, toType, Distance);
        //}

        /// <summary>
        /// Ugly method to convert to/from different types of distance units
        /// </summary>
        /// <param name="fromType">DistanceTypes</param>
        /// <param name="toType">DistanceTypes</param>
        internal double GetDistanceFromTo(DistanceTypes fromType, DistanceTypes toType, double input)
        {
            double length = input;

            try
            {
                if (fromType == DistanceTypes.Meters && toType == DistanceTypes.Kilometers)
                    length /= 1000.0;
                else if (fromType == DistanceTypes.Meters && toType == DistanceTypes.Feet)
                    length *= 3.28084;
                else if (fromType == DistanceTypes.Meters && toType == DistanceTypes.SurveyFoot)
                    length *= 3.280833333;
                else if (fromType == DistanceTypes.Meters && toType == DistanceTypes.NauticalMile)
                    length *= 0.000539957;
                else if (fromType == DistanceTypes.Kilometers && toType == DistanceTypes.Meters)
                    length *= 1000.0;
                else if (fromType == DistanceTypes.Kilometers && toType == DistanceTypes.Feet)
                    length *= 3280.84;
                else if (fromType == DistanceTypes.Kilometers && toType == DistanceTypes.SurveyFoot)
                    length *= 3280.833333;
                else if (fromType == DistanceTypes.Kilometers && toType == DistanceTypes.NauticalMile)
                    length *= 0.539957;
                else if (fromType == DistanceTypes.Feet && toType == DistanceTypes.Kilometers)
                    length *= 0.0003048;
                else if (fromType == DistanceTypes.Feet && toType == DistanceTypes.Meters)
                    length *= 0.3048;
                else if (fromType == DistanceTypes.Feet && toType == DistanceTypes.SurveyFoot)
                    length *= 0.999998000004;
                else if (fromType == DistanceTypes.Feet && toType == DistanceTypes.NauticalMile)
                    length *= 0.000164579;
                else if (fromType == DistanceTypes.SurveyFoot && toType == DistanceTypes.Kilometers)
                    length *= 0.0003048006096;
                else if (fromType == DistanceTypes.SurveyFoot && toType == DistanceTypes.Meters)
                    length *= 0.3048006096;
                else if (fromType == DistanceTypes.SurveyFoot && toType == DistanceTypes.Feet)
                    length *= 1.000002;
                else if (fromType == DistanceTypes.SurveyFoot && toType == DistanceTypes.NauticalMile)
                    length *= 0.00016457916285097;
                else if (fromType == DistanceTypes.NauticalMile && toType == DistanceTypes.Kilometers)
                    length *= 1.852001376036;
                else if (fromType == DistanceTypes.NauticalMile && toType == DistanceTypes.Meters)
                    length *= 1852.001376036;
                else if (fromType == DistanceTypes.NauticalMile && toType == DistanceTypes.Feet)
                    length *= 6076.1154855643;
                else if (fromType == DistanceTypes.NauticalMile && toType == DistanceTypes.SurveyFoot)
                    length *= 6076.1033333576;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return length;
        }

        /// <summary>
        /// Get the currently selected geodetic type
        /// </summary>
        /// <returns>esriGeodeticType</returns>
        //internal esriGeodeticType GetEsriGeodeticType()
        //{
        //    esriGeodeticType type = esriGeodeticType.esriGeodeticTypeGeodesic;

        //    switch (LineType)
        //    {
        //        case LineTypes.Geodesic:
        //            type = esriGeodeticType.esriGeodeticTypeGeodesic;
        //            break;
        //        case LineTypes.GreatElliptic:
        //            type = esriGeodeticType.esriGeodeticTypeGreatElliptic;
        //            break;
        //        case LineTypes.Loxodrome:
        //            type = esriGeodeticType.esriGeodeticTypeLoxodrome;
        //            break;
        //        default:
        //            type = esriGeodeticType.esriGeodeticTypeGeodesic;
        //            break;
        //    }

        //    return type;
        //}
        //internal double GetGeodeticLengthFromPolyline(IPolyline polyline)
        //{
        //    if (polyline == null)
        //        return 0.0;

        //    var polycurvegeo = polyline as IPolycurveGeodetic;

        //    var geodeticType = GetEsriGeodeticType();
        //    var linearUnit = GetLinearUnit();
        //    var geodeticLength = polycurvegeo.get_LengthGeodetic(geodeticType, linearUnit);

        //    return geodeticLength;
        //}
        /// <summary>
        /// Gets the distance/lenght of a polyline
        /// </summary>
        /// <param name="geometry">IGeometry</param>
        //internal void UpdateDistance(IGeometry geometry)
        //{
        //    var polyline = geometry as IPolyline;

        //    if (polyline == null)
        //        return;

        //    Distance = GetGeodeticLengthFromPolyline(polyline);
        //}
        /// <summary>
        /// Handler for the mouse move event
        /// When the mouse moves accross the map, IPoints are returned to aid in updating feedback to user
        /// </summary>
        /// <param name="obj">IPoint</param>
        internal virtual void OnMouseMoveEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            var point = obj as MapPoint;

            if (point == null)
                return;

            //TODO add this back in when manual input is added
            // dynamically update start point if not set yet
            //if (!HasPoint1)
            //{
            //    Point1 = point;
            //}
        }

        /// <summary>
        /// Method used to convert a string to a known coordinate
        /// Assumes WGS84 for now
        /// Uses the IConversionNotation interface
        /// </summary>
        /// <param name="coordinate">the coordinate as a string</param>
        /// <returns>IPoint if successful, null if not</returns>
        //internal IPoint GetPointFromString(string coordinate)
        //{
        //    Type t = Type.GetTypeFromProgID("esriGeometry.SpatialReferenceEnvironment");
        //    System.Object obj = Activator.CreateInstance(t);
        //    ISpatialReferenceFactory srFact = obj as ISpatialReferenceFactory;

        //    // Use the enumeration to create an instance of the predefined object.

        //    IGeographicCoordinateSystem geographicCS =
        //        srFact.CreateGeographicCoordinateSystem((int)
        //        esriSRGeoCSType.esriSRGeoCS_WGS1984);

        //    var point = new Point() as IPoint;
        //    point.SpatialReference = geographicCS;
        //    var cn = point as IConversionNotation;

        //    if (cn == null)
        //        return null;

        //    try { cn.PutCoordsFromDD(coordinate); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromDDM(coordinate); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromDMS(coordinate); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromGARS(esriGARSModeEnum.esriGARSModeCENTER, coordinate); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromGARS(esriGARSModeEnum.esriGARSModeLL, coordinate); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromMGRS(coordinate, esriMGRSModeEnum.esriMGRSMode_Automatic); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromMGRS(coordinate, esriMGRSModeEnum.esriMGRSMode_NewStyle); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromMGRS(coordinate, esriMGRSModeEnum.esriMGRSMode_NewWith180InZone01); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromMGRS(coordinate, esriMGRSModeEnum.esriMGRSMode_OldStyle); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromMGRS(coordinate, esriMGRSModeEnum.esriMGRSMode_OldWith180InZone01); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromMGRS(coordinate, esriMGRSModeEnum.esriMGRSMode_USNG); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromUSNG(coordinate); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromUTM(esriUTMConversionOptionsEnum.esriUTMAddSpaces, coordinate); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromUTM(esriUTMConversionOptionsEnum.esriUTMUseNS, coordinate); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromUTM(esriUTMConversionOptionsEnum.esriUTMAddSpaces | esriUTMConversionOptionsEnum.esriUTMUseNS, coordinate); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromUTM(esriUTMConversionOptionsEnum.esriUTMNoOptions, coordinate); return point; }
        //    catch { }
        //    try { cn.PutCoordsFromGeoRef(coordinate); return point; }
        //    catch { }

        //    // lets see if we have a PCS coordinate
        //    // we'll assume the same units as the map units
        //    // get spatial reference of map
        //    if (ArcMap.Document == null || ArcMap.Document.FocusMap == null || ArcMap.Document.FocusMap.SpatialReference == null)
        //        return null;

        //    var map = ArcMap.Document.FocusMap;
        //    var pcs = map.SpatialReference as IProjectedCoordinateSystem;

        //    if (pcs == null)
        //        return null;

        //    point.SpatialReference = map.SpatialReference;
        //    // get pcs coordinate from input
        //    coordinate = coordinate.Trim();

        //    Regex regexMercator = new Regex(@"^(?<latitude>\-?\d+\.?\d*)[+,;:\s]*(?<longitude>\-?\d+\.?\d*)");

        //    var matchMercator = regexMercator.Match(coordinate);

        //    if (matchMercator.Success && matchMercator.Length == coordinate.Length)
        //    {
        //        try
        //        {
        //            var Lat = Double.Parse(matchMercator.Groups["latitude"].Value);
        //            var Lon = Double.Parse(matchMercator.Groups["longitude"].Value);
        //            point.PutCoords(Lon, Lat);
        //            return point;
        //        }
        //        catch
        //        {
        //            return null;
        //        }
        //    }

        //    return null;
        //}
 
        #endregion Private Functions
    }
}
