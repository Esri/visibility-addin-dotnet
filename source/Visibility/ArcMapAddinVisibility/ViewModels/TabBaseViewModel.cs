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
using System.Text.RegularExpressions;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using VisibilityLibrary.Helpers;
using VisibilityLibrary;
using VisibilityLibrary.ViewModels;
using VisibilityLibrary.Models;
using ArcMapAddinVisibility.Models;

namespace ArcMapAddinVisibility.ViewModels
{
    /// <summary>
    /// Base class for all the common properties, commands and events for tab items
    /// </summary>
    public class TabBaseViewModel : BaseViewModel
    {
        public TabBaseViewModel()
        {
            //commands
            ClearGraphicsCommand = new RelayCommand(OnClearGraphics);
            ActivateToolCommand = new RelayCommand(OnActivateTool);
            EnterKeyCommand = new RelayCommand(OnEnterKeyCommand);
            CancelCommand = new RelayCommand(OnCancelCommand);

            // Mediator
            Mediator.Register(Constants.NEW_MAP_POINT, OnNewMapPointEvent);
            Mediator.Register(Constants.MOUSE_MOVE_POINT, OnMouseMoveEvent);
            Mediator.Register(Constants.TAB_ITEM_SELECTED, OnTabItemSelected);
        }

        #region Properties

        // lists to store GUIDs of graphics, temp feedback and map graphics
        private static List<AMGraphic> GraphicsList = new List<AMGraphic>();

        public bool HasMapGraphics
        {
            get
            {
                // only non temp graphics please
                return GraphicsList.Any(g => g.IsTemp == false);
            }
        }

        private IPoint point1 = null;
        /// <summary>
        /// Property for the first IPoint
        /// </summary>
        public virtual IPoint Point1
        {
            get
            {
                return point1;
            }
            set
            {
                // do not add anything to the map from here
                point1 = value;
                RaisePropertyChanged(() => Point1);
                RaisePropertyChanged(() => Point1Formatted);
            }
        }

        private IPoint point2 = null;
        /// <summary>
        /// Property for the second IPoint
        /// Not all tools need a second point
        /// </summary>
        public virtual IPoint Point2
        {
            get
            {
                return point2;
            }
            set
            {
                point2 = value;
                RaisePropertyChanged(() => Point2);
                RaisePropertyChanged(() => Point2Formatted);
            }
        }
        string point1Formatted = string.Empty;
        /// <summary>
        /// String property for the first IPoint
        /// This is used to format the point for the UI and allow string input of different types of coordinates
        /// </summary>
        public string Point1Formatted
        {
            get
            {
                // return a formatted first point depending on how it was entered, manually or via map point tool
                if (string.IsNullOrWhiteSpace(point1Formatted))
                {
                    if (Point1 == null)
                        return string.Empty;

                    // only format if the Point1 data was generated from a mouse click
                    return GetFormattedPoint(Point1);
                }
                else
                {
                    // this was user inputed so just return the inputed string
                    return point1Formatted;
                }
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    point1Formatted = string.Empty;
                    RaisePropertyChanged(() => Point1Formatted);
                    return;
                }
                // try to convert string to an IPoint
                var point = GetPointFromString(value);
                if (point != null)
                {
                    point1Formatted = value;
                    Point1 = point;
                    //AddGraphicToMap(Point1, true);
                    var mxdoc = ArcMap.Application.Document as IMxDocument;
                    point.Project(mxdoc.FocusMap.SpatialReference);
                }
                else
                {
                    // invalid coordinate, reset and throw exception
                    Point1 = null;
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEInvalidCoordinate);
                }
            }
        }

        string point2Formatted = string.Empty;
        /// <summary>
        /// String property for the second IPoint
        /// This is used to format the point for the UI and allow string input of different types of coordinates
        /// Input types like GARS, MGRS, USNG, UTM
        /// </summary>
        public string Point2Formatted
        {
            get
            {
                // return a formatted second point depending on how it was entered, manually or via map point tool
                if (string.IsNullOrWhiteSpace(point2Formatted))
                {
                    if (Point2 == null)
                        return string.Empty;

                    // only format if the Point2 data was generated from a mouse click
                    return GetFormattedPoint(Point2);
                }
                else
                {
                    // this was user inputed so just return the inputed string
                    return point2Formatted;
                }
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    point2Formatted = string.Empty;
                    RaisePropertyChanged(() => Point2Formatted);
                    return;
                }
                // try to convert string to an IPoint
                var point = GetPointFromString(value);
                if (point != null)
                {
                    point2Formatted = value;
                    Point2 = point;
                    //AddGraphicToMap(Point2, true);
                    var mxdoc = ArcMap.Application.Document as IMxDocument;
                    Point2.Project(mxdoc.FocusMap.SpatialReference);
                }
                else
                {
                    // invalid coordinate, reset and throw exception
                    Point2 = null;
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEInvalidCoordinate);
                }
            }
        }

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

        /// <summary>
        /// Property used to test if there is enough info to create a line map element
        /// </summary>
        public virtual bool CanCreateElement
        {
            get
            {
                return (Point1 != null && Point2 != null);
            }
        }

        /// <summary>
        /// Property used to set / get the spatial reference of the selected surface
        /// </summary>
        public ISpatialReference SelectedSurfaceSpatialRef { get; set; }

        #endregion

        #region Commands

        public RelayCommand ClearGraphicsCommand { get; set; }
        public RelayCommand ActivateToolCommand { get; set; }
        public RelayCommand EnterKeyCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }

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
        /// Clears all the graphics from the maps graphic container except temp graphics
        /// Inlucdes map graphics only
        /// Only removes map graphics that were created by this add-in
        /// </summary>
        /// <param name="obj"></param>
        private void OnClearGraphics(object obj)
        {
            var mxdoc = ArcMap.Application.Document as IMxDocument;
            if (mxdoc == null)
                return;
            var av = mxdoc.FocusMap as IActiveView;
            if (av == null)
                return;
            var gc = av as IGraphicsContainer;
            if (gc == null)
                return;

            RemoveGraphics(gc, GraphicsList.Where(g => g.IsTemp == false).ToList());

            //av.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
            av.Refresh(); // sometimes a partial refresh is not working
        }

        /// <summary>
        /// Method to clear all temp graphics
        /// </summary>
        internal void ClearTempGraphics()
        {
            var mxdoc = ArcMap.Application.Document as IMxDocument;
            if (mxdoc == null)
                return;
            var av = mxdoc.FocusMap as IActiveView;
            if (av == null)
                return;
            var gc = av as IGraphicsContainer;
            if (gc == null)
                return;

            RemoveGraphics(gc, GraphicsList.Where(g => g.IsTemp == true).ToList());

            av.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
        }
        /// <summary>
        /// Method used to remove graphics from the graphics container
        /// Elements are tagged with a GUID on the IElementProperties.Name property
        /// </summary>
        /// <param name="gc">map graphics container</param>
        /// <param name="list">list of GUIDs to remove</param>
        internal void RemoveGraphics(IGraphicsContainer gc, List<AMGraphic> list)
        {
            if (gc == null || !list.Any())
                return;

            var elementList = new List<IElement>();
            gc.Reset();
            var element = gc.Next();
            while (element != null)
            {
                var eleProps = element as IElementProperties;
                if(list.Any(g => g.UniqueId == eleProps.Name))
                {
                    elementList.Add(element);
                }
                element = gc.Next();
            }

            foreach (var ele in elementList)
            {
                gc.DeleteElement(ele);
            }

            // remove from master graphics list
            foreach(var graphic in list)
            {
                if (GraphicsList.Contains(graphic))
                    GraphicsList.Remove(graphic);
            }
            elementList.Clear();
            
            RaisePropertyChanged(() => HasMapGraphics);
        }

        internal void RemoveGraphics(List<string> guidList)
        {
            if (!guidList.Any())
                return;

            var mxdoc = ArcMap.Application.Document as IMxDocument;
            if (mxdoc == null)
                return;
            var av = mxdoc.FocusMap as IActiveView;
            if (av == null)
                return;
            var gc = av as IGraphicsContainer;
            if (gc == null)
                return;

            var graphics = GraphicsList.Where(g => guidList.Contains(g.UniqueId)).ToList();
            RemoveGraphics(gc, graphics);

            av.Refresh();
        }

        /// <summary>
        /// Activates the map tool to get map points from mouse clicks/movement
        /// </summary>
        /// <param name="obj"></param>
        internal virtual void OnActivateTool(object obj)
        {
            SetToolActiveInToolBar(ArcMap.Application, "Esri_ArcMapAddinVisibility_MapPointTool");
        }
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

            var point = obj as IPoint;

            if (point == null)
                return;

            // do nothing
        }

        #endregion
        #region Public Functions
        /// <summary>
        /// Method used to deactivate tool
        /// </summary>
        public void DeactivateTool(string toolname)
        {
            if (ArcMap.Application != null
                && ArcMap.Application.CurrentTool != null
                && ArcMap.Application.CurrentTool.Name.Equals(toolname))
            {
                ArcMap.Application.CurrentTool = null;
            }
        }
        /// <summary>
        /// Method to set the map tool as the active tool for the map
        /// </summary>
        /// <param name="application"></param>
        /// <param name="toolName"></param>
        public void SetToolActiveInToolBar(ESRI.ArcGIS.Framework.IApplication application, System.String toolName)
        {
            ESRI.ArcGIS.Framework.ICommandBars commandBars = application.Document.CommandBars;
            ESRI.ArcGIS.esriSystem.UID commandID = new ESRI.ArcGIS.esriSystem.UIDClass();
            commandID.Value = toolName;
            ESRI.ArcGIS.Framework.ICommandItem commandItem = commandBars.Find(commandID, false, false);

            if (commandItem != null)
                application.CurrentTool = commandItem;
        }
        #endregion
        #region Private Functions
        /// <summary>
        /// Method will return a formatted point as a string based on the configuration settings for display coordinate type
        /// </summary>
        /// <param name="point">IPoint that is to be formatted</param>
        /// <returns>String that is formatted based on addin config display coordinate type</returns>
        private string GetFormattedPoint(IPoint point)
        {
            var result = string.Format("{0:0.0} {1:0.0}", point.Y, point.X);
            var cn = point as IConversionNotation;
            if (cn != null)
            {
                switch (VisibilityConfig.AddInConfig.DisplayCoordinateType)
                {
                    case CoordinateTypes.DD:
                        result = cn.GetDDFromCoords(6);
                        break;
                    case CoordinateTypes.DDM:
                        result = cn.GetDDMFromCoords(4);
                        break;
                    case CoordinateTypes.DMS:
                        result = cn.GetDMSFromCoords(2);
                        break;
                    case CoordinateTypes.GARS:
                        result = cn.GetGARSFromCoords();
                        break;
                    case CoordinateTypes.MGRS:
                        result = cn.CreateMGRS(5, true, esriMGRSModeEnum.esriMGRSMode_Automatic);
                        break;
                    case CoordinateTypes.USNG:
                        result = cn.GetUSNGFromCoords(5, true, true);
                        break;
                    case CoordinateTypes.UTM:
                        result = cn.GetUTMFromCoords(esriUTMConversionOptionsEnum.esriUTMAddSpaces | esriUTMConversionOptionsEnum.esriUTMUseNS);
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

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

            Point1 = null;
            Point2 = null;
            Point1Formatted = string.Empty;
            Point2Formatted = string.Empty;
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

        internal string AddTextToMap(string text, IGeometry geom, IColor color, bool IsTempGraphic = false, int size = 12)
        {
            if (geom == null || ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return string.Empty;

            IElement element = null;

            geom.Project(ArcMap.Document.FocusMap.SpatialReference);

            if (geom.GeometryType == esriGeometryType.esriGeometryPoint)
            {
                var te = new TextElementClass() as ITextElement;
                te.Text = text;

                var ts = new TextSymbolClass();
                ts.Size = size;
                ts.VerticalAlignment = esriTextVerticalAlignment.esriTVACenter;
                ts.HorizontalAlignment = esriTextHorizontalAlignment.esriTHACenter;

                te.Symbol = ts;

                element = te as IElement;
            }

            if (element == null)
                return string.Empty;

            element.Geometry = geom;

            var mxdoc = ArcMap.Application.Document as IMxDocument;
            var av = mxdoc.FocusMap as IActiveView;
            var gc = av as IGraphicsContainer;

            // store guid
            var eprop = element as IElementProperties;
            eprop.Name = Guid.NewGuid().ToString();

            GraphicsList.Add(new AMGraphic(eprop.Name, geom, IsTempGraphic));

            gc.AddElement(element, 0);

            av.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);

            RaisePropertyChanged(() => HasMapGraphics);

            return eprop.Name;
        }

        /// <summary>
        /// Adds a graphic element to the map graphics container
        /// </summary>
        /// <param name="geom">IGeometry</param>
        internal string AddGraphicToMap(IGeometry geom, IColor color, bool IsTempGraphic = false, esriSimpleMarkerStyle markerStyle = esriSimpleMarkerStyle.esriSMSCircle, int size = 5)
        {
            if (geom == null || ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return string.Empty;

            IElement element = null;
            double width = 2.0;

            geom.Project(ArcMap.Document.FocusMap.SpatialReference);

            if (geom.GeometryType == esriGeometryType.esriGeometryPoint)
            {
                // Marker symbols
                var simpleMarkerSymbol = new SimpleMarkerSymbol() as ISimpleMarkerSymbol;
                simpleMarkerSymbol.Color = color;
                simpleMarkerSymbol.Outline = true;
                simpleMarkerSymbol.OutlineColor = color;
                simpleMarkerSymbol.Size = size;
                simpleMarkerSymbol.Style = markerStyle;

                var markerElement = new MarkerElement() as IMarkerElement;
                markerElement.Symbol = simpleMarkerSymbol;
                element = markerElement as IElement;
            }
            else if (geom.GeometryType == esriGeometryType.esriGeometryPolyline)
            {
                // create graphic then add to map
                var le = new LineElementClass() as ILineElement;
                element = le as IElement;

                var lineSymbol = new SimpleLineSymbolClass();
                lineSymbol.Color = color;
                lineSymbol.Width = width;

                le.Symbol = lineSymbol;
            }
            else if (geom.GeometryType == esriGeometryType.esriGeometryPolygon)
            {
                // create graphic then add to map
                IPolygonElement pe = new PolygonElementClass() as IPolygonElement;
                element = pe as IElement;
                IFillShapeElement fe = pe as IFillShapeElement;
                
                var fillSymbol = new SimpleFillSymbolClass();
                RgbColor selectedColor = new RgbColorClass();
                selectedColor.Red = 0;
                selectedColor.Green = 0;
                selectedColor.Blue = 0;

                selectedColor.Transparency = (byte)0;
                fillSymbol.Color = selectedColor;  
                
                fe.Symbol = fillSymbol;
            }

            if (element == null)
                return string.Empty;

            element.Geometry = geom;

            var mxdoc = ArcMap.Application.Document as IMxDocument;
            var av = mxdoc.FocusMap as IActiveView;
            var gc = av as IGraphicsContainer;

            // store guid
            var eprop = element as IElementProperties;
            eprop.Name = Guid.NewGuid().ToString();

            GraphicsList.Add(new AMGraphic(eprop.Name, geom, IsTempGraphic)); 

            gc.AddElement(element, 0);

            av.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);

            RaisePropertyChanged(() => HasMapGraphics);

            return eprop.Name;
        }

        internal void AddGraphicToMap(IGeometry geom, IColor color)
        {
            AddGraphicToMap(geom, color, false);
        }

        internal void AddGraphicToMap(IGeometry geom)
        {
            var rgbColor = new ESRI.ArcGIS.Display.RgbColorClass() { Red = 255 };
            AddGraphicToMap(geom, rgbColor);
        }
        internal void AddGraphicToMap(IGeometry geom, bool isTemp)
        {
            ESRI.ArcGIS.Display.IRgbColor rgbColor = new ESRI.ArcGIS.Display.RgbColorClass();
            rgbColor.Red = 255;
            //ESRI.ArcGIS.Display.IColor color = rgbColor; // Implicit cast.
            AddGraphicToMap(geom, rgbColor, isTemp);
        }

        internal DistanceTypes GetDistanceType(int linearUnitFactoryCode)
        { 
            DistanceTypes distanceType = DistanceTypes.Meters;
            switch (linearUnitFactoryCode)
            {
                case (int)esriSRUnitType.esriSRUnit_Foot:
                    distanceType = DistanceTypes.Feet;
                    break;
                case (int)esriSRUnitType.esriSRUnit_Kilometer:
                    distanceType = DistanceTypes.Kilometers;
                    break;
                case (int)esriSRUnitType.esriSRUnit_Meter:
                    distanceType = DistanceTypes.Meters;
                    break;
                case (int)esriSRUnitType.esriSRUnit_NauticalMile:
                    distanceType = DistanceTypes.NauticalMile;
                    break;
                case (int)esriSRUnitType.esriSRUnit_SurveyFoot:
                    distanceType = DistanceTypes.SurveyFoot;
                    break;
                default:
                    distanceType = DistanceTypes.Meters;
                    break;
            }

            return distanceType;
        }

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
        /// Handler for the mouse move event
        /// When the mouse moves accross the map, IPoints are returned to aid in updating feedback to user
        /// </summary>
        /// <param name="obj">IPoint</param>
        internal virtual void OnMouseMoveEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            var point = obj as IPoint;

            if (point == null)
                return;

            // do nothing
        }

        /// <summary>
        /// Method used to convert a string to a known coordinate
        /// Assumes WGS84 for now
        /// Uses the IConversionNotation interface
        /// </summary>
        /// <param name="coordinate">the coordinate as a string</param>
        /// <returns>IPoint if successful, null if not</returns>
        internal IPoint GetPointFromString(string coordinate)
        {
            Type t = Type.GetTypeFromProgID("esriGeometry.SpatialReferenceEnvironment");
            System.Object obj = Activator.CreateInstance(t);
            ISpatialReferenceFactory srFact = obj as ISpatialReferenceFactory;

            // Use the enumeration to create an instance of the predefined object.

            IGeographicCoordinateSystem geographicCS =
                srFact.CreateGeographicCoordinateSystem((int)
                esriSRGeoCSType.esriSRGeoCS_WGS1984);

            var point = new Point() as IPoint;
            point.SpatialReference = geographicCS;
            var cn = point as IConversionNotation;

            if (cn == null)
                return null;

            try { cn.PutCoordsFromDD(coordinate); return point; }
            catch { }
            try { cn.PutCoordsFromDDM(coordinate); return point; }
            catch { }
            try { cn.PutCoordsFromDMS(coordinate); return point; }
            catch { }
            try { cn.PutCoordsFromGARS(esriGARSModeEnum.esriGARSModeCENTER, coordinate); return point; }
            catch { }
            try { cn.PutCoordsFromGARS(esriGARSModeEnum.esriGARSModeLL, coordinate); return point; }
            catch { }
            try { cn.PutCoordsFromMGRS(coordinate, esriMGRSModeEnum.esriMGRSMode_Automatic); return point; }
            catch { }
            try { cn.PutCoordsFromMGRS(coordinate, esriMGRSModeEnum.esriMGRSMode_NewStyle); return point; }
            catch { }
            try { cn.PutCoordsFromMGRS(coordinate, esriMGRSModeEnum.esriMGRSMode_NewWith180InZone01); return point; }
            catch { }
            try { cn.PutCoordsFromMGRS(coordinate, esriMGRSModeEnum.esriMGRSMode_OldStyle); return point; }
            catch { }
            try { cn.PutCoordsFromMGRS(coordinate, esriMGRSModeEnum.esriMGRSMode_OldWith180InZone01); return point; }
            catch { }
            try { cn.PutCoordsFromMGRS(coordinate, esriMGRSModeEnum.esriMGRSMode_USNG); return point; }
            catch { }
            try { cn.PutCoordsFromUSNG(coordinate); return point; }
            catch { }
            try { cn.PutCoordsFromUTM(esriUTMConversionOptionsEnum.esriUTMAddSpaces, coordinate); return point; }
            catch { }
            try { cn.PutCoordsFromUTM(esriUTMConversionOptionsEnum.esriUTMUseNS, coordinate); return point; }
            catch { }
            try { cn.PutCoordsFromUTM(esriUTMConversionOptionsEnum.esriUTMAddSpaces | esriUTMConversionOptionsEnum.esriUTMUseNS, coordinate); return point; }
            catch { }
            try { cn.PutCoordsFromUTM(esriUTMConversionOptionsEnum.esriUTMNoOptions, coordinate); return point; }
            catch { }
            try { cn.PutCoordsFromGeoRef(coordinate); return point; }
            catch { }

            // lets see if we have a PCS coordinate
            // we'll assume the same units as the map units
            // get spatial reference of map
            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null || ArcMap.Document.FocusMap.SpatialReference == null)
                return null;

            var map = ArcMap.Document.FocusMap;
            var pcs = map.SpatialReference as IProjectedCoordinateSystem;

            if (pcs == null)
                return null;

            point.SpatialReference = map.SpatialReference;
            // get pcs coordinate from input
            coordinate = coordinate.Trim();

            Regex regexMercator = new Regex(@"^(?<latitude>\-?\d+\.?\d*)[+,;:\s]*(?<longitude>\-?\d+\.?\d*)");

            var matchMercator = regexMercator.Match(coordinate);

            if (matchMercator.Success && matchMercator.Length == coordinate.Length)
            {
                try
                {
                    var Lat = Double.Parse(matchMercator.Groups["latitude"].Value);
                    var Lon = Double.Parse(matchMercator.Groups["longitude"].Value);
                    point.PutCoords(Lon, Lat);
                    return point;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        #endregion Private Functions
    }
}
