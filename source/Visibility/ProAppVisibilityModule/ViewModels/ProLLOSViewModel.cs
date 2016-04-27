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
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using VisibilityLibrary.Helpers;
using ArcMapAddinVisibility.Models;
using ProAppVisibilityModule.Helpers;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System.Diagnostics;

namespace ProAppVisibilityModule.ViewModels
{
    public class ProLLOSViewModel : ProLOSBaseViewModel
    {
        public ProLLOSViewModel()
        {
            TargetAddInPoints = new ObservableCollection<AddInPoint>();
            IsActiveTab = true;

            // commands
            SubmitCommand = new RelayCommand(async (obj) => 
            {
                try
                {
                    await OnSubmitCommand(obj);
                }
                catch(Exception ex)
                {
                    Debug.Print(ex.Message);
                }
            });
        }

        #region Properties

        public ObservableCollection<AddInPoint> TargetAddInPoints { get; set; }

        #endregion

        #region Commands

        public RelayCommand SubmitCommand { get; set; }

        /// <summary>
        /// Method to handle the Submit/OK button command
        /// </summary>
        /// <param name="obj">null</param>
        private async Task OnSubmitCommand(object obj)
        {
            try
            {

                await Task.Run(async () =>
                    {
                        // TODO udpate wait cursor/progressor
                        //var savedCursor = System.Windows.Forms.Cursor.Current;
                        //System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                        //System.Windows.Forms.Application.DoEvents();
                        // promote temp graphics
                        //MoveTempGraphicsToMapGraphics();
                        try
                        {
                            await CreateMapElement();

                            await Reset(true);
                        }
                        catch(Exception ex)
                        {
                            Debug.Print(ex.Message);
                        }

                        //System.Windows.Forms.Cursor.Current = savedCursor;
                    });
            }
            catch(Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        /// <summary>
        /// Method override to handle deletion of points
        /// Here we need to do target points in addition to observers
        /// </summary>
        /// <param name="obj">List of AddInPoint</param>
        internal override void OnDeletePointCommand(object obj)
        {
            // take care of ObserverPoints
            base.OnDeletePointCommand(obj);

            // now lets take care of Target Points
            var items = obj as IList;
            var targets = items.Cast<AddInPoint>().ToList();

            if (targets == null)
                return;

            DeleteTargetPoints(targets);
        }

        /// <summary>
        /// Method used to delete target points
        /// </summary>
        /// <param name="targets">List<AddInPoint></param>
        private void DeleteTargetPoints(List<AddInPoint> targets)
        {
            if (targets == null || !targets.Any())
                return;

            // remove map graphics
            var guidList = targets.Select(x => x.GUID).ToList();
            RemoveGraphics(guidList);

            // remove from collection
            foreach (var obj in targets)
            {
                TargetAddInPoints.Remove(obj);
            }
        }

        /// <summary>
        /// Method used to delete all points
        /// Here we need to handle target points in addition to observers
        /// </summary>
        /// <param name="obj"></param>
        internal override void OnDeleteAllPointsCommand(object obj)
        {
            var mode = obj.ToString();

            if (string.IsNullOrWhiteSpace(mode))
                return;

            if (mode == VisibilityLibrary.Properties.Resources.ToolModeObserver)
                base.OnDeleteAllPointsCommand(obj);
            else if (mode == VisibilityLibrary.Properties.Resources.ToolModeTarget)
                DeleteTargetPoints(TargetAddInPoints.ToList());
        }

        #endregion

        /// <summary>
        /// Method override to handle new map points
        /// Here we must take of targets in addition to observers
        /// </summary>
        /// <param name="obj">MapPoint</param>
        internal override async void OnNewMapPointEvent(object obj)
        {
            base.OnNewMapPointEvent(obj);

            if (!IsActiveTab)
                return;

            var point = obj as MapPoint;

            if (point == null || !IsValidPoint(point).Result)
                return;

            if (ToolMode == MapPointToolMode.Target)
            {
                var guid = await AddGraphicToMap(point, ColorFactory.Red, true, 5.0, markerStyle: SimpleMarkerStyle.Square);
                var addInPoint = new AddInPoint() { Point = point, GUID = guid };
                Application.Current.Dispatcher.Invoke(() =>
                    {
                        TargetAddInPoints.Insert(0, addInPoint);
                    });
            }
        }

        /// <summary>
        /// Method override reset to include TargetAddInPoints
        /// </summary>
        /// <param name="toolReset"></param>
        internal override async Task Reset(bool toolReset)
        {
            try
            {
                await base.Reset(toolReset);

                if (MapView.Active == null || MapView.Active.Map == null)
                    return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // reset target points
                    TargetAddInPoints.Clear();
                });
            }
            catch(Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        /// <summary>
        /// Method override to determine if we can execute tool
        /// Check for surface, observers, targets, and offsets
        /// </summary>
        public override bool CanCreateElement
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(SelectedSurfaceName) 
                    && ObserverAddInPoints.Any() 
                    && TargetAddInPoints.Any()
                    && TargetOffset.HasValue
                    && ObserverOffset.HasValue);
            }
        }

        /// <summary>
        /// Here we need to create the lines of sight and determine is a target can be seen or not
        /// Visualize the visible targets with GREEN circles
        /// Visualize the non visible targets with RED circles
        /// Visualize the number of observers that can see a target with a label #
        /// Visualize an observer that can see no targets with a RED circle on top of a BLUE circle
        /// Visualize an observer that can see at least one target with a GREEN circle on top of a BLUE circle
        /// </summary>
        internal override async Task CreateMapElement()
        {
            try
            {
                IsRunning = true;

                //TODO update to Pro
                if (!CanCreateElement || MapView.Active == null || MapView.Active.Map == null || string.IsNullOrWhiteSpace(SelectedSurfaceName))
                    return;


                // take your observer and target points and construct sight lines

                await CreateObserverFeatureLayer();

                //TODO update to Pro
                //var surface = GetSurfaceFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName);

                //if (surface == null)
                //    return;

                //var geoBridge = new GeoDatabaseHelperClass() as IGeoDatabaseBridge2;

                //if (geoBridge == null)
                //    return;

                //IPoint pointObstruction = null;
                //IPolyline polyVisible = null;
                //IPolyline polyInvisible = null;
                //bool targetIsVisible = false;

                //double finalObserverOffset = GetOffsetInZUnits(ArcMap.Document.FocusMap, ObserverOffset.Value, surface.ZFactor, OffsetUnitType);
                //double finalTargetOffset = GetOffsetInZUnits(ArcMap.Document.FocusMap, TargetOffset.Value, surface.ZFactor, OffsetUnitType);

                //var DictionaryTargetObserverCount = new Dictionary<IPoint, int>();

                //foreach (var observerPoint in ObserverAddInPoints)
                //{
                //    // keep track of visible targets for this observer
                //    var CanSeeAtLeastOneTarget = false;

                //    var z1 = surface.GetElevation(observerPoint.Point) + finalObserverOffset;

                //    if (surface.IsVoidZ(z1))
                //    {
                //        if (double.IsNaN(z1))
                //            z1 = 0.000001;
                //    }

                //    foreach (var targetPoint in TargetAddInPoints)
                //    {
                //        var z2 = surface.GetElevation(targetPoint.Point) + finalTargetOffset;

                //        if (surface.IsVoidZ(z2))
                //        {
                //            if (double.IsNaN(z2))
                //                z2 = 0.000001;
                //        }

                //        var fromPoint = new PointClass() { Z = z1, X = observerPoint.Point.X, Y = observerPoint.Point.Y, ZAware = true } as IPoint;
                //        var toPoint = new PointClass() { Z = z2, X = targetPoint.Point.X, Y = targetPoint.Point.Y, ZAware = true } as IPoint;

                //        geoBridge.GetLineOfSight(surface, fromPoint, toPoint,
                //            out pointObstruction, out polyVisible, out polyInvisible, out targetIsVisible, false, false);

                //        // set the flag if we can see at least one target
                //        if (targetIsVisible)
                //        {
                //            CanSeeAtLeastOneTarget = true;

                //            // update target observer count
                //            UpdateTargetObserverCount(DictionaryTargetObserverCount, targetPoint.Point);
                //        }

                //        if (polyVisible != null)
                //        {
                //            AddGraphicToMap(polyVisible, new RgbColorClass() { Green = 255 });
                //        }

                //        if (polyInvisible != null)
                //        {
                //            AddGraphicToMap(polyInvisible, new RgbColorClass() { Red = 255 });
                //        }

                //        if (polyVisible == null && polyInvisible == null)
                //        {
                //            var pcol = new PolylineClass() as IPointCollection;
                //            pcol.AddPoint(fromPoint);
                //            pcol.AddPoint(toPoint);

                //            if (targetIsVisible)
                //                AddGraphicToMap(pcol as IPolyline, new RgbColorClass() { Green = 255 });
                //            else
                //                AddGraphicToMap(pcol as IPolyline, new RgbColorClass() { Red = 255 });
                //        }
                //    }

                //    // visualize observer

                //    // add blue dot
                //    AddGraphicToMap(observerPoint.Point, new RgbColorClass() { Blue = 255 }, size: 10);

                //    if (CanSeeAtLeastOneTarget)
                //    {
                //        // add green dot
                //        AddGraphicToMap(observerPoint.Point, new RgbColorClass() { Green = 255 });
                //    }
                //    else
                //    {
                //        // add red dot
                //        AddGraphicToMap(observerPoint.Point, new RgbColorClass() { Red = 255 });
                //    }
                //}

                //VisualizeTargets(DictionaryTargetObserverCount);

                await base.CreateMapElement();
            }
            catch(Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(VisibilityLibrary.Properties.Resources.ExceptionSomethingWentWrong);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task CreateObserverFeatureLayer()
        {
            try
            {
                await FeatureClassHelper.CreateLayer("vis_observers", "POINT");

                // add fields for observer offset

                await FeatureClassHelper.AddFieldToLayer("vis_observers", "offset", "DOUBLE");

                // add observer points to feature layer

                await CreatingObserverFeatures();

                //ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                //{
                //    //Create the edit operation
                //    var createOperation = new ArcGIS.Desktop.Editing.EditOperation();
                //    createOperation.Name = "Generate observer points";
                //    createOperation.SelectNewFeatures = false;

                //    //Loop through csv data
                //    foreach (var item in ObserverAddInPoints)
                //    {
                //        //Create the point geometry
                //        //ArcGIS.Core.Geometry.MapPoint newMapPoint = ArcGIS.Core.Geometry.MapPointBuilder.CreateMapPoint(item.X, item.Y);

                //        // include the attributes via a dictionary
                //        var atts = new Dictionary<string, object>();
                //        atts.Add("offset", ObserverOffset.Value);
                //        //TODO get shape field name
                //        atts.Add("Shape", item.Point);   // I know the shape field is called Shape - but dont assume

                //        // queue feature creation
                //        createOperation.Create(layer, atts);
                //    }

                //    // execute the edit (feature creation) operation
                //    return createOperation.Execute();
                //});
            }
            catch(Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        private async Task CreatingObserverFeatures()
        {
            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>("vis_observers"))
                    using (FeatureClassDefinition facilitySiteDefinition = enterpriseFeatureClass.GetDefinition())
                    {
                        EditOperation editOperation = new EditOperation();
                        editOperation.Callback(context =>
                        {
                            try
                            {
                                var shapeFieldName = facilitySiteDefinition.GetShapeField();

                                foreach (var item in ObserverAddInPoints)
                                {
                                    //int facilityIdIndex = facilitySiteDefinition.FindField("FACILITYID");
                                    using (var rowBuffer = enterpriseFeatureClass.CreateRowBuffer())
                                    {
                                        // Either the field index or the field name can be used in the indexer.
                                        rowBuffer["offset"] = ObserverOffset.Value;

                                        rowBuffer[shapeFieldName] = item.Point;

                                        using (var feature = enterpriseFeatureClass.CreateRow(rowBuffer))
                                        {
                                            //To Indicate that the attribute table has to be updated
                                            context.Invalidate(feature);
                                        }
                                    }
                                }
                            }
                            catch (GeodatabaseException exObj)
                            {
                                message = exObj.Message;
                            }
                        }, enterpriseFeatureClass);

                        //var task = editOperation.ExecuteAsync();
                        creationResult = await editOperation.ExecuteAsync(); //task.Result;
                        if (!creationResult)
                            message = editOperation.ErrorMessage;
                    }
                });
                if (!creationResult)
                    MessageBox.Show(message);

            }
            catch(Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        private void VisualizeTargets(Dictionary<MapPoint, int> dict)
        {
            // visualize targets
            foreach (var targetPoint in TargetAddInPoints)
            {
                if (dict.ContainsKey(targetPoint.Point))
                {
                    //TODO update to Pro
                    // add green circle
                    //AddGraphicToMap(targetPoint.Point, new RgbColorClass() { Green = 255 }, size: 10);
                    // add label
                    //AddTextToMap(dict[targetPoint.Point].ToString(), targetPoint.Point, new RgbColorClass(), size: 10);
                }
                else
                {
                    // add red circle
                    //AddGraphicToMap(targetPoint.Point, new RgbColorClass() { Red = 255 }, size: 10);
                }
            }
        }

        private void UpdateTargetObserverCount(Dictionary<MapPoint, int> dict, MapPoint targetPoint)
        {
            if (dict.ContainsKey(targetPoint))
            {
                dict[targetPoint] += 1;
            }
            else
            {
                dict.Add(targetPoint, 1);
            }
        }

        internal override void OnDisplayCoordinateTypeChanged(object obj)
        {
            var list = TargetAddInPoints.ToList();
            TargetAddInPoints.Clear();
            foreach (var item in list)
                TargetAddInPoints.Add(item);

            // and update observers
            base.OnDisplayCoordinateTypeChanged(obj);
        }
    }
}
