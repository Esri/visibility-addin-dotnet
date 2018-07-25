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

using ArcMapAddinVisibility.Models;
using CoordinateConversionLibrary.Models;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using VisibilityLibrary;
using VisibilityLibrary.Helpers;
using VisibilityLibrary.ViewModels;
using VisibilityLibrary.Views;

namespace ArcMapAddinVisibility.ViewModels
{
    public class LOSBaseViewModel : TabBaseViewModel
    {
        public LOSBaseViewModel()
        {
            ObserverOffset = 2.0;
            TargetOffset = 0.0;
            OffsetUnitType = DistanceTypes.Meters;
            DistanceUnitType = DistanceTypes.Meters;
            AngularUnitType = AngularTypes.DEGREES;

            ObserverAddInPoints = new ObservableCollection<AddInPoint>();
            TargetAddInPoints = new ObservableCollection<AddInPoint>();
            LLOS_ObserversInExtent = new ObservableCollection<AddInPointObject>();
            LLOS_ObserversOutOfExtent = new ObservableCollection<AddInPointObject>();
            LLOS_TargetsInExtent = new ObservableCollection<AddInPointObject>();
            LLOS_TargetsOutOfExtent = new ObservableCollection<AddInPointObject>();
            RLOS_ObserversInExtent = new ObservableCollection<AddInPointObject>();
            RLOS_ObserversOutOfExtent = new ObservableCollection<AddInPointObject>();
            EnterManullyOption = VisibilityLibrary.Properties.Resources.EnterManuallyOption;

            toolMode = MapPointToolMode.Unknown;
            SurfaceLayerNames = new ObservableCollection<string>();
            LLOS_ObserverLyrNames = new ObservableCollection<string>();
            LLOS_TargetLyrNames = new ObservableCollection<string>();
            RLOS_ObserverLyrNames = new ObservableCollection<string>();
            SelectedSurfaceName = string.Empty;
            SelectedLLOS_ObserverLyrName = string.Empty;
            SelectedLLOS_TargetLyrName = string.Empty;
            SelectedRLOS_ObserverLyrName = string.Empty;

            Mediator.Register(Constants.MAP_TOC_UPDATED, OnMapTocUpdated);
            Mediator.Register(Constants.DISPLAY_COORDINATE_TYPE_CHANGED, OnDisplayCoordinateTypeChanged);

            DeletePointCommand = new RelayCommand(OnDeletePointCommand);
            DeleteAllPointsCommand = new RelayCommand(OnDeleteAllPointsCommand);
            PasteCoordinatesCommand = new RelayCommand(OnPasteCommand);
            ImportCSVFileCommand = new RelayCommand(OnImportCSVFileCommand);
            EditPropertiesDialogCommand = new RelayCommand(OnEditPropertiesDialogCommand);
        }

        #region Properties

        private bool observerToolActive = false;
        public bool ObserverToolActive
        {
            get { return observerToolActive; }
            set
            {
                observerToolActive = value;
                RaisePropertyChanged(() => ObserverToolActive);
            }
        }

        private bool targetToolActive = false;
        public bool TargetToolActive
        {
            get { return targetToolActive; }
            set
            {
                targetToolActive = value;
                RaisePropertyChanged(() => TargetToolActive);
            }
        }

        private bool isRunning = false;
        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                isRunning = value;
                RaisePropertyChanged(() => IsRunning);
            }
        }

        private double? observerOffset;
        public double? ObserverOffset
        {
            get { return observerOffset; }
            set
            {
                observerOffset = value;
                RaisePropertyChanged(() => ObserverOffset);

                if (!observerOffset.HasValue)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEInvalidInput);
            }
        }

        private double? targetOffset;
        public double? TargetOffset
        {
            get { return targetOffset; }
            set
            {
                targetOffset = value;
                RaisePropertyChanged(() => TargetOffset);

                if (!targetOffset.HasValue)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEInvalidInput);
            }
        }

        private MapPointToolMode toolMode;
        public MapPointToolMode ToolMode
        {
            get { return toolMode; }
            set
            {
                toolMode = value;
                if (toolMode == MapPointToolMode.Observer)
                {
                    ObserverToolActive = true;
                    TargetToolActive = false;
                }
                else if (toolMode == MapPointToolMode.Target)
                {
                    ObserverToolActive = false;
                    TargetToolActive = true;
                }
                else
                {
                    ObserverToolActive = false;
                    TargetToolActive = false;

                    DeactivateTool(ThisAddIn.IDs.MapPointTool);
                }
            }
        }

        private string _selectedLLOS_TargetLyrName;
        public string SelectedLLOS_TargetLyrName
        {
            get
            {
                return _selectedLLOS_TargetLyrName;
            }
            set
            {
                _selectedLLOS_TargetLyrName = value;
                ValidateLLOS_LayerSelection();
                RaisePropertyChanged(() => SelectedLLOS_TargetLyrName);
            }
        }

        private string _selectedLLOS_ObserverLyrName;
        public string SelectedLLOS_ObserverLyrName
        {
            get
            {
                return _selectedLLOS_ObserverLyrName;
            }
            set
            {
                _selectedLLOS_ObserverLyrName = value;
                ValidateLLOS_LayerSelection();
                RaisePropertyChanged(() => SelectedLLOS_ObserverLyrName);
            }
        }

        private string _selectedRLOS_ObserverLyrName;
        public string SelectedRLOS_ObserverLyrName
        {
            get
            {
                return _selectedRLOS_ObserverLyrName;
            }
            set
            {
                _selectedRLOS_ObserverLyrName = value;
                ValidateRLOS_LayerSelection();
                RaisePropertyChanged(() => SelectedRLOS_ObserverLyrName);
            }
        }

        public ObservableCollection<string> LLOS_ObserverLyrNames { get; set; }
        public ObservableCollection<string> LLOS_TargetLyrNames { get; set; }
        public ObservableCollection<string> RLOS_ObserverLyrNames { get; set; }
        
        private bool _isLLOSValidSelection { get; set; }
        public bool IsLLOSValidSelection
        {
            get
            {
                return _isLLOSValidSelection;
            }
            set
            {
                _isLLOSValidSelection = value;
                RaisePropertyChanged(() => IsLLOSValidSelection);
            }
        }
        
        private bool _isRLOSValidSelection { get; set; }
        public bool IsRLOSValidSelection
        {
            get
            {
                return _isRLOSValidSelection;
            }
            set
            {
                _isRLOSValidSelection = value;
                RaisePropertyChanged(() => IsRLOSValidSelection);
            }
        }
        
        public string EnterManullyOption { get; set; }
        public ObservableCollection<AddInPointObject> LLOS_ObserversInExtent { get; set; }
        public ObservableCollection<AddInPointObject> LLOS_ObserversOutOfExtent { get; set; }
        public ObservableCollection<AddInPointObject> LLOS_TargetsInExtent { get; set; }
        public ObservableCollection<AddInPointObject> LLOS_TargetsOutOfExtent { get; set; }
        public ObservableCollection<AddInPointObject> RLOS_ObserversInExtent { get; set; }
        public ObservableCollection<AddInPointObject> RLOS_ObserversOutOfExtent { get; set; }

        public ObservableCollection<AddInPoint> ObserverAddInPoints { get; set; }
        public ObservableCollection<AddInPoint> TargetAddInPoints { get; set; }
        public ObservableCollection<string> SurfaceLayerNames { get; set; }
        public string SelectedSurfaceName { get; set; }
        public DistanceTypes OffsetUnitType { get; set; }
        public DistanceTypes DistanceUnitType { get; set; }
        public AngularTypes AngularUnitType { get; set; }

        #endregion

        #region Commands

        public RelayCommand DeletePointCommand { get; set; }
        public RelayCommand DeleteAllPointsCommand { get; set; }
        public RelayCommand EditPropertiesDialogCommand { get; set; }
        public RelayCommand PasteCoordinatesCommand { get; set; }
        public RelayCommand ImportCSVFileCommand { get; set; }

        /// <summary>
        /// Command method to delete points
        /// </summary>
        /// <param name="obj"></param>
        internal virtual void OnDeletePointCommand(object obj)
        {
            // remove observer points
            var items = obj as IList;
            var objects = items.Cast<AddInPoint>().ToList();

            if (objects == null)
                return;

            DeletePoints(objects);
        }

        internal virtual void OnDeleteAllPointsCommand(object obj)
        {
            DeletePoints(ObserverAddInPoints.ToList());
        }

        /// <summary>
        /// Handler for opening the edit properties dialog
        /// </summary>
        /// <param name="obj"></param>
        private void OnEditPropertiesDialogCommand(object obj)
        {
            var dlg = new EditPropertiesView();

            dlg.DataContext = new EditPropertiesViewModel();

            dlg.ShowDialog();
        }

        private void DeletePoints(List<AddInPoint> observers)
        {
            if (observers == null || !observers.Any())
                return;

            // remove graphics from map
            var guidList = observers.Select(x => x.GUID).ToList();
            RemoveGraphics(guidList);

            foreach (var point in observers)
            {
                ObserverAddInPoints.Remove(point);
            }
        }

        /// <summary>
        /// Command method to import points from csv file.
        /// </summary>
        /// <param name="obj"></param>
        public virtual void OnImportCSVFileCommand(object obj)
        {
            var mode = obj.ToString();
            CoordinateConversionLibraryConfig.AddInConfig.DisplayAmbiguousCoordsDlg = false;

            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.CheckFileExists = true;
            fileDialog.CheckPathExists = true;
            fileDialog.Filter = "csv files|*.csv";

            // attemp to import
            var fieldVM = new CoordinateConversionLibrary.ViewModels.SelectCoordinateFieldsViewModel();
            var result = fileDialog.ShowDialog();
            if (result.HasValue && result.Value == true)
            {
                var dlg = new CoordinateConversionLibrary.Views.SelectCoordinateFieldsView();
                using (Stream s = new FileStream(fileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var headers = CoordinateConversionLibrary.Helpers.ImportCSV.GetHeaders(s);
                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            fieldVM.AvailableFields.Add(header);
                            System.Diagnostics.Debug.WriteLine("header : {0}", header);
                        }
                        dlg.DataContext = fieldVM;
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgNoDataFound);
                        return;
                    }
                }
                if (dlg.ShowDialog() == true)
                {
                    using (Stream s = new FileStream(fileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var lists = CoordinateConversionLibrary.Helpers.ImportCSV.Import<CoordinateConversionLibrary.ViewModels.ImportCoordinatesList>(s, fieldVM.SelectedFields.ToArray());

                        foreach (var item in lists)
                        {
                            string outFormattedString = string.Empty;
                            var sb = new StringBuilder();
                            sb.Append(item.lat.Trim());
                            if (fieldVM.UseTwoFields)
                                sb.Append(string.Format(" {0}", item.lon.Trim()));

                            string coordinate = sb.ToString();
                            CoordinateConversionLibrary.Models.CoordinateType ccType = CoordinateConversionLibrary.Helpers.ConversionUtils.GetCoordinateString(coordinate, out outFormattedString);
                            if (ccType == CoordinateConversionLibrary.Models.CoordinateType.Unknown)
                            {
                                Regex regexMercator = new Regex(@"^(?<latitude>\-?\d+\.?\d*)[+,;:\s]*(?<longitude>\-?\d+\.?\d*)");
                                var matchMercator = regexMercator.Match(coordinate);
                                if (matchMercator.Success && matchMercator.Length == coordinate.Length)
                                {
                                    ccType = CoordinateType.DD;
                                }
                            }
                            IPoint point = (ccType != CoordinateConversionLibrary.Models.CoordinateType.Unknown) ? GetPointFromString(outFormattedString) : null;
                            if (point != null)
                            {
                                if (mode == VisibilityLibrary.Properties.Resources.ToolModeObserver)
                                {
                                    ToolMode = MapPointToolMode.Observer;
                                    Point1 = point;
                                    if ((ArcMap.Document != null) && (ArcMap.Document.FocusMap != null))
                                        point.Project(ArcMap.Document.FocusMap.SpatialReference);
                                    OnNewMapPointEvent(Point1);
                                }
                                else if (mode == VisibilityLibrary.Properties.Resources.ToolModeTarget)
                                {
                                    ToolMode = MapPointToolMode.Target;
                                    Point2 = point;
                                    if ((ArcMap.Document != null) && (ArcMap.Document.FocusMap != null))
                                        point.Project(ArcMap.Document.FocusMap.SpatialReference);
                                    OnNewMapPointEvent(Point2);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Command method to paste points from clipboard.
        /// </summary>
        /// <param name="obj"></param>
        internal virtual void OnPasteCommand(object obj)
        {
            var mode = obj.ToString();

            if (string.IsNullOrWhiteSpace(mode))
                return;

            var input = Clipboard.GetText().Trim();
            string[] lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var coordinates = new List<string>();
            foreach (var item in lines)
            {
                string outFormattedString = string.Empty;
                string coordinate = item.Trim().ToString();
                CoordinateConversionLibrary.Models.CoordinateType ccType = CoordinateConversionLibrary.Helpers.ConversionUtils.GetCoordinateString(coordinate, out outFormattedString);
                if (ccType == CoordinateConversionLibrary.Models.CoordinateType.Unknown)
                {
                    Regex regexMercator = new Regex(@"^(?<latitude>\-?\d+\.?\d*)[+,;:\s]*(?<longitude>\-?\d+\.?\d*)");
                    var matchMercator = regexMercator.Match(coordinate);
                    if (matchMercator.Success && matchMercator.Length == coordinate.Length)
                    {
                        ccType = CoordinateType.DD;
                    }
                }
                IPoint point = (ccType != CoordinateConversionLibrary.Models.CoordinateType.Unknown) ? GetPointFromString(outFormattedString) : null;
                if (point != null)
                {
                    if (mode == VisibilityLibrary.Properties.Resources.ToolModeObserver)
                    {
                        ToolMode = MapPointToolMode.Observer;
                        Point1 = point;
                        if ((ArcMap.Document != null) && (ArcMap.Document.FocusMap != null))
                            point.Project(ArcMap.Document.FocusMap.SpatialReference);
                        OnNewMapPointEvent(Point1);
                    }
                    else if (mode == VisibilityLibrary.Properties.Resources.ToolModeTarget)
                    {
                        ToolMode = MapPointToolMode.Target;
                        Point2 = point;
                        if ((ArcMap.Document != null) && (ArcMap.Document.FocusMap != null))
                            point.Project(ArcMap.Document.FocusMap.SpatialReference);
                        OnNewMapPointEvent(Point2);
                    }
                }
            }
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Method called when the map TOC is updated
        /// Reset surface names
        /// </summary>
        /// <param name="obj">not used</param>
        private void OnMapTocUpdated(object obj)
        {
            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return;

            var map = ArcMap.Document.FocusMap;

            ResetSurfaceNames(map);
        }

        /// <summary>
        /// Override this method to implement a "Mode" to separate the input of
        /// observer points and target points
        /// </summary>
        /// <param name="obj">ToolMode string from resource file</param>
        internal override void OnActivateToolCommand(object obj)
        {
            var mode = obj.ToString();

            MapPointToolMode lastToolMode = ToolMode;

            if (string.IsNullOrWhiteSpace(mode))
                return;

            if ((mode == VisibilityLibrary.Properties.Resources.ToolModeObserver) &&
                (lastToolMode != MapPointToolMode.Observer))
                ToolMode = MapPointToolMode.Observer;
            else if ((mode == VisibilityLibrary.Properties.Resources.ToolModeTarget) &&
                (lastToolMode != MapPointToolMode.Target))
                ToolMode = MapPointToolMode.Target;
            else
                ToolMode = MapPointToolMode.Unknown;

            if (ToolMode != MapPointToolMode.Unknown)
                base.OnActivateToolCommand(obj);
        }

        protected override void OnMapPointToolDeactivated(object obj)
        {
            if (ToolMode != MapPointToolMode.Unknown)
                ToolMode = MapPointToolMode.Unknown;

            base.OnMapPointToolDeactivated(obj);
        }

        /// <summary>
        /// Override this event to collect observer points based on tool mode
        /// </summary>
        /// <param name="obj">MapPointToolMode</param>
        internal override void OnNewMapPointEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            var point = obj as IPoint;

            if (point == null || !IsValidPoint(point, true))
                return;

            // ok, we have a point
            if (ToolMode == MapPointToolMode.Observer)
            {
                // in tool mode "Observer" we add observer points
                // otherwise ignore

                var color = new RgbColorClass() { Blue = 255 } as IColor;
                var guid = AddGraphicToMap(point, color, true);
                var addInPoint = new AddInPoint() { Point = point, GUID = guid };
                ObserverAddInPoints.Insert(0, addInPoint);
            }
        }

        internal override void OnMouseMoveEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            var point = obj as IPoint;

            if (point == null)
                return;

            if (ToolMode == MapPointToolMode.Observer)
            {
                Point1Formatted = string.Empty;
                Point1 = point;
            }
            else if (ToolMode == MapPointToolMode.Target)
            {
                Point2Formatted = string.Empty;
                Point2 = point;
            }
        }

        /// <summary>
        /// Handler for "Enter" key press
        /// If pressed when input textbox for observer or target is focused
        ///     will set the correct tool mode and then call OnNewMapPointEvent
        /// If pressed anywhere else, resets tool mode and calls base method
        /// </summary>
        /// <param name="obj">ToolMode from resources</param>
        internal override void OnEnterKeyCommand(object obj)
        {
            var keyCommandMode = obj as string;

            if (keyCommandMode == VisibilityLibrary.Properties.Resources.ToolModeObserver)
            {
                ToolMode = MapPointToolMode.Observer;
                OnNewMapPointEvent(Point1);
            }
            else if (keyCommandMode == VisibilityLibrary.Properties.Resources.ToolModeTarget)
            {
                ToolMode = MapPointToolMode.Target;
                OnNewMapPointEvent(Point2);
            }
            else
            {
                ToolMode = MapPointToolMode.Unknown;
                base.OnEnterKeyCommand(obj);
            }
        }

        /// <summary>
        /// Method to check to see point is withing the currently selected surface
        /// returns true if there is no surface selected or point is contained by layer AOI
        /// returns false if the point is not contained in the layer AOI
        /// </summary>
        /// <param name="point">IPoint to validate</param>
        /// <param name="showPopup">boolean to show popup message or not</param>
        /// <returns></returns>
        internal bool IsValidPoint(IPoint point, bool showPopup = false)
        {
            var validPoint = true;

            if (!string.IsNullOrWhiteSpace(SelectedSurfaceName) && ArcMap.Document != null && ArcMap.Document.FocusMap != null)
            {
                validPoint = IsPointWithinExtent(point, GetLayerFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName).AreaOfInterest);

                if (validPoint == false && showPopup)
                    System.Windows.Forms.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgOutOfAOI);
            }

            return validPoint;
        }

        #endregion

        /// <summary>
        /// Enumeration used for the different tool modes
        /// </summary>
        public enum MapPointToolMode : int
        {
            Unknown = 0,
            Observer = 1,
            Target = 2
        }

        /// <summary>
        /// Method used to check to see if a point is contained by an envelope
        /// </summary>
        /// <param name="point">IPoint</param>
        /// <param name="env">IEnvelope</param>
        /// <returns></returns>
        internal bool IsPointWithinExtent(IPoint point, IEnvelope env)
        {
            var relationOp = env as IRelationalOperator;

            if (relationOp == null)
                return false;

            return relationOp.Contains(point);
        }

        /// <summary>
        /// Method to get a z offset distance in the correct units for the map
        /// </summary>
        /// <param name="offset">the input offset</param>
        /// <param name="zFactor">ISurface z factor</param>
        /// <param name="distanceType">the "from" distance unit type</param>
        /// <returns></returns>
        internal double GetOffsetInZUnits(double offset, double zFactor, DistanceTypes distanceType)
        {
            if (SelectedSurfaceSpatialRef == null)
                return offset;

            double offsetInMapUnits = 0.0;
            DistanceTypes distanceTo = DistanceTypes.Meters; // default to meters

            var pcs = SelectedSurfaceSpatialRef as IProjectedCoordinateSystem;

            if (pcs != null)
            {
                // need to convert the offset from the input distance type to the spatial reference linear type
                // then apply the zFactor
                distanceTo = GetDistanceType(pcs.CoordinateUnit.FactoryCode);
            }

            offsetInMapUnits = GetDistanceFromTo(distanceType, distanceTo, offset);

            var result = offsetInMapUnits / zFactor;

            return result;
        }

        /// <summary>
        /// Method to get a ISurface from a map with layer name
        /// </summary>
        /// <param name="map">IMap that contains surface layer</param>
        /// <param name="name">Name of the layer that you are looking for</param>
        /// <returns>ISurface</returns>
        public ISurface GetSurfaceFromMapByName(IMap map, string name)
        {
            if (map == null)
                return null;

            var layers = map.get_Layers();

            if (layers == null)
                return null;

            var layer = layers.Next();

            while (layer != null)
            {
                if (layer.Name != name)
                {
                    layer = layers.Next();
                    continue;
                }

                var tin = layer as ITinLayer;
                if (tin != null)
                {
                    return tin.Dataset as ISurface;
                }

                var rasterSurface = (IRasterSurface)new RasterSurfaceClass();
                ISurface surface = null;

                var mosaicLayer = layer as IMosaicLayer;
                var rasterLayer = layer as IRasterLayer;

                if ((mosaicLayer != null) && (mosaicLayer.PreviewLayer != null) &&
                    (mosaicLayer.PreviewLayer.Raster != null))
                {
                    rasterSurface.PutRaster(mosaicLayer.PreviewLayer.Raster, 0);
                }
                else if ((rasterLayer != null) && (rasterLayer.Raster != null))
                {
                    rasterSurface.PutRaster(rasterLayer.Raster, 0);
                }

                surface = rasterSurface as ISurface;

                if (surface != null)
                    return surface;
            }

            return null;
        }

        /// <summary>
        /// returns ILayer if found in the map layer collection
        /// </summary>
        /// <param name="map">IMap</param>
        /// <param name="name">string name of layer</param>
        /// <returns></returns>
        public ILayer GetLayerFromMapByName(IMap map, string name)
        {
            if (map == null)
                return null;

            var layers = map.get_Layers();

            if (layers == null)
                return null;

            var layer = layers.Next();

            while (layer != null)
            {
                if (layer.Name == name)
                    return layer;

                layer = layers.Next();
            }

            return null;
        }

        /// <summary>
        /// Method to get all the names of the raster/tin layers that support ISurface
        /// we use this method to populate a combobox for input selection of surface layer
        /// </summary>
        /// <param name="map">IMap</param>
        /// <returns></returns>
        public List<string> GetSurfaceNamesFromMap(IMap map, bool IncludeTinLayers = false)
        {
            var list = new List<string>();

            if (map == null)
                return list;

            var layers = map.get_Layers();

            if (layers == null)
                return list;

            var layer = layers.Next();

            while (layer != null)
            {
                try
                {
                    var tin = layer as ITinLayer;

                    if (tin != null)
                    {
                        if (IncludeTinLayers)
                            list.Add(layer.Name);

                        layer = layers.Next();
                        continue;
                    }

                    var rasterSurface = (IRasterSurface)new RasterSurfaceClass();
                    ISurface surface = null;

                    var ml = layer as IMosaicLayer;

                    if (ml != null)
                    {
                        if (ml.PreviewLayer != null && ml.PreviewLayer.Raster != null)
                        {
                            rasterSurface.PutRaster(ml.PreviewLayer.Raster, 0);

                            surface = rasterSurface as ISurface;
                            if (surface != null)
                                list.Add(layer.Name);
                        }

                        layer = layers.Next();
                        continue;
                    }

                    var rasterLayer = layer as IRasterLayer;
                    if ((rasterLayer != null) && (rasterLayer.Raster != null))
                    {
                        rasterSurface.PutRaster(rasterLayer.Raster, 0);

                        surface = rasterSurface as ISurface;
                        if (surface != null)
                            list.Add(layer.Name);

                        layer = layers.Next();
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                layer = layers.Next();
            }

            return list;
        }

        /// <summary>
        /// Override to add aditional items in the class to reset tool
        /// </summary>
        /// <param name="toolReset"></param>
        internal override void Reset(bool toolReset)
        {
            base.Reset(toolReset);

            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return;

            // reset surface names OC
            ResetSurfaceNames(ArcMap.Document.FocusMap);

            // reset observer points
            ObserverAddInPoints.Clear();

            ClearTempGraphics();
        }

        /// <summary>
        /// Method used to reset the currently selected surfacename
        /// Use when toc items or map changes, on tab selection changed, etc
        /// </summary>
        /// <param name="map">IMap</param>
        internal void ResetSurfaceNames(IMap map)
        {
            // keep the current selection if it's still valid
            var tempName = SelectedSurfaceName;

            SurfaceLayerNames.Clear();

            foreach (var name in GetSurfaceNamesFromMap(map, (this.GetType() == typeof(LLOSViewModel)) ? true : false))
                SurfaceLayerNames.Add(name);

            if (SurfaceLayerNames.Contains(tempName))
                SelectedSurfaceName = tempName;
            else if (SurfaceLayerNames.Any())
                SelectedSurfaceName = SurfaceLayerNames[0];
            else
                SelectedSurfaceName = string.Empty;

            ResetLayerNames(map);

            RaisePropertyChanged(() => SelectedSurfaceName);
            RaisePropertyChanged(() => SelectedLLOS_ObserverLyrName);
            RaisePropertyChanged(() => SelectedLLOS_TargetLyrName);
            RaisePropertyChanged(() => SelectedRLOS_ObserverLyrName);
        }

        private void ResetLayerNames(IMap map)
        {
            var layerNames = new ObservableCollection<string>();
            Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    layerNames = GetLayerNames(map);
                });

            var tempSelectedLLOS_Observer = SelectedLLOS_ObserverLyrName;
            var tempSelectedLLOS_Target = SelectedLLOS_TargetLyrName;
            var tempSelectedRLOS_Observer = SelectedRLOS_ObserverLyrName;


            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                ResetLayerCollectionNames(layerNames);
            });
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                ResetSelectedLyrName(tempSelectedLLOS_Observer, tempSelectedLLOS_Target, tempSelectedRLOS_Observer);
            });

        }

        private void ResetSelectedLyrName(string tempSelectedLLOS_Observer, string tempSelectedLLOS_Target, string tempSelectedRLOS_Observer)
        {
            if (SurfaceLayerNames.Contains(tempSelectedLLOS_Observer))
                SelectedLLOS_ObserverLyrName = tempSelectedLLOS_Observer;
            else if (SurfaceLayerNames.Any())
                SelectedLLOS_ObserverLyrName = LLOS_ObserverLyrNames[0];
            else
                SelectedLLOS_ObserverLyrName = string.Empty;


            if (SurfaceLayerNames.Contains(tempSelectedLLOS_Target))
                SelectedLLOS_TargetLyrName = tempSelectedLLOS_Target;
            else if (SurfaceLayerNames.Any())
                SelectedLLOS_TargetLyrName = LLOS_TargetLyrNames[0];
            else
                SelectedLLOS_TargetLyrName = string.Empty;

            if (SurfaceLayerNames.Contains(tempSelectedRLOS_Observer))
                SelectedRLOS_ObserverLyrName = tempSelectedRLOS_Observer;
            else if (SurfaceLayerNames.Any())
                SelectedRLOS_ObserverLyrName = RLOS_ObserverLyrNames[0];
            else
                SelectedRLOS_ObserverLyrName = string.Empty;
        }



        /// <summary>
        /// Method to handle the display coordinate type change
        /// Need to update the list boxes
        /// </summary>
        /// <param name="obj">null, not used</param>
        internal virtual void OnDisplayCoordinateTypeChanged(object obj)
        {
            var list = ObserverAddInPoints.ToList();
            ObserverAddInPoints.Clear();
            foreach (var item in list)
                ObserverAddInPoints.Add(item);
            RaisePropertyChanged(() => HasMapGraphics);
        }

        private void ResetLayerCollectionNames(ObservableCollection<string> layerNames)
        {
            LLOS_ObserverLyrNames.Clear();
            LLOS_TargetLyrNames.Clear();
            RLOS_ObserverLyrNames.Clear();

            if (!LLOS_ObserverLyrNames.Contains(EnterManullyOption))
            {
                LLOS_ObserverLyrNames.Add(EnterManullyOption);
                LLOS_TargetLyrNames.Add(EnterManullyOption);
                RLOS_ObserverLyrNames.Add(EnterManullyOption);
            }

            foreach (var layerName in layerNames)
            {
                if (!LLOS_ObserverLyrNames.Contains(layerName))
                {
                    LLOS_ObserverLyrNames.Add(layerName);
                    LLOS_TargetLyrNames.Add(layerName);
                    RLOS_ObserverLyrNames.Add(layerName);
                }
            }
        }

        private ObservableCollection<string> GetLayerNames(IMap map)
        {
            var layerNames = new ObservableCollection<string>();

            if (map == null)
                return layerNames;

            var layers = map.get_Layers();

            if (layers == null)
                return layerNames;
            var layer = layers.Next();
            while (layer != null)
            {
                var lyr = layer as FeatureClass;
                if (lyr != null && lyr.Type == esriDatasetType.esriDTFeatureClass)
                {
                    IFeatureLayer FLayer = (IFeatureLayer)lyr;
                    var geomertyType = FLayer.FeatureClass.ShapeType;
                    if (geomertyType == esriGeometryType.esriGeometryPoint)
                        layerNames.Add(layer.Name);
                }
                layer = layers.Next();
            }
            return layerNames;
        }

        internal void ValidateLLOS_LayerSelection()
        {
            if (SelectedLLOS_ObserverLyrName == EnterManullyOption)
            {
                LLOS_ObserversInExtent.Clear();
                LLOS_ObserversOutOfExtent.Clear();
            }
            if (SelectedLLOS_TargetLyrName == EnterManullyOption)
            {
                LLOS_TargetsInExtent.Clear();
                LLOS_TargetsOutOfExtent.Clear();
            }

            IsLLOSValidSelection = (
                ((SelectedLLOS_ObserverLyrName == EnterManullyOption || string.IsNullOrWhiteSpace(SelectedLLOS_ObserverLyrName))
                && LLOS_ObserversInExtent.Count == 0 && LLOS_ObserversOutOfExtent.Count == 0 && ObserverAddInPoints.Count == 0)
                ||
                ((SelectedLLOS_TargetLyrName == EnterManullyOption || string.IsNullOrWhiteSpace(SelectedLLOS_TargetLyrName))
                && LLOS_TargetsInExtent.Count == 0 && LLOS_TargetsOutOfExtent.Count == 0 && TargetAddInPoints.Count == 0)
                ) ? false : true;
        }

        internal void ValidateRLOS_LayerSelection()
        {
            IsRLOSValidSelection =
                ((SelectedRLOS_ObserverLyrName == EnterManullyOption || string.IsNullOrWhiteSpace(SelectedRLOS_ObserverLyrName))
                && RLOS_ObserversInExtent.Count == 0 && RLOS_ObserversOutOfExtent.Count == 0) ? false : true;
        }

        internal void ReadSelectedLyrPoints(ObservableCollection<AddInPointObject> inExtentPoints, ObservableCollection<AddInPointObject> outOfExtentPoints, string selectedLyrName, IColor color)
        {
            var map = ArcMap.Document.FocusMap;
            if (map == null)
                return;
            var layers = map.get_Layers();
            if (layers == null)
                return;
            var layer = layers.Next();
            while (layer != null)
            {
                if (layer.Name == selectedLyrName)
                {
                    IFeature feature = null;
                    IFeatureLayer FLayer = (IFeatureLayer)layer;
                    var cursor = FLayer.FeatureClass.Search(null, false);
                    var surface = GetSurfaceFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName);
                    double finalObserverOffset = GetOffsetInZUnits(ObserverOffset.Value, surface.ZFactor, OffsetUnitType);
                    while ((feature = cursor.NextFeature()) != null)
                    {
                        var value = feature.get_Value(FLayer.FeatureClass.FindField("Shape"));
                        var point = (IPoint)value;
                        var guid = AddGraphicToMap(point, color, true, esriSimpleMarkerStyle.esriSMSSquare);
                        var objectId = FLayer.FeatureClass.FindField("ObjectId");
                        var FID = FLayer.FeatureClass.FindField("FID");
                        var idIndex = objectId != -1 ? objectId : FID;
                        var id = Convert.ToInt32(feature.get_Value(idIndex));
                        var z1 = surface.GetElevation(point) + finalObserverOffset;
                        var addInPoint = new AddInPoint() { Point = point, GUID = guid };
                        if (double.IsNaN(z1))
                            outOfExtentPoints.Add(new AddInPointObject() { AddInPoint = addInPoint, ID = id });
                        else
                            inExtentPoints.Add(new AddInPointObject() { AddInPoint = addInPoint, ID = id });
                    }
                }
                layer = layers.Next();
            }
        }

        internal void ClearLLOSCollections()
        {
            LLOS_TargetsInExtent.Clear();
            LLOS_TargetsOutOfExtent.Clear();
            LLOS_ObserversInExtent.Clear();
            LLOS_ObserversOutOfExtent.Clear();
        }

        internal void ClearRLOSCollections()
        {
            RLOS_ObserversInExtent.Clear();
            RLOS_ObserversOutOfExtent.Clear();
        }
    }
}