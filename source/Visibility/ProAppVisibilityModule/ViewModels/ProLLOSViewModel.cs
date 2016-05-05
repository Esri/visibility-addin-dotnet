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
using System.Diagnostics;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using VisibilityLibrary.Helpers;
using ProAppVisibilityModule.Helpers;
using ProAppVisibilityModule.Models;

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
                        try
                        {
                            await CreateMapElement();

                            await Reset(true);
                        }
                        catch(Exception ex)
                        {
                            Debug.Print(ex.Message);
                        }

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

            if (point == null || !(await IsValidPoint(point)))
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
        /// </summary>
        internal override async Task CreateMapElement()
        {
            try
            {
                IsRunning = true;

                if (!CanCreateElement || MapView.Active == null || MapView.Active.Map == null || string.IsNullOrWhiteSpace(SelectedSurfaceName))
                    return;

                await ExecuteVisibilityLLOS();

                await base.CreateMapElement();
            }
            catch(Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(VisibilityLibrary.Properties.Resources.ExceptionSomethingWentWrong,
                                                                VisibilityLibrary.Properties.Resources.CaptionError);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task ExecuteVisibilityLLOS()
        {
            try
            {
                await FeatureClassHelper.CreateLayer(VisibilityLibrary.Properties.Resources.ObserversLayerName, "POINT");

                // add fields for observer offset

                await FeatureClassHelper.AddFieldToLayer(VisibilityLibrary.Properties.Resources.ObserversLayerName, VisibilityLibrary.Properties.Resources.OffsetFieldName, "DOUBLE");
                await FeatureClassHelper.AddFieldToLayer(VisibilityLibrary.Properties.Resources.ObserversLayerName, VisibilityLibrary.Properties.Resources.OffsetWithZFieldName, "DOUBLE");

                await FeatureClassHelper.CreateLayer(VisibilityLibrary.Properties.Resources.TargetsLayerName, "POINT");

                // add fields for target offset

                await FeatureClassHelper.AddFieldToLayer(VisibilityLibrary.Properties.Resources.TargetsLayerName, VisibilityLibrary.Properties.Resources.OffsetFieldName, "DOUBLE");
                await FeatureClassHelper.AddFieldToLayer(VisibilityLibrary.Properties.Resources.TargetsLayerName, VisibilityLibrary.Properties.Resources.OffsetWithZFieldName, "DOUBLE");

                // add observer points to feature layer

                await CreatingFeatures(VisibilityLibrary.Properties.Resources.ObserversLayerName, ObserverAddInPoints, ConvertFromTo(OffsetUnitType, VisibilityLibrary.DistanceTypes.Meters, ObserverOffset.Value));

                // add target points to feature layer

                await CreatingFeatures(VisibilityLibrary.Properties.Resources.TargetsLayerName, TargetAddInPoints, ConvertFromTo(OffsetUnitType, VisibilityLibrary.DistanceTypes.Meters, TargetOffset.Value));

                // update with surface information

                await FeatureClassHelper.AddSurfaceInformation(VisibilityLibrary.Properties.Resources.ObserversLayerName, SelectedSurfaceName, VisibilityLibrary.Properties.Resources.ZFieldName);
                await FeatureClassHelper.AddSurfaceInformation(VisibilityLibrary.Properties.Resources.TargetsLayerName, SelectedSurfaceName, VisibilityLibrary.Properties.Resources.ZFieldName);

                await UpdateShapeWithZ(VisibilityLibrary.Properties.Resources.ObserversLayerName, VisibilityLibrary.Properties.Resources.ZFieldName, ObserverOffset.Value);
                await UpdateShapeWithZ(VisibilityLibrary.Properties.Resources.TargetsLayerName, VisibilityLibrary.Properties.Resources.ZFieldName, TargetOffset.Value);

                await FeatureClassHelper.Delete(CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + VisibilityLibrary.Properties.Resources.SightLinesLayerName);
                await FeatureClassHelper.Delete(CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + VisibilityLibrary.Properties.Resources.LOSOutputLayerName);

                // create sight lines

                await FeatureClassHelper.CreateSightLines(VisibilityLibrary.Properties.Resources.ObserversLayerName, 
                    VisibilityLibrary.Properties.Resources.TargetsLayerName,
                    CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + VisibilityLibrary.Properties.Resources.SightLinesLayerName, 
                    VisibilityLibrary.Properties.Resources.OffsetWithZFieldName, 
                    VisibilityLibrary.Properties.Resources.OffsetWithZFieldName);

                // LOS

                await FeatureClassHelper.CreateLOS(SelectedSurfaceName, 
                    VisibilityLibrary.Properties.Resources.SightLinesLayerName,
                    CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + VisibilityLibrary.Properties.Resources.LOSOutputLayerName);

                await Reset(true);
            }
            catch(Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        private async Task CreatingFeatures(string featureClassName, ObservableCollection<AddInPoint> collection, double offset)
        {
            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName))
                    using (FeatureClassDefinition fcDefinition = enterpriseFeatureClass.GetDefinition())
                    {
                        EditOperation editOperation = new EditOperation();
                        editOperation.Callback(context =>
                        {
                            try
                            {
                                var shapeFieldName = fcDefinition.GetShapeField();

                                foreach (var item in collection)
                                {
                                    using (var rowBuffer = enterpriseFeatureClass.CreateRowBuffer())
                                    {
                                        // Either the field index or the field name can be used in the indexer.
                                        rowBuffer[VisibilityLibrary.Properties.Resources.OffsetFieldName] = offset;
                                        var point = MapPointBuilder.CreateMapPoint(item.Point.X, item.Point.Y, 0.0, item.Point.SpatialReference);
                                        rowBuffer[shapeFieldName] = point;

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

                        creationResult = await editOperation.ExecuteAsync();
                        if (!creationResult)
                            message = editOperation.ErrorMessage;

                        await Project.Current.SaveEditsAsync();
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

        private async Task UpdateShapeWithZ(string featureClassName, string zFieldName, double offsetInMeters)
        {
            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName))
                    using (FeatureClassDefinition fcDefinition = enterpriseFeatureClass.GetDefinition())
                    {
                        int zFieldIndex = fcDefinition.FindField(zFieldName);

                        EditOperation editOperation = new EditOperation();
                        editOperation.Callback(context =>
                        {
                            try
                            {
                                var shapeFieldName = fcDefinition.GetShapeField();

                                using (RowCursor rowCursor = enterpriseFeatureClass.Search(null, false))
                                {
                                    while(rowCursor.MoveNext())
                                    {
                                        using(Feature feature = (Feature)rowCursor.Current)
                                        {
                                            context.Invalidate(feature);
                                            var mp = (MapPoint)feature[shapeFieldName];
                                            var z = (Double)feature[zFieldIndex] + offsetInMeters;
                                            feature[VisibilityLibrary.Properties.Resources.OffsetWithZFieldName] = z;
                                            feature.SetShape(MapPointBuilder.CreateMapPoint(mp.X, mp.Y, z, mp.SpatialReference));

                                            feature.Store();

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

                        creationResult = await editOperation.ExecuteAsync();
                        if (!creationResult)
                            message = editOperation.ErrorMessage;

                        await Project.Current.SaveEditsAsync();
                    }
                });
                if (!creationResult)
                    MessageBox.Show(message);

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
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
