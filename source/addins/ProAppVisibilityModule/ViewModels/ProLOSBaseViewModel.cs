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

using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProAppVisibilityModule.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using VisibilityLibrary;
using VisibilityLibrary.Helpers;
using VisibilityLibrary.ViewModels;
using VisibilityLibrary.Views;

namespace ProAppVisibilityModule.ViewModels
{
    public class ProLOSBaseViewModel : ProTabBaseViewModel
    {
        public ProLOSBaseViewModel()
        {
            ObserverOffset = 2.0;
            TargetOffset = 0.0;
            OffsetUnitType = DistanceTypes.Meters;
            DistanceUnitType = DistanceTypes.Meters;
            AngularUnitType = AngularTypes.DEGREES;
            EnterManullyOption = VisibilityLibrary.Properties.Resources.EnterManuallyOption;

            ObserverAddInPoints = new ObservableCollection<AddInPoint>();
            ObserverInExtentPoints = new ObservableCollection<AddInPoint>();
            ObserverOutExtentPoints = new ObservableCollection<AddInPoint>();
            LLOS_ObserversInExtent = new ObservableCollection<AddInPointObject>();
            LLOS_ObserversOutOfExtent = new ObservableCollection<AddInPointObject>();
            LLOS_TargetsInExtent = new ObservableCollection<AddInPointObject>();
            LLOS_TargetsOutOfExtent = new ObservableCollection<AddInPointObject>();
            RLOS_ObserversInExtent = new ObservableCollection<AddInPointObject>();
            RLOS_ObserversOutOfExtent = new ObservableCollection<AddInPointObject>();
            LLOS_ObserverLyrNames = new ObservableCollection<string>();
            LLOS_TargetLyrNames = new ObservableCollection<string>();
            RLOS_ObserverLyrNames = new ObservableCollection<string>();

            ToolMode = MapPointToolMode.Unknown;
            SurfaceLayerNames = new ObservableCollection<string>();
            SelectedSurfaceName = string.Empty;

            Mediator.Register(VisibilityLibrary.Constants.DISPLAY_COORDINATE_TYPE_CHANGED, OnDisplayCoordinateTypeChanged);

            DeletePointCommand = new RelayCommand(OnDeletePointCommand);
            DeleteAllPointsCommand = new RelayCommand(OnDeleteAllPointsCommand);
            EditPropertiesDialogCommand = new RelayCommand(OnEditPropertiesDialogCommand);
            ImportCSVFileCommand = new RelayCommand(OnImportCSVFileCommand);
            PasteCoordinatesCommand = new RelayCommand(OnPasteCommand);

            // subscribe to some mapping events
            ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);
            LayersAddedEvent.Subscribe(OnLayersAdded);
            LayersRemovedEvent.Subscribe(OnLayersAdded);
            MapPropertyChangedEvent.Subscribe(OnMapPropertyChanged);
            MapMemberPropertiesChangedEvent.Subscribe(OnMapMemberPropertyChanged);
        }

        ~ProLOSBaseViewModel()
        {
            ActiveMapViewChangedEvent.Unsubscribe(OnActiveMapViewChanged);
            LayersAddedEvent.Unsubscribe(OnLayersAdded);
            LayersRemovedEvent.Unsubscribe(OnLayersAdded);
            MapPropertyChangedEvent.Unsubscribe(OnMapPropertyChanged);
        }

        #region Properties

        protected override void OnMapPointToolDeactivated(object obj)
        {
            ToolMode = MapPointToolMode.Unknown;

            base.OnMapPointToolDeactivated(obj);
        }

        public Task<bool> IsElevationSurfaceValid { get; set; }

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

                    DeactivateTool(VisibilityMapTool.ToolId);
                }
            }
        }

        public string EnterManullyOption { get; set; }

        public bool _isLLOSValidSelection { get; set; }
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
        public ObservableCollection<AddInPointObject> LLOS_TargetsInExtent { get; set; }
        public ObservableCollection<AddInPointObject> LLOS_TargetsOutOfExtent { get; set; }
        public ObservableCollection<AddInPointObject> LLOS_ObserversInExtent { get; set; }
        public ObservableCollection<AddInPointObject> LLOS_ObserversOutOfExtent { get; set; }
        public ObservableCollection<AddInPointObject> RLOS_ObserversInExtent { get; set; }
        public ObservableCollection<AddInPointObject> RLOS_ObserversOutOfExtent { get; set; }

        public ObservableCollection<AddInPoint> ObserverAddInPoints { get; set; }
        public ObservableCollection<AddInPoint> ObserverInExtentPoints { get; set; }
        public ObservableCollection<AddInPoint> ObserverOutExtentPoints { get; set; }
        public ObservableCollection<AddInPoint> TargetAddInPoints { get; set; }
        public ObservableCollection<AddInPoint> TargetInExtentPoints { get; set; }
        public ObservableCollection<AddInPoint> TargetOutExtentPoints { get; set; }
        public ObservableCollection<string> SurfaceLayerNames { get; set; }
        public string SelectedSurfaceName { get; set; }
        public DistanceTypes OffsetUnitType { get; set; }
        public DistanceTypes DistanceUnitType { get; set; }
        public AngularTypes AngularUnitType { get; set; }

        private string selectedSurfaceTooltip;
        public string SelectedSurfaceTooltip
        {
            get { return selectedSurfaceTooltip; }
            set
            {
                selectedSurfaceTooltip = value;
                RaisePropertyChanged(() => SelectedSurfaceTooltip);
            }
        }


        private System.Windows.Media.Brush selectedBorderBrush;
        public System.Windows.Media.Brush SelectedBorderBrush
        {
            get { return selectedBorderBrush; }
            set
            {
                selectedBorderBrush = value;
                RaisePropertyChanged(() => SelectedBorderBrush);
            }
        }

        #endregion

        #region Commands

        public RelayCommand DeletePointCommand { get; set; }
        public RelayCommand DeleteAllPointsCommand { get; set; }
        public RelayCommand EditPropertiesDialogCommand { get; set; }
        public RelayCommand ImportCSVFileCommand { get; set; }
        public RelayCommand PasteCoordinatesCommand { get; set; }

        /// <summary>
        /// Command method to delete points
        /// </summary>
        /// <param name="obj">List of AddInPoint</param>
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
            var dlg = new ProEditPropertiesView();

            dlg.DataContext = new EditPropertiesViewModel();

            dlg.ShowDialog();
        }

        public virtual void OnImportCSVFileCommand(object obj)
        {
            var mode = obj as string;
            CoordinateConversionLibrary.Models.CoordinateConversionLibraryConfig.AddInConfig.DisplayAmbiguousCoordsDlg = false;
            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.CheckFileExists = true;
            fileDialog.CheckPathExists = true;
            fileDialog.Filter = "csv files|*.csv";

            // attemp to import
            var fieldVM = new CoordinateConversionLibrary.ViewModels.SelectCoordinateFieldsViewModel();
            var result = fileDialog.ShowDialog();
            if (result.HasValue && result.Value == true)
            {
                var dlg = new CoordinateConversionLibrary.Views.ProSelectCoordinateFieldsView();
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
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgNoDataFound,
                                                                      VisibilityLibrary.Properties.Resources.CaptionError);
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
                                    ccType = CoordinateConversionLibrary.Models.CoordinateType.DD;
                                }
                            }
                            MapPoint point = (ccType != CoordinateConversionLibrary.Models.CoordinateType.Unknown) ? GetMapPointFromString(outFormattedString) : null;
                            if (point != null)
                            {
                                if (mode == VisibilityLibrary.Properties.Resources.ToolModeObserver)
                                {
                                    ToolMode = MapPointToolMode.Observer;
                                    Point1 = point;
                                    OnNewMapPointEvent(Point1);
                                }
                                else if (mode == VisibilityLibrary.Properties.Resources.ToolModeTarget)
                                {
                                    ToolMode = MapPointToolMode.Target;
                                    Point2 = point;
                                    OnNewMapPointEvent(Point2);
                                }
                            }
                        }
                    }
                }
            }
        }

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
                        ccType = CoordinateConversionLibrary.Models.CoordinateType.DD;
                    }
                }
                MapPoint point = (ccType != CoordinateConversionLibrary.Models.CoordinateType.Unknown) ? GetMapPointFromString(outFormattedString) : null;
                if (point != null)
                {
                    if (mode == VisibilityLibrary.Properties.Resources.ToolModeObserver)
                    {
                        ToolMode = MapPointToolMode.Observer;
                        Point1 = point;
                        OnNewMapPointEvent(Point1);
                    }
                    else if (mode == VisibilityLibrary.Properties.Resources.ToolModeTarget)
                    {
                        ToolMode = MapPointToolMode.Target;
                        Point2 = point;
                        OnNewMapPointEvent(Point2);
                    }
                }
            }
        }


        /// <summary>
        /// Method used to delete points frome the view's observer listbox
        /// </summary>
        /// <param name="observers">List of AddInPoint</param>
        private void DeletePoints(List<AddInPoint> observers)
        {
            if (observers == null || !observers.Any())
                return;

            // remove graphics from map
            var guidList = observers.Select(x => x.GUID).ToList();
            RemoveGraphics(guidList);

            // remove items from collection
            foreach (var point in observers)
            {
                ObserverAddInPoints.Remove(point);
                ObserverInExtentPoints.Remove(point);
                ObserverOutExtentPoints.Remove(point);
            }
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Override OnKeyKeyCommand to handle manual input
        /// </summary>
        /// <param name="obj"></param>
        internal async override void OnEnterKeyCommand(object obj)
        {
            var keyCommandMode = obj as string;

            if (keyCommandMode == VisibilityLibrary.Properties.Resources.ToolModeObserver)
            {
                if (!(await IsValidPoint(Point1, true)))
                    return;

                ToolMode = MapPointToolMode.Observer;
                OnNewMapPointEvent(Point1);
            }
            else if (keyCommandMode == VisibilityLibrary.Properties.Resources.ToolModeTarget)
            {
                if (!(await IsValidPoint(Point2, true)))
                    return;

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

        /// <summary>
        /// Override this event to collect observer points based on tool mode
        /// </summary>
        /// <param name="obj">MapPointToolMode</param>
        internal override async void OnNewMapPointEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            //if (string.IsNullOrEmpty(SelectedSurfaceName))
            //{
            //    MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgSurfaceLayerNotFound,
            //        VisibilityLibrary.Properties.Resources.CaptionError, MessageBoxButton.OK);
            //    return;
            //}

            //IsElevationSurfaceValid = ValidateElevationSurface(MapView.Active.Map, SelectedSurfaceName);
            //if (!await IsElevationSurfaceValid)
            //{
            //    MessageBox.Show(VisibilityLibrary.Properties.Resources.LOSDataFrameMatch, VisibilityLibrary.Properties.Resources.LOSSpatialReferenceCaption);
            //    SelectedSurfaceTooltip = VisibilityLibrary.Properties.Resources.LOSDataFrameMatch;
            //    SetErrorTemplate(false);
            //    return;
            //}

            var point = obj as MapPoint;

            // ok, we have a point
            if (point != null && ToolMode == MapPointToolMode.Observer)
            {
                if (IsMapClick)
                {
                    if (!(await IsValidPoint(point, true)))
                    {
                        IsMapClick = false;
                        return;
                    }
                }
                // in tool mode "Observer" we add observer points
                // otherwise ignore

                var guid = await AddGraphicToMap(point, ColorFactory.Instance.BlueRGB, true, 5.0);
                var addInPoint = new AddInPoint() { Point = point, GUID = guid };
                bool isValid = await IsValidPoint(point, false);
                Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!isValid)
                        {
                            ObserverOutExtentPoints.Insert(0, addInPoint);
                        }
                        else
                        {
                            ObserverInExtentPoints.Insert(0, addInPoint);
                        }

                        ObserverAddInPoints.Insert(0, addInPoint);
                    });
                IsMapClick = false;
            }

        }

        /// <summary>
        /// Method to update manual input boxes on mouse movement
        /// </summary>
        /// <param name="obj"></param>
        internal override void OnMouseMoveEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            var point = obj as MapPoint;

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
        /// Method to check to see point is withing the currently selected surface
        /// returns true if there is no surface selected or point is contained by layer AOI
        /// returns false if the point is not contained in the layer AOI
        /// </summary>
        /// <param name="point">MapPoint to validate</param>
        /// <param name="showPopup">boolean to show popup message or not</param>
        /// <returns></returns>
        internal async Task<bool> IsValidPoint(MapPoint point, bool showPopup = false)
        {
            var validPoint = true;

            if (!string.IsNullOrWhiteSpace(SelectedSurfaceName) && MapView.Active != null && MapView.Active.Map != null)
            {
                var layer = GetLayerFromMapByName(SelectedSurfaceName);

                // WORKAROUND/BUG:
                // QueryExtent() is taking several minutes to return from this call with ImageServiceLayer
                // during which MCT can't do anything, so for now just return true,
                // fix this in the future when QueryExtent() or alternate works with ImageServiceLayer
                if (layer is ImageServiceLayer)
                {
                    return true;
                }

                var env = await QueuedTask.Run(() =>
                {
                    return layer.QueryExtent();
                });

                validPoint = await IsPointWithinExtent(point, env);

                if (validPoint == false && showPopup)
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgOutOfAOI, VisibilityLibrary.Properties.Resources.CaptionError);
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
        /// <param name="point">MapPoint</param>
        /// <param name="env">Envelope</param>
        /// <returns>bool</returns>
        internal async Task<bool> IsPointWithinExtent(MapPoint point, Envelope env)
        {
            if ((point == null) || (env == null) || (env.SpatialReference == null))
                return false;

            var result = await QueuedTask.Run(() =>
                {
                    ArcGIS.Core.Geometry.Geometry projectedPoint = GeometryEngine.Instance.Project(point, env.SpatialReference);

                    return GeometryEngine.Instance.Contains(env, projectedPoint);
                });

            return result;
        }

        /// <summary>
        /// returns Layer if found in the map
        /// </summary>
        /// <param name="name">string name of layer</param>
        /// <returns>Layer</returns>
        ///
        internal Layer GetLayerFromMapByName(string name)
        {
            var layer = MapView.Active.Map.GetLayersAsFlattenedList().FirstOrDefault(l => l.Name == name);
            return layer;
        }

        /// <summary>
        /// Method used to get a list of surface layer names from the map
        /// </summary>
        /// <returns></returns>
        internal async Task<List<string>> GetSurfaceNamesFromMap()
        {
            var layerList = MapView.Active.Map.GetLayersAsFlattenedList();

            var elevationSurfaceList = await QueuedTask.Run(() =>
                {
                    var list = new List<Layer>();
                    foreach (var layer in layerList)
                    {
                        var def = layer.GetDefinition();
                        if (def != null && def.LayerType == ArcGIS.Core.CIM.MapLayerType.Operational &&
                            (def is CIMRasterLayer || def is CIMTinLayer || def is CIMLASDatasetLayer || def is CIMMosaicLayer))
                        {
                            list.Add(layer);
                        }
                    }

                    return list;
                });

            var sortedList = elevationSurfaceList.Select(l => l.Name).ToList();
            sortedList.Sort();

            return sortedList;
        }

        /// <summary>
        /// Method to get spatial reference from a feature class
        /// </summary>
        /// <param name="fcName">name of layer</param>
        /// <returns>SpatialReference</returns>
        internal async Task<SpatialReference> GetSpatialReferenceFromLayer(string layerName)
        {
            try
            {
                return await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    var layer = GetLayerFromMapByName(layerName);

                    return layer.GetSpatialReference();
                });
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Override to add aditional items in the class to reset tool
        /// </summary>
        /// <param name="toolReset"></param>
        internal override async Task Reset(bool toolReset)
        {
            try
            {
                await base.Reset(toolReset);

                if (MapView.Active == null || MapView.Active.Map == null)
                    return;

                // reset surface names OC
                await ResetSurfaceNames();
                Application.Current.Dispatcher.Invoke(() =>
                    {
                        // reset observer points
                        ObserverAddInPoints.Clear();
                        ObserverInExtentPoints.Clear();
                        ObserverOutExtentPoints.Clear();
                        ClearTempGraphics();
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Method used to reset the currently selected surfacename
        /// Use when toc items or map changes, on tab selection changed, etc
        /// </summary>
        internal async Task ResetSurfaceNames()
        {
            try
            {
                if (MapView.Active == null || MapView.Active.Map == null)
                    return;

                var names = await GetSurfaceNamesFromMap();

                // keep the current selection if it's still valid
                var tempName = SelectedSurfaceName;

                Application.Current.Dispatcher.Invoke(() =>
                    {
                        SurfaceLayerNames.Clear();
                    });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var name in names)
                        SurfaceLayerNames.Add(name);
                });
                if (SurfaceLayerNames.Contains(tempName))
                    SelectedSurfaceName = tempName;
                else if (SurfaceLayerNames.Any())
                    SelectedSurfaceName = SurfaceLayerNames[0];
                else
                    SelectedSurfaceName = string.Empty;

                await ResetLayerNames();
                RaisePropertyChanged(() => SelectedSurfaceName);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        private async Task ResetLayerNames()
        {
            var layerNames = await GetLayerNamesFromMap();

            var tempSelectedLLOS_ObserverLyr = SelectedLLOS_ObserverLyrName;
            var tempLLOS_TargetLyrNames = SelectedLLOS_TargetLyrName;
            var tempRLOS_ObserverLyrNames = SelectedRLOS_ObserverLyrName;

            ResetLayerNameCollections(layerNames);

            ResetSelectedLyrName(tempSelectedLLOS_ObserverLyr, tempLLOS_TargetLyrNames, tempRLOS_ObserverLyrNames);
        }

        private void ResetSelectedLyrName(string tempSelectedLLOS_ObserverLyr, string tempLLOS_TargetLyrNames, string tempRLOS_ObserverLyrNames)
        {
            if (LLOS_ObserverLyrNames.Contains(tempSelectedLLOS_ObserverLyr))
                SelectedLLOS_ObserverLyrName = tempSelectedLLOS_ObserverLyr;
            else if (LLOS_ObserverLyrNames.Any())
                SelectedLLOS_ObserverLyrName = LLOS_ObserverLyrNames[0];
            else
                SelectedLLOS_ObserverLyrName = string.Empty;

            if (LLOS_TargetLyrNames.Contains(tempLLOS_TargetLyrNames))
                SelectedLLOS_TargetLyrName = tempLLOS_TargetLyrNames;
            else if (LLOS_TargetLyrNames.Any())
                SelectedLLOS_TargetLyrName = LLOS_TargetLyrNames[0];
            else
                SelectedLLOS_TargetLyrName = string.Empty;

            if (RLOS_ObserverLyrNames.Contains(tempRLOS_ObserverLyrNames))
                SelectedRLOS_ObserverLyrName = tempRLOS_ObserverLyrNames;
            else if (RLOS_ObserverLyrNames.Any())
                SelectedRLOS_ObserverLyrName = RLOS_ObserverLyrNames[0];
            else
                SelectedRLOS_ObserverLyrName = string.Empty;
        }

        private void ResetLayerNameCollections(List<string> layerNames)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LLOS_ObserverLyrNames.Clear();
                LLOS_TargetLyrNames.Clear();
                RLOS_ObserverLyrNames.Clear();

                LLOS_ObserverLyrNames.Add(EnterManullyOption);
                LLOS_TargetLyrNames.Add(EnterManullyOption);
                RLOS_ObserverLyrNames.Add(EnterManullyOption);

                foreach (var name in layerNames)
                {
                    LLOS_ObserverLyrNames.Add(name);
                    LLOS_TargetLyrNames.Add(name);
                    RLOS_ObserverLyrNames.Add(name);
                }
            });
        }

        private Task<List<string>> GetLayerNamesFromMap()
        {
            return QueuedTask.Run(() =>
                {
                    var layer = new List<string>();
                    try
                    {
                        layer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
                                         .Where(l => l.ShapeType == esriGeometryType.esriGeometryPoint)
                                         .Select(x => x.ToString()).ToList();
                    }
                    catch (Exception) { }
                    return layer;
                });
        }

        /// <summary>
        /// Method to handle the display coordinate type change
        /// Need to update the list boxes
        /// </summary>
        /// <param name="obj">null, not used</param>
        internal virtual void OnDisplayCoordinateTypeChanged(object obj)
        {
            var list = ObserverAddInPoints.ToList();
            var inExtentList = ObserverInExtentPoints.ToList();
            var outExtentList = ObserverOutExtentPoints.ToList();
            ObserverAddInPoints.Clear();
            ObserverInExtentPoints.Clear();
            ObserverOutExtentPoints.Clear();

            foreach (var item in list)
                ObserverAddInPoints.Add(item);

            foreach (var item in inExtentList)
                ObserverInExtentPoints.Add(item);

            foreach (var item in outExtentList)
                ObserverOutExtentPoints.Add(item);

            RaisePropertyChanged(() => HasMapGraphics);
        }

        /// <summary>
        /// Method used to convert offset value to surface z units
        /// </summary>
        /// <param name="sr">spatial reference of z unit</param>
        /// <param name="value">value to be converted into z units</param>
        /// <returns>value is z units</returns>
        internal double GetAsMapZUnits(SpatialReference sr, double value)
        {
            double result = value;

            Unit unit = null;
            // try to get map Z unit
            try
            {
                unit = sr.ZUnit;
            }
            catch (Exception ex)
            {
                // do nothing - catching this since accessing the ZUnit crashes when not set
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            // default to map linear unit
            if (unit == null)
                unit = sr.Unit;

            // get linear unit of selected offset unit type
            var offsetLinearUnit = GetLinearUnit(OffsetUnitType);

            // convert to map z unit
            result = offsetLinearUnit.ConvertTo(value, unit as LinearUnit);

            return result;
        }

        internal double GetAsMapUnits(SpatialReference sr, double value)
        {
            double result = value;

            // get map unit
            var mapUnit = sr.Unit as LinearUnit;

            if (mapUnit == null)
                return result;

            var offsetLinearUnit = GetLinearUnit(DistanceUnitType);

            result = offsetLinearUnit.ConvertTo(value, mapUnit);

            return result;
        }

        /// <summary>
        /// Handler for active map view changed event
        /// </summary>
        /// <param name="obj"></param>
        private async void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs obj)
        {
            await ResetSurfaceNames();
        }

        /// <summary>
        /// Handler for when layers are added or removed
        /// </summary>
        /// <param name="obj"></param>
        private async void OnLayersAdded(LayerEventsArgs obj)
        {
            await ResetSurfaceNames();
        }

        private async void OnMapPropertyChanged(MapPropertyChangedEventArgs obj)
        {
            await ResetSurfaceNames();

            IsElevationSurfaceValid = ValidateElevationSurface(MapView.Active.Map, SelectedSurfaceName);
            if (!await IsElevationSurfaceValid && !string.IsNullOrWhiteSpace(SelectedSurfaceName))
                SetErrorTemplate(false);
            else
                SetErrorTemplate(true);
        }

        private async void OnMapMemberPropertyChanged(MapMemberPropertiesChangedEventArgs obj)
        {
            IEnumerable<MapMemberEventHint> mapMemberHint = obj.EventHints;
            if (mapMemberHint.ElementAt(0).ToString() == "Name")
                await ResetSurfaceNames();
        }

        internal LinearUnit GetLinearUnit(DistanceTypes dtype)
        {
            LinearUnit result = LinearUnit.Meters;
            switch (dtype)
            {
                case DistanceTypes.Feet:
                    result = LinearUnit.Feet;
                    break;
                case DistanceTypes.Kilometers:
                    result = LinearUnit.Kilometers;
                    break;
                case DistanceTypes.Miles:
                    result = LinearUnit.Miles;
                    break;
                case DistanceTypes.NauticalMile:
                    result = LinearUnit.NauticalMiles;
                    break;
                case DistanceTypes.Yards:
                    result = LinearUnit.Yards;
                    break;
                case DistanceTypes.Meters:
                default:
                    result = LinearUnit.Meters;
                    break;
            }
            return result;
        }

        internal void ValidateLLOS_LayerSelection()
        {
            IsLLOSValidSelection = (
                ((SelectedLLOS_ObserverLyrName == EnterManullyOption || string.IsNullOrWhiteSpace(SelectedLLOS_ObserverLyrName))
                && ObserverAddInPoints.Count == 0 && LLOS_ObserversInExtent.Count == 0 && LLOS_ObserversOutOfExtent.Count == 0)
                ||
                ((SelectedLLOS_TargetLyrName == EnterManullyOption || string.IsNullOrWhiteSpace(SelectedLLOS_TargetLyrName))
                && TargetAddInPoints.Count == 0 && LLOS_TargetsInExtent.Count == 0 && LLOS_TargetsOutOfExtent.Count == 0)
                ) ? false : true;
        }

        internal void ValidateRLOS_LayerSelection()
        {
            IsRLOSValidSelection =
                ((SelectedRLOS_ObserverLyrName == EnterManullyOption || string.IsNullOrWhiteSpace(SelectedRLOS_ObserverLyrName))
                && ObserverAddInPoints.Count == 0 && RLOS_ObserversInExtent.Count == 0 && RLOS_ObserversOutOfExtent.Count == 0) ? false : true;
        }

        internal async Task<Envelope> GetSurfaceEnvelope()
        {
            if (!string.IsNullOrWhiteSpace(SelectedSurfaceName) && MapView.Active != null && MapView.Active.Map != null)
            {
                var selectedSurface = GetLayerFromMapByName(SelectedSurfaceName);

                // WORKAROUND/BUG:
                // QueryExtent() is taking several minutes to return from this call with ImageServiceLayer
                // during which MCT can't do anything, so for now just return true,
                // fix this in the future when QueryExtent() or alternate works with ImageServiceLayer
                if (selectedSurface is ImageServiceLayer)
                {
                    return null;
                }

                var envelope = await QueuedTask.Run(() =>
                {
                    return selectedSurface.QueryExtent();
                });
                return envelope;
            }
            return null;
        }

        internal async void ReadPointFromLayer(Envelope surfaceEnvelope, ObservableCollection<AddInPointObject> inExtentPoints,
            ObservableCollection<AddInPointObject> outOfExtentPoints, string selectedLayerName, List<long> selectedFeaturesCollections, string tag = "")
        {
            if (selectedLayerName != EnterManullyOption && !string.IsNullOrWhiteSpace(selectedLayerName))
            {
                var layer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
                .Where(lyr => lyr.Name == selectedLayerName && lyr.ShapeType == esriGeometryType.esriGeometryPoint).FirstOrDefault();
                var cursor = layer.GetFeatureClass().Search();

                while (cursor.MoveNext())
                {
                    var point = (MapPoint)cursor.Current["Shape"];
                    var addInPoint = new AddInPoint { Point = point, GUID = Guid.NewGuid().ToString() };
                    var objectId = -1;
                    var FID = -1;
                    try
                    {
                        objectId = Convert.ToInt32(cursor.Current["ObjectId"]);
                    }
                    catch (Exception) { }
                    try
                    {
                        FID = Convert.ToInt32(cursor.Current["FID"]);
                    }
                    catch (Exception) { }

                    var ID = objectId != -1 ? objectId : FID;
                    var isWithinEntent = await IsPointWithinExtent(point, surfaceEnvelope);
                    if (selectedFeaturesCollections == null || !selectedFeaturesCollections.Any() ||
                        (selectedFeaturesCollections.Any() && selectedFeaturesCollections.Where(x => Convert.ToInt32(x) == ID).Any()))
                    {
                        if (isWithinEntent)
                            inExtentPoints.Add(new AddInPointObject() { ID = ID, AddInPoint = addInPoint });
                        else
                            outOfExtentPoints.Add(new AddInPointObject() { ID = ID, AddInPoint = addInPoint });
                    }
                }
            }
        }

        internal async Task<bool> ValidateElevationSurface(Map map, string selectedSurfaceName)
        {
            if (string.IsNullOrWhiteSpace(selectedSurfaceName))
                return false;
            return await QueuedTask.Run(() =>
            {
                var surfaceSR = GetSpatialReferenceFromLayer(selectedSurfaceName);
                return map.SpatialReference.Wkid == surfaceSR.Result.Wkid;
            });
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

        internal void SetErrorTemplate(bool isDefaultColor)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isDefaultColor)
                {
                    var themeColor = Application.Current.Resources["Esri_BorderBrush"].ToString();
                    Color color = (Color)ColorConverter.ConvertFromString(themeColor);
                    SelectedBorderBrush = Brushes.Transparent;
                    SelectedSurfaceTooltip = "Select Surface";
                }
                else
                {
                    SelectedBorderBrush = new SolidColorBrush(Colors.Red);
                    SelectedSurfaceTooltip = VisibilityLibrary.Properties.Resources.LOSDataFrameMatchError;
                }
            });
        }
    }
}
