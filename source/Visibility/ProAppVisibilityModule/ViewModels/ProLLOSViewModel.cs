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
using ArcGIS.Desktop.Core;
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

        private int executionCounter = 0;
        
        private string _ObserversLayerName = VisibilityLibrary.Properties.Resources.LLOSObserversLayerName;
        public string ObserversLayerName
        {
            get
            {
                if (executionCounter > 0)
                {
                    _ObserversLayerName = string.Format("{0}_{1}", VisibilityLibrary.Properties.Resources.LLOSObserversLayerName, executionCounter);
                }
                return _ObserversLayerName;
            }
            set { }
        }

        private string _TargetsLayerName = VisibilityLibrary.Properties.Resources.LLOSTargetsLayerName;
        public string TargetsLayerName
        {
            get
            {
                if (executionCounter > 0)
                {
                    _TargetsLayerName = string.Format("{0}_{1}", VisibilityLibrary.Properties.Resources.LLOSTargetsLayerName, executionCounter);
                }
                return _TargetsLayerName;
            }
            set { }
        }

        private string _OutputLayerName = VisibilityLibrary.Properties.Resources.LLOSOutputLayerName;
        public string OutputLayerName
        {
            get
            {
                if (executionCounter > 0)
                {
                    _OutputLayerName = string.Format("{0}_{1}", VisibilityLibrary.Properties.Resources.LLOSOutputLayerName, executionCounter);
                }
                return _OutputLayerName;
            }
            set { }
        }

        private string _SightLinesLayerName = VisibilityLibrary.Properties.Resources.LLOSSightLinesLayerName;
        public string SightLinesLayerName
        {
            get
            {
                if (executionCounter > 0)
                {
                    _SightLinesLayerName = string.Format("{0}_{1}", VisibilityLibrary.Properties.Resources.LLOSSightLinesLayerName, executionCounter);
                }
                return _SightLinesLayerName;
            }
            set { }
        }

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

                            //await Reset(true);
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
                var guid = await AddGraphicToMap(point, ColorFactory.Red, true, 5.0, markerStyle: SimpleMarkerStyle.Square, tag: "target");
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

                DeactivateTool("ProAppVisibilityModule_MapTool");

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                //await base.CreateMapElement();
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
                var surfaceSR = await GetSpatialReferenceFromLayer(SelectedSurfaceName);

                if (surfaceSR == null || !surfaceSR.IsProjected)
                {
                    MessageBox.Show(VisibilityLibrary.Properties.Resources.RLOSUserPrompt, VisibilityLibrary.Properties.Resources.RLOSUserPromptCaption);
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TargetAddInPoints.Clear();
                        ObserverAddInPoints.Clear();
                        ClearTempGraphics();
                    });

                    await Reset(true);
                    
                    return;
                }

                await FeatureClassHelper.CreateLayer(ObserversLayerName, "POINT", true, true);

                // add fields for observer offset

                await FeatureClassHelper.AddFieldToLayer(ObserversLayerName, VisibilityLibrary.Properties.Resources.OffsetFieldName, "DOUBLE");
                await FeatureClassHelper.AddFieldToLayer(ObserversLayerName, VisibilityLibrary.Properties.Resources.OffsetWithZFieldName, "DOUBLE");
                await FeatureClassHelper.AddFieldToLayer(ObserversLayerName, VisibilityLibrary.Properties.Resources.TarIsVisFieldName, "SHORT");

                await FeatureClassHelper.CreateLayer(TargetsLayerName, "POINT", true, true);

                // add fields for target offset

                await FeatureClassHelper.AddFieldToLayer(TargetsLayerName, VisibilityLibrary.Properties.Resources.OffsetFieldName, "DOUBLE");
                await FeatureClassHelper.AddFieldToLayer(TargetsLayerName, VisibilityLibrary.Properties.Resources.OffsetWithZFieldName, "DOUBLE");
                await FeatureClassHelper.AddFieldToLayer(TargetsLayerName, VisibilityLibrary.Properties.Resources.NumOfObserversFieldName, "SHORT");

                // add observer points to feature layer

                await FeatureClassHelper.CreatingFeatures(ObserversLayerName, ObserverAddInPoints, GetAsMapZUnits(surfaceSR, ObserverOffset.Value));

                // add target points to feature layer

                await FeatureClassHelper.CreatingFeatures(TargetsLayerName, TargetAddInPoints, GetAsMapZUnits(surfaceSR, TargetOffset.Value));

                // update with surface information

                await FeatureClassHelper.AddSurfaceInformation(ObserversLayerName, SelectedSurfaceName, VisibilityLibrary.Properties.Resources.ZFieldName);
                await FeatureClassHelper.AddSurfaceInformation(TargetsLayerName, SelectedSurfaceName, VisibilityLibrary.Properties.Resources.ZFieldName);

                await FeatureClassHelper.UpdateShapeWithZ(ObserversLayerName, VisibilityLibrary.Properties.Resources.ZFieldName, GetAsMapZUnits(surfaceSR, ObserverOffset.Value));
                await FeatureClassHelper.UpdateShapeWithZ(TargetsLayerName, VisibilityLibrary.Properties.Resources.ZFieldName, GetAsMapZUnits(surfaceSR, TargetOffset.Value));

                // create sight lines
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                await FeatureClassHelper.CreateSightLines(ObserversLayerName,
                    TargetsLayerName,
                    CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + SightLinesLayerName, 
                    VisibilityLibrary.Properties.Resources.OffsetWithZFieldName, 
                    VisibilityLibrary.Properties.Resources.OffsetWithZFieldName);

                // LOS
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                await FeatureClassHelper.CreateLOS(SelectedSurfaceName,
                    CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + SightLinesLayerName,
                    CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + OutputLayerName);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // join fields with sight lines

                await FeatureClassHelper.JoinField(CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + SightLinesLayerName,
                                                    "OID",
                                                    CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + OutputLayerName,
                                                    "SourceOID",
                                                    new string[] { "TarIsVis" });

                // gather results for updating observer and target layers
                var sourceOIDs = await FeatureClassHelper.GetSourceOIDs(OutputLayerName);

                //if (sourceOIDs.Count > 0)
                //{
                    var visStats = await FeatureClassHelper.GetVisibilityStats(sourceOIDs, SightLinesLayerName);

                    await FeatureClassHelper.UpdateLayersWithVisibilityStats(visStats, ObserversLayerName, TargetsLayerName);

                //}

                var observersLayer = GetLayerFromMapByName(ObserversLayerName) as FeatureLayer;
                var targetsLayer = GetLayerFromMapByName(TargetsLayerName) as FeatureLayer;
                var sightLinesLayer = GetLayerFromMapByName(SightLinesLayerName) as FeatureLayer;
                var outputLayer = GetLayerFromMapByName(OutputLayerName) as FeatureLayer;

                if (observersLayer != null && targetsLayer != null && sightLinesLayer != null && outputLayer != null)
                {
                    await FeatureClassHelper.CreateObserversRenderer(GetLayerFromMapByName(ObserversLayerName) as FeatureLayer);

                    await FeatureClassHelper.CreateTargetsRenderer(GetLayerFromMapByName(TargetsLayerName) as FeatureLayer);

                    await FeatureClassHelper.CreateTargetLayerLabels(GetLayerFromMapByName(TargetsLayerName) as FeatureLayer);

                    await FeatureClassHelper.CreateVisCodeRenderer(GetLayerFromMapByName(SightLinesLayerName) as FeatureLayer,
                                                                   VisibilityLibrary.Properties.Resources.TarIsVisFieldName,
                                                                   1,
                                                                   0,
                                                                   ColorFactory.WhiteRGB,
                                                                   ColorFactory.BlackRGB,
                                                                   6.0,
                                                                   6.0);

                    await FeatureClassHelper.CreateVisCodeRenderer(GetLayerFromMapByName(OutputLayerName) as FeatureLayer,
                                                                   VisibilityLibrary.Properties.Resources.VisCodeFieldName,
                                                                   1,
                                                                   2,
                                                                   ColorFactory.GreenRGB,
                                                                   ColorFactory.RedRGB,
                                                                   5.0,
                                                                   3.0);
                    //await Reset(true);

                    //string groupName = "LLOS Group";
                    //if (executionCounter > 0)
                    //    groupName = string.Format("{0}_{1}", groupName, executionCounter.ToString());

                    //await FeatureClassHelper.CreateGroupLayer(layerList, groupName);

                    // for now we are not resetting after a run of the tool
                    //await Reset(true);
                    executionCounter++;
                }
                else
                {
                    MessageBox.Show("LLOS computations did not complete correctly.  Please try again by selecting the 'OK' button.");
                }
                
            }
            catch(Exception ex)
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
