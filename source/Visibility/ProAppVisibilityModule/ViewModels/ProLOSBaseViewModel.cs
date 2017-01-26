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
using System.Collections.ObjectModel;
using System.Collections;
using System.Windows;
using System.Threading.Tasks;
using System.Diagnostics;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping.Events;
using VisibilityLibrary;
using VisibilityLibrary.Helpers;
using VisibilityLibrary.Views;
using VisibilityLibrary.ViewModels;
using ProAppVisibilityModule.Models;
using ArcGIS.Core.CIM;

namespace ProAppVisibilityModule.ViewModels
{
    public class ProLOSBaseViewModel : ProTabBaseViewModel
    {
        public ProLOSBaseViewModel()
        {
            ObserverOffset = 2.0;
            TargetOffset = 0.0;
            OffsetUnitType = DistanceTypes.Meters;
            AngularUnitType = AngularTypes.DEGREES;

            ObserverAddInPoints = new ObservableCollection<AddInPoint>();
            
            ToolMode = MapPointToolMode.Unknown;
            SurfaceLayerNames = new ObservableCollection<string>();
            SelectedSurfaceName = string.Empty;

            Mediator.Register(VisibilityLibrary.Constants.DISPLAY_COORDINATE_TYPE_CHANGED, OnDisplayCoordinateTypeChanged);

            DeletePointCommand = new RelayCommand(OnDeletePointCommand);
            DeleteAllPointsCommand = new RelayCommand(OnDeleteAllPointsCommand);
            EditPropertiesDialogCommand = new RelayCommand(OnEditPropertiesDialogCommand);

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
        public MapPointToolMode ToolMode { get; set; }
        public ObservableCollection<AddInPoint> ObserverAddInPoints { get; set; }
        public ObservableCollection<string> SurfaceLayerNames { get; set; }
        public string SelectedSurfaceName { get; set; }
        public DistanceTypes OffsetUnitType { get; set; }
        public AngularTypes AngularUnitType { get; set; }

        #endregion

        #region Commands

        public RelayCommand DeletePointCommand { get; set; }
        public RelayCommand DeleteAllPointsCommand { get; set; }
        public RelayCommand EditPropertiesDialogCommand { get; set; }

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
            var dlg = new EditPropertiesView();

            dlg.DataContext = new EditPropertiesViewModel();

            dlg.ShowDialog();
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
            }
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Override OnKeyKeyCommand to handle manual input
        /// </summary>
        /// <param name="obj"></param>
        internal override void OnEnterKeyCommand(object obj)
        {
            var keyCommandMode = obj as string;

            if(keyCommandMode == VisibilityLibrary.Properties.Resources.ToolModeObserver)
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
        /// Override this method to implement a "Mode" to separate the input of
        /// observer points and target points
        /// </summary>
        /// <param name="obj">ToolMode string from resource file</param>
        internal override void OnActivateToolCommand(object obj)
        {
            var mode = obj.ToString();
            ToolMode = MapPointToolMode.Unknown;

            if (string.IsNullOrWhiteSpace(mode))
                return;

            if (mode == VisibilityLibrary.Properties.Resources.ToolModeObserver)
                ToolMode = MapPointToolMode.Observer;
            else if (mode == VisibilityLibrary.Properties.Resources.ToolModeTarget)
                ToolMode = MapPointToolMode.Target;

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

            var point = obj as MapPoint;

            if (point == null || !(await IsValidPoint(point, true)))
                return;

            // ok, we have a point
            if (ToolMode == MapPointToolMode.Observer)
            {
                // in tool mode "Observer" we add observer points
                // otherwise ignore
                
                var guid = await AddGraphicToMap(point, ColorFactory.Blue, true, 5.0);
                var addInPoint = new AddInPoint() { Point = point, GUID = guid };
                Application.Current.Dispatcher.Invoke(() =>
                    {
                        ObserverAddInPoints.Insert(0, addInPoint);
                    });
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
                var env = await QueuedTask.Run(() =>
                    {
                        return layer.QueryExtent();
                    });
                validPoint = await IsPointWithinExtent(point, env);

                if (validPoint == false && showPopup)
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgOutOfAOI,
                                                                        VisibilityLibrary.Properties.Resources.CaptionError);
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
            var result = await QueuedTask.Run(() =>
                {
                    return GeometryEngine.Contains(env, point);
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
                    foreach(var layer in layerList)
                    {
                        var def = layer.GetDefinition();
                        if(def != null && def.LayerType == ArcGIS.Core.CIM.MapLayerType.Operational && 
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
                    
                        ClearTempGraphics();
                    });
            }
            catch(Exception ex)
            {

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

                RaisePropertyChanged(() => SelectedSurfaceName);
            }
            catch(Exception ex)
            {
                Debug.Print(ex.Message);
            }
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
            catch 
            {
                // do nothing
                // catching this since accessing the ZUnit crashes when not set
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

            var offsetLinearUnit = GetLinearUnit(OffsetUnitType);

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
        }

        private async void OnMapMemberPropertyChanged(MapMemberPropertiesChangedEventArgs obj)
        {
            IEnumerable<MapMemberEventHint> mapMemberHint = obj.EventHints;
            if (mapMemberHint.ElementAt(0).ToString() == "Name")
                await ResetSurfaceNames();
        }


        //internal double ConvertFromTo(DistanceTypes fromType, DistanceTypes toType, double input)
        //{
        //    double result = 0.0;

        //    var linearUnitFrom = GetLinearUnit(fromType);
        //    var linearUnitTo = GetLinearUnit(toType);

        //    var unit = LinearUnit.CreateLinearUnit(linearUnitFrom.FactoryCode);

        //    result = unit.ConvertTo(input, linearUnitTo);

        //    return result;
        //}

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

    }
}
