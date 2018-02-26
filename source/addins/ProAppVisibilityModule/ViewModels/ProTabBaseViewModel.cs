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
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

// Pro SDK
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProAppVisibilityModule.Helpers;
using ProAppVisibilityModule.Models;

// Visibility
using VisibilityLibrary.Helpers;
using VisibilityLibrary.ViewModels;

namespace ProAppVisibilityModule.ViewModels
{
    /// <summary>
    /// Base class for all the common properties, commands and events for tab items
    /// </summary>
    public class ProTabBaseViewModel : BaseViewModel
    {
        public ProTabBaseViewModel()
        {
            //commands
            ClearGraphicsCommand = new VisibilityLibrary.Helpers.RelayCommand(OnClearGraphics);
            ActivateToolCommand = new VisibilityLibrary.Helpers.RelayCommand(OnActivateToolCommand);
            EnterKeyCommand = new VisibilityLibrary.Helpers.RelayCommand(OnEnterKeyCommand);
            CancelCommand = new VisibilityLibrary.Helpers.RelayCommand(OnCancelCommand);

            // Mediator
            Mediator.Register(VisibilityLibrary.Constants.NEW_MAP_POINT, OnNewMapPointEvent);
            Mediator.Register(VisibilityLibrary.Constants.MOUSE_MOVE_POINT, OnMouseMoveEvent);
            Mediator.Register(VisibilityLibrary.Constants.TAB_ITEM_SELECTED, OnTabItemSelected);

            Mediator.Register(VisibilityLibrary.Constants.MAP_POINT_TOOL_ACTIVATED, OnMapPointToolActivated);
            Mediator.Register(VisibilityLibrary.Constants.MAP_POINT_TOOL_DEACTIVATED, OnMapPointToolDeactivated);

            // Pro Events
            ArcGIS.Desktop.Framework.Events.ActiveToolChangedEvent.Subscribe(OnActiveToolChanged);

            ClearGraphicsVisible = false;
        }

        private async void OnMapPointToolActivated(object obj)
        {
            var addList = new List<tempProGraphic>();
            var removeList = new List<ProGraphic>();

            foreach (var item in ProGraphicsList)
            {
                if (item.Disposable != null || item.IsTemp == false)
                    continue;

                // re-add graphic to map overlay
                SimpleMarkerStyle ms = SimpleMarkerStyle.Circle;
                CIMColor color = ColorFactory.Instance.BlueRGB;

                if (item.Tag == "target")
                {
                    ms = SimpleMarkerStyle.Square;
                    color = ColorFactory.Instance.RedRGB;
                }
                addList.Add(new tempProGraphic()
                {
                    GUID = item.GUID,
                    Geometry = item.Geometry,
                    Color = color,
                    IsTemp = true,
                    Size = 5.0,
                    MarkerStyle = ms
                });
            }

            foreach (var temp in addList)
            {
                var pgOLD = ProGraphicsList.FirstOrDefault(g => g.GUID == temp.GUID);

                var guid = await AddGraphicToMap(temp.Geometry, temp.Color, temp.IsTemp, temp.Size, markerStyle: temp.MarkerStyle, tag: pgOLD.Tag);

                var pgNew = ProGraphicsList.FirstOrDefault(g => g.GUID == guid);
                pgNew.GUID = pgOLD.GUID;
                removeList.Add(pgOLD);
            }

            foreach (var pg in removeList)
                ProGraphicsList.Remove(pg);
        }

        protected virtual void OnMapPointToolDeactivated(object obj)
        {
            foreach (var item in ProGraphicsList)
            {
                if (item.Disposable != null && item.IsTemp == true)
                {
                    if (item.Disposable != null)
                        item.Disposable.Dispose();
                    item.Disposable = null;
                }
            }
        }

        private class tempProGraphic
        {
            public tempProGraphic() { }

            public string GUID { get; set; }
            public Geometry Geometry { get; set; }
            public CIMColor Color { get; set; }
            public bool IsTemp { get; set; }
            public double Size { get; set; }
            public SimpleMarkerStyle MarkerStyle { get; set; }
        }

        #region Properties

        /// <summary>
        /// save last active tool used, so we can set back to this 
        /// </summary>
        private string lastActiveToolName;

        /// <summary>
        /// lists to store GUIDs of graphics, temp feedback and map graphics
        /// </summary>
        private static List<ProGraphic> ProGraphicsList = new List<ProGraphic>();

        /// <summary>
        /// Property used to determine if there are non temp graphics
        /// </summary>
        public bool HasMapGraphics
        {
            get
            {
                return ProGraphicsList.Any(g => g.IsTemp == false);
            }
        }

        /// <summary>
        /// Property used to determine if ClearGraphics button should be visible
        /// </summary>
        public bool ClearGraphicsVisible { get; set; }

        private MapPoint point1 = null;
        /// <summary>
        /// Property for the observer MapPoint
        /// </summary>
        public virtual MapPoint Point1
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

        private MapPoint point2 = null;
        /// <summary>
        /// Property for the target MapPoint
        /// Not all tools need a second point
        /// </summary>
        public virtual MapPoint Point2
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
        /// String property for the observer MapPoint
        /// This is used to format the point for the UI and allow string input of different types of coordinates
        /// </summary>
        public string Point1Formatted
        {
            get
            {
                // return a formatted first point depending on how it was entered, manually or via map point tool
                if (string.IsNullOrWhiteSpace(point1Formatted))
                {
                    if (Point1 != null)
                    {
                        // only format if the Point1 data was generated from a mouse click
                        string outFormattedString = string.Empty;
                        CoordinateConversionLibrary.Models.CoordinateType ccType = CoordinateConversionLibrary.Helpers.ConversionUtils.GetCoordinateString(MapPointHelper.GetMapPointAsDisplayString(Point1), out outFormattedString);
                        return outFormattedString;
                    }
                    return string.Empty;
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
                // try to convert string to a MapPoint
                string outFormattedString = string.Empty;
                CoordinateConversionLibrary.Models.CoordinateType ccType = CoordinateConversionLibrary.Helpers.ConversionUtils.GetCoordinateString(value, out outFormattedString);
                MapPoint point = (ccType != CoordinateConversionLibrary.Models.CoordinateType.Unknown) ? GetMapPointFromString(outFormattedString) : null;
                if (point != null)
                {
                    point1Formatted = value;
                    Point1 = point;
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
        /// String property for the target MapPoint
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
                    if (Point2 != null)
                    {
                        // only format if the Point2 data was generated from a mouse click
                        string outFormattedString = string.Empty;
                        CoordinateConversionLibrary.Models.CoordinateType ccType = CoordinateConversionLibrary.Helpers.ConversionUtils.GetCoordinateString(MapPointHelper.GetMapPointAsDisplayString(Point2), out outFormattedString);
                        return outFormattedString;
                    }
                    return string.Empty;
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
                // try to convert string to a MapPoint
                string outFormattedString = string.Empty;
                CoordinateConversionLibrary.Models.CoordinateType ccType = CoordinateConversionLibrary.Helpers.ConversionUtils.GetCoordinateString(value, out outFormattedString);
                MapPoint point = (ccType != CoordinateConversionLibrary.Models.CoordinateType.Unknown) ? GetMapPointFromString(outFormattedString) : null;
                if (point != null)
                {
                    point2Formatted = value;
                    Point2 = point;
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
        /// Property used to test if there is enough info to create a map element(s)
        /// </summary>
        public virtual bool CanCreateElement
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Commands

        public VisibilityLibrary.Helpers.RelayCommand ClearGraphicsCommand { get; set; }
        public VisibilityLibrary.Helpers.RelayCommand EnterKeyCommand { get; set; }
        public VisibilityLibrary.Helpers.RelayCommand CancelCommand { get; set; }
        public VisibilityLibrary.Helpers.RelayCommand ActivateToolCommand { get; set; }

        /// <summary>
        /// Clears all the graphics from the maps graphic container
        /// Inlucdes temp and map graphics
        /// Only removes temp and map graphics that were created by this add-in
        /// </summary>
        /// <param name="obj"></param>
        private void OnClearGraphics(object obj)
        {
            try
            {
                if (MapView.Active == null)
                    return;

                foreach (var item in ProGraphicsList)
                {
                    if (item.Disposable != null)
                        item.Disposable.Dispose();
                }

                ProGraphicsList.Clear();

                RaisePropertyChanged(() => HasMapGraphics);
            }
            catch(Exception ex)
            {
                Debug.Print(ex.Message);
            }
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
        /// Handler for the cancel command
        /// </summary>
        /// <param name="obj"></param>
        private void OnCancelCommand(object obj)
        {
            Reset(true);
        }

        /// <summary>
        /// Handler for the activate tool command
        /// Sets the current tool
        /// </summary>
        /// <param name="obj"></param>
        internal virtual void OnActivateToolCommand(object obj)
        {
            FrameworkApplication.SetCurrentToolAsync(VisibilityMapTool.ToolId);
        }

        #endregion

        #region Event Methods

        /// <summary>
        /// Handler for the new map point click event
        /// </summary>
        /// <param name="obj">MapPoint</param>
        internal virtual void OnNewMapPointEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            var point = obj as MapPoint;

            if (point == null)
                return;

            // do nothing
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Removes graphics from the map
        /// </summary>
        /// <param name="guidList">list of GUIDs</param>
        internal void RemoveGraphics(List<string> guidList)
        {
            var list = ProGraphicsList.Where(g => guidList.Contains(g.GUID)).ToList();
            foreach (var graphic in list)
            {
                if(graphic.Disposable != null)
                    graphic.Disposable.Dispose();
                ProGraphicsList.Remove(graphic);
            }

            RaisePropertyChanged(() => HasMapGraphics);
        }

        /// <summary>
        /// Derived class must override this method in order to create map elements
        /// Clears temp graphics by default
        /// </summary>
        internal virtual async Task CreateMapElement()
        {
                await Task.Run(() =>
                    {
                        ClearTempGraphics();
                    });
        }

        /// <summary>
        /// Method to clear all temp graphics
        /// </summary>
        internal void ClearTempGraphics()
        {
            var list = ProGraphicsList.Where(g => g.IsTemp == true).ToList();

            foreach (var item in list)
            {
                if (item.Disposable != null)
                    item.Disposable.Dispose();
                Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProGraphicsList.Remove(item);
                    });
            }

            RaisePropertyChanged(() => HasMapGraphics);
        }
        
        /// <summary>
        /// Method used to totally reset the tool
        /// reset points, feedback
        /// clear out textboxes
        /// </summary>
        internal virtual async Task Reset(bool toolReset)
        {
            if (toolReset)
            {
                DeactivateTool(VisibilityMapTool.ToolId);
            }

            Point1 = null;
            Point2 = null;
            Point1Formatted = string.Empty;
            Point2Formatted = string.Empty;
        }

        /// <summary>
        /// Method used to convert a string to a known coordinate
        /// Assumes WGS84 for now
        /// </summary>
        /// <param name="coordinate">the coordinate as a string</param>
        /// <returns>MapPoint if successful, null if not</returns>
        internal MapPoint GetMapPointFromString(string coordinate)
        {
            MapPoint point = null;

            // future use if order of GetValues is not acceptable
            //var listOfTypes = new List<GeoCoordinateType>(new GeoCoordinateType[] {
            //    GeoCoordinateType.DD,
            //    GeoCoordinateType.DDM,
            //    GeoCoordinateType.DMS,
            //    GeoCoordinateType.GARS,
            //    GeoCoordinateType.GeoRef,
            //    GeoCoordinateType.MGRS,
            //    GeoCoordinateType.USNG,
            //    GeoCoordinateType.UTM
            //});

            var listOfTypes = Enum.GetValues(typeof(GeoCoordinateType)).Cast<GeoCoordinateType>();

            foreach (var type in listOfTypes)
            {
                try
                {
                    point = QueuedTask.Run(() =>
                    {
                        return MapPointBuilder.FromGeoCoordinateString(coordinate, MapView.Active.Map.SpatialReference, type, FromGeoCoordinateMode.Default);
                    }).Result;
                }
                catch (Exception ex)
                {
                    // do nothing
                }

                if (point != null)
                    return point;
            }

            try
            {
                point = QueuedTask.Run(() =>
                {
                    return MapPointBuilder.FromGeoCoordinateString(coordinate, MapView.Active.Map.SpatialReference, GeoCoordinateType.UTM, FromGeoCoordinateMode.UtmNorthSouth);
                }).Result;
            }
            catch (Exception ex)
            {
                // do nothing
            }

            if (point == null)
            {
                coordinate = coordinate.Trim();

                Regex regexMercator = new Regex(@"^(?<latitude>\-?\d+\.?\d*)[+,;:\s]*(?<longitude>\-?\d+\.?\d*)");

                var matchMercator = regexMercator.Match(coordinate);

                if (matchMercator.Success && matchMercator.Length == coordinate.Length)
                {
                    try
                    {
                        var Lat = Double.Parse(matchMercator.Groups["latitude"].Value);
                        var Lon = Double.Parse(matchMercator.Groups["longitude"].Value);
                        point = QueuedTask.Run(() =>
                            {
                                return MapPointBuilder.CreateMapPoint(Lon, Lat);
                            }).Result;
                        return point;
                    }
                    catch (Exception ex)
                    {
                        return null;
                    }
                }
            }

            return point;
        }

        internal async Task<string> AddGraphicToMap(Geometry geom, bool IsTempGraphic = false, double size = 1.0)
        {
            // default color Red
            return await AddGraphicToMap(geom, ColorFactory.Instance.RedRGB, IsTempGraphic, size);
        }

        internal async Task<string> AddGraphicToMap(Geometry geom, CIMColor color, bool IsTempGraphic = false, double size = 1.0, string text = "", SimpleMarkerStyle markerStyle = SimpleMarkerStyle.Circle, string tag = "")
        {
            if (geom == null || MapView.Active == null)
                return string.Empty;

            CIMSymbolReference symbol = null;

            if (!string.IsNullOrWhiteSpace(text) && geom.GeometryType == GeometryType.Point)
            {
                await QueuedTask.Run(() =>
                {
                    // TODO add text graphic
                    //var tg = new CIMTextGraphic() { Placement = Anchor.CenterPoint, Text = text};
                });
            }
            else if (geom.GeometryType == GeometryType.Point)
            {
                await QueuedTask.Run(() =>
                {
                    var s = SymbolFactory.Instance.ConstructPointSymbol(color, size, markerStyle);
                    symbol = new CIMSymbolReference() { Symbol = s };
                });
            }
            else if (geom.GeometryType == GeometryType.Polyline)
            {
                await QueuedTask.Run(() =>
                {
                    var s = SymbolFactory.Instance.ConstructLineSymbol(color, size);
                    symbol = new CIMSymbolReference() { Symbol = s };
                });
            }
            else if (geom.GeometryType == GeometryType.Polygon)
            {
                await QueuedTask.Run(() =>
                {
                    var outline = SymbolFactory.Instance.ConstructStroke(ColorFactory.Instance.BlackRGB, 1.0, SimpleLineStyle.Solid);
                    var s = SymbolFactory.Instance.ConstructPolygonSymbol(color, SimpleFillStyle.Solid, outline);
                    symbol = new CIMSymbolReference() { Symbol = s };
                });
            }

            var result = await QueuedTask.Run(() =>
            {
                var disposable = MapView.Active.AddOverlay(geom, symbol);
                var guid = Guid.NewGuid().ToString();
                ProGraphicsList.Add(new ProGraphic(disposable, guid, geom, IsTempGraphic, tag));
                return guid;
            });

            return result;
        }

        /// <summary>
        /// Handler for the mouse move event
        /// When the mouse moves accross the map, MapPoints are returned to aid in updating feedback to user
        /// </summary>
        /// <param name="obj">MapPoint</param>
        internal virtual void OnMouseMoveEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            var point = obj as MapPoint;

            if (point == null)
                return;

            // do nothing
        }

        internal async Task ZoomToExtent(Envelope env)
        {
            if (env == null || MapView.Active == null || MapView.Active.Map == null)
                return;

            double extentPercent = (env.XMax - env.XMin) > (env.YMax - env.YMin) ? (env.XMax - env.XMin) * .3 : (env.YMax - env.YMin) * .3;
            double xmax = env.XMax + extentPercent;
            double xmin = env.XMin - extentPercent;
            double ymax = env.YMax + extentPercent;
            double ymin = env.YMin - extentPercent;

            //Create the envelope
            var envelope = await QueuedTask.Run(() => ArcGIS.Core.Geometry.EnvelopeBuilder.CreateEnvelope(xmin, ymin, xmax, ymax, MapView.Active.Map.SpatialReference));

            //Zoom the view to a given extent.
            await MapView.Active.ZoomToAsync(envelope, TimeSpan.FromSeconds(0.5));
        }

        #endregion Internal Methods

        #region Private Methods

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

        /// <summary>
        /// Method used to deactivate tool
        /// </summary>
        internal void DeactivateTool(string toolname)
        {
            if (FrameworkApplication.CurrentTool != null &&
                FrameworkApplication.CurrentTool.Equals(toolname))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FrameworkApplication.SetCurrentToolAsync(lastActiveToolName);
                });
            }
        }

        private void OnActiveToolChanged(ArcGIS.Desktop.Framework.Events.ToolEventArgs args)
        {
            string currentActiveToolName = args.CurrentID;

            if (currentActiveToolName != VisibilityMapTool.ToolId)
            {
                lastActiveToolName = currentActiveToolName;
            }
        }

        #endregion Private Methods
    }
}
