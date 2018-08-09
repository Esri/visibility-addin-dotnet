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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

// Pro SDK
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

// Visibility 
using ProAppVisibilityModule.Helpers;
using ProAppVisibilityModule.Models;
using VisibilityLibrary.Helpers;
using ArcGIS.Core.Data;
using System.Text.RegularExpressions;
using ArcGIS.Core.CIM;

namespace ProAppVisibilityModule.ViewModels
{
    public class ProLLOSViewModel : ProLOSBaseViewModel
    {
        public ProLLOSViewModel()
        {
            TargetAddInPoints = new ObservableCollection<AddInPoint>();
            TargetInExtentPoints = new ObservableCollection<AddInPoint>();
            TargetOutExtentPoints = new ObservableCollection<AddInPoint>();
            IsActiveTab = true;
            DisplayProgressBarLLOS = Visibility.Hidden;
            // commands
            SubmitCommand = new RelayCommand(async (obj) =>
            {
                try
                {
                    await OnSubmitCommand(obj);
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
            });

        }

        #region Properties


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

        private Visibility _displayProgressBar = Visibility.Collapsed;
        public Visibility DisplayProgressBarLLOS
        {
            get
            {
                return _displayProgressBar;
            }
            set
            {
                _displayProgressBar = value;
                RaisePropertyChanged(() => DisplayProgressBarLLOS);
            }
        }

        private string _FeatureDatasetName = VisibilityLibrary.Properties.Resources.LLOSFeatureDatasetName;
        public string FeatureDatasetName
        {
            get
            {
                if (executionCounter > 0)
                {
                    _FeatureDatasetName = string.Format("{0}_{1}", VisibilityLibrary.Properties.Resources.LLOSFeatureDatasetName, executionCounter);
                }
                return _FeatureDatasetName;
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
                            DisplayProgressBarLLOS = Visibility.Visible;
                            IsRunning = true;
                            await CreateMapElement();
                            //await Reset(true);
                        }
                        catch (Exception ex)
                        {
                            Debug.Print(ex.Message);
                        }
                        finally
                        {
                            DisplayProgressBarLLOS = Visibility.Hidden;
                        }

                    });
            }
            catch (Exception ex)
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
            ValidateLLOS_LayerSelection();
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
                TargetInExtentPoints.Remove(obj);
                TargetOutExtentPoints.Remove(obj);
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

            ValidateLLOS_LayerSelection();
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

            if (point != null && ToolMode == MapPointToolMode.Target)
            {
                if (IsMapClick)
                {
                    if (!(await IsValidPoint(point, true)))
                    {
                        IsMapClick = false;
                        return;
                    }
                }
                var guid = await AddGraphicToMap(point, ColorFactory.Instance.RedRGB, true, 5.0, markerStyle: SimpleMarkerStyle.Square, tag: "target");
                var addInPoint = new AddInPoint() { Point = point, GUID = guid };
                bool isValid = await IsValidPoint(point, false);
                Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!isValid)
                        {
                            TargetOutExtentPoints.Insert(0, addInPoint);
                        }
                        else
                        {
                            TargetInExtentPoints.Insert(0, addInPoint);
                        }

                        TargetAddInPoints.Insert(0, addInPoint);
                    });
                IsMapClick = false;
            }

            ValidateLLOS_LayerSelection();
            
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
                    TargetInExtentPoints.Clear();
                    TargetOutExtentPoints.Clear();
                });
            }
            catch (Exception ex)
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
                    && (ObserverAddInPoints.Any() || LLOS_ObserversInExtent.Any() || LLOS_ObserversOutOfExtent.Any())
                    && (TargetAddInPoints.Any() || LLOS_TargetsInExtent.Any() || LLOS_TargetsOutOfExtent.Any())
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

                await ReadSelectedLayers();

                if (!CanCreateElement || MapView.Active == null || MapView.Active.Map == null || string.IsNullOrWhiteSpace(SelectedSurfaceName))
                    return;


                if ((LLOS_ObserversInExtent.Any() || ObserverAddInPoints.Any())
                    && LLOS_TargetsInExtent.Any() || TargetAddInPoints.Any())
                {
                    bool success = await ExecuteVisibilityLLOS();
                    if (!success)
                        MessageBox.Show("LLOS computations did not complete correctly.\nPlease check your parameters and try again.",
                            VisibilityLibrary.Properties.Resources.CaptionError);
                }
                else
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(VisibilityLibrary.Properties.Resources.OutOfExtentMsg, VisibilityLibrary.Properties.Resources.OutOfExtentHeader);
                }

                DeactivateTool(VisibilityMapTool.ToolId);
                OnMapPointToolDeactivated(null);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                //await base.CreateMapElement();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(VisibilityLibrary.Properties.Resources.ExceptionSomethingWentWrong,
                                                                VisibilityLibrary.Properties.Resources.CaptionError);
            }
            finally
            {
                IsRunning = false;
                ClearLLOSCollections();
            }
        }

        private async Task<bool> ExecuteVisibilityLLOS()
        {
            bool success = false;

            try
            {
                // Check surface spatial reference
                var surfaceSR = await GetSpatialReferenceFromLayer(SelectedSurfaceName);
                if (surfaceSR == null || !surfaceSR.IsProjected)
                {
                    MessageBox.Show(VisibilityLibrary.Properties.Resources.RLOSUserPrompt, VisibilityLibrary.Properties.Resources.RLOSUserPromptCaption);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TargetAddInPoints.Clear();
                        ObserverAddInPoints.Clear();
                        ObserverInExtentPoints.Clear();
                        TargetInExtentPoints.Clear();
                        ObserverOutExtentPoints.Clear();
                        TargetOutExtentPoints.Clear();
                        ClearTempGraphics();
                    });

                    await Reset(true);

                    return false;
                }

                var observerPoints = new ObservableCollection<AddInPoint>(LLOS_ObserversInExtent.Select(x => x.AddInPoint).Union(ObserverInExtentPoints));
                var targetPoints = new ObservableCollection<AddInPoint>(LLOS_TargetsInExtent.Select(x => x.AddInPoint).Union(TargetInExtentPoints));
                // Warn if Image Service layer
                Layer surfaceLayer = GetLayerFromMapByName(SelectedSurfaceName);
                if (surfaceLayer is ImageServiceLayer)
                {
                    MessageBoxResult mbr = MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgLayerIsImageService,
                        VisibilityLibrary.Properties.Resources.CaptionLayerIsImageService, MessageBoxButton.YesNo);

                    if (mbr == MessageBoxResult.No)
                    {
                        System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgTryAgain, VisibilityLibrary.Properties.Resources.MsgCalcCancelled);
                        return false;
                    }
                }

                //Validate Dataframe Spatial reference with surface spatial reference
                if (MapView.Active.Map.SpatialReference.Wkid != surfaceSR.Wkid)
                {
                    MessageBox.Show(VisibilityLibrary.Properties.Resources.LOSDataFrameMatch, VisibilityLibrary.Properties.Resources.LOSSpatialReferenceCaption);
                    return false;
                }

                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(CoreModule.CurrentProject.DefaultGeodatabasePath))))
                    {
                        executionCounter = 0;
                        int featureDataSetSuffix = 0;
                        var enterpriseDefinitionNames = geodatabase.GetDefinitions<FeatureDatasetDefinition>().Where(i => i.GetName().StartsWith(VisibilityLibrary.Properties.Resources.LLOSFeatureDatasetName)).Select(i => i.GetName()).ToList();
                        foreach (var defName in enterpriseDefinitionNames)
                        {
                            int n;
                            bool isNumeric = int.TryParse(Regex.Match(defName, @"\d+$").Value, out n);
                            if (isNumeric)
                                featureDataSetSuffix = featureDataSetSuffix < n ? n : featureDataSetSuffix;
                        }
                        featureDataSetSuffix = enterpriseDefinitionNames.Count > 0 ? featureDataSetSuffix + 1 : 0;

                        var observerLyrSuffix = GetLayerSuffix(VisibilityLibrary.Properties.Resources.LLOSObserversLayerName, geodatabase);
                        var targetLyrSuffix = GetLayerSuffix(VisibilityLibrary.Properties.Resources.LLOSTargetsLayerName, geodatabase);
                        var sightLinesLyrSuffix = GetLayerSuffix(VisibilityLibrary.Properties.Resources.LLOSSightLinesLayerName, geodatabase);
                        var outputLyrSuffix = GetLayerSuffix(VisibilityLibrary.Properties.Resources.LLOSOutputLayerName, geodatabase);

                        executionCounter = new List<int> { featureDataSetSuffix, observerLyrSuffix, targetLyrSuffix, sightLinesLyrSuffix, outputLyrSuffix }.Max();
                    }
                });

                //Create Feature dataset
                success = await FeatureClassHelper.CreateFeatureDataset(FeatureDatasetName);
                if (!success)
                    return false;

                success = await FeatureClassHelper.CreateLayer(FeatureDatasetName, ObserversLayerName, "POINT", true, true);

                if (!success)
                    return false;

                // add fields for observer offset

                await FeatureClassHelper.AddFieldToLayer(ObserversLayerName, VisibilityLibrary.Properties.Resources.OffsetFieldName, "DOUBLE");
                await FeatureClassHelper.AddFieldToLayer(ObserversLayerName, VisibilityLibrary.Properties.Resources.OffsetWithZFieldName, "DOUBLE");
                await FeatureClassHelper.AddFieldToLayer(ObserversLayerName, VisibilityLibrary.Properties.Resources.TarIsVisFieldName, "SHORT");

                success = await FeatureClassHelper.CreateLayer(FeatureDatasetName, TargetsLayerName, "POINT", true, true);

                if (!success)
                    return false;

                // add fields for target offset

                await FeatureClassHelper.AddFieldToLayer(TargetsLayerName, VisibilityLibrary.Properties.Resources.OffsetFieldName, "DOUBLE");
                await FeatureClassHelper.AddFieldToLayer(TargetsLayerName, VisibilityLibrary.Properties.Resources.OffsetWithZFieldName, "DOUBLE");
                await FeatureClassHelper.AddFieldToLayer(TargetsLayerName, VisibilityLibrary.Properties.Resources.NumOfObserversFieldName, "SHORT");

                // add observer points to feature layer
                await FeatureClassHelper.CreatingFeatures(ObserversLayerName, observerPoints, GetAsMapZUnits(surfaceSR, ObserverOffset.Value));

                // add target points to feature layer
                await FeatureClassHelper.CreatingFeatures(TargetsLayerName, targetPoints, GetAsMapZUnits(surfaceSR, TargetOffset.Value));

                // update with surface information
                success = await FeatureClassHelper.AddSurfaceInformation(ObserversLayerName, SelectedSurfaceName, VisibilityLibrary.Properties.Resources.ZFieldName);
                if (!success)
                    return false;

                success = await FeatureClassHelper.AddSurfaceInformation(TargetsLayerName, SelectedSurfaceName, VisibilityLibrary.Properties.Resources.ZFieldName);
                if (!success)
                    return false;

                await FeatureClassHelper.UpdateShapeWithZ(ObserversLayerName, VisibilityLibrary.Properties.Resources.ZFieldName, GetAsMapZUnits(surfaceSR, ObserverOffset.Value));
                await FeatureClassHelper.UpdateShapeWithZ(TargetsLayerName, VisibilityLibrary.Properties.Resources.ZFieldName, GetAsMapZUnits(surfaceSR, TargetOffset.Value));

                // create sight lines
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                success = await FeatureClassHelper.CreateSightLines(ObserversLayerName,
                    TargetsLayerName,
                    CoreModule.CurrentProject.DefaultGeodatabasePath + System.IO.Path.DirectorySeparatorChar + FeatureDatasetName + System.IO.Path.DirectorySeparatorChar + SightLinesLayerName,
                    VisibilityLibrary.Properties.Resources.OffsetWithZFieldName,
                    VisibilityLibrary.Properties.Resources.OffsetWithZFieldName);

                if (!success)
                    return false;

                // LOS
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                success = await FeatureClassHelper.CreateLOS(SelectedSurfaceName,
                    CoreModule.CurrentProject.DefaultGeodatabasePath + System.IO.Path.DirectorySeparatorChar + FeatureDatasetName + System.IO.Path.DirectorySeparatorChar + SightLinesLayerName,
                    CoreModule.CurrentProject.DefaultGeodatabasePath + System.IO.Path.DirectorySeparatorChar + FeatureDatasetName + System.IO.Path.DirectorySeparatorChar + OutputLayerName);

                if (!success)
                    return false;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // join fields with sight lines

                await FeatureClassHelper.JoinField(CoreModule.CurrentProject.DefaultGeodatabasePath + System.IO.Path.DirectorySeparatorChar + FeatureDatasetName + System.IO.Path.DirectorySeparatorChar + SightLinesLayerName,
                                                    "OID",
                                                    CoreModule.CurrentProject.DefaultGeodatabasePath + System.IO.Path.DirectorySeparatorChar + FeatureDatasetName + System.IO.Path.DirectorySeparatorChar + OutputLayerName,
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

                var observerOutOfExtent = new ObservableCollection<AddInPoint>(LLOS_ObserversOutOfExtent.Select(x => x.AddInPoint).Union(ObserverOutExtentPoints));
                // add observer points present out of extent to feature layer
                await FeatureClassHelper.CreatingFeatures(ObserversLayerName, observerOutOfExtent, GetAsMapZUnits(surfaceSR, TargetOffset.Value), VisibilityLibrary.Properties.Resources.TarIsVisFieldName);

                var targetOutOfExtent = new ObservableCollection<AddInPoint>(LLOS_TargetsOutOfExtent.Select(x => x.AddInPoint).Union(TargetOutExtentPoints));
                // add target points present out of extent to feature layer
                await FeatureClassHelper.CreatingFeatures(TargetsLayerName, targetOutOfExtent, GetAsMapZUnits(surfaceSR, TargetOffset.Value), VisibilityLibrary.Properties.Resources.NumOfObserversFieldName);

                if (observersLayer != null && targetsLayer != null && sightLinesLayer != null && outputLayer != null)
                {
                    await FeatureClassHelper.CreateObserversRenderer(GetLayerFromMapByName(ObserversLayerName) as FeatureLayer);

                    await FeatureClassHelper.CreateTargetsRenderer(GetLayerFromMapByName(TargetsLayerName) as FeatureLayer);

                    await FeatureClassHelper.CreateTargetLayerLabels(GetLayerFromMapByName(TargetsLayerName) as FeatureLayer);

                    await FeatureClassHelper.CreateVisCodeRenderer(GetLayerFromMapByName(SightLinesLayerName) as FeatureLayer,
                                                                   VisibilityLibrary.Properties.Resources.TarIsVisFieldName,
                                                                   1,
                                                                   0,
                                                                   ColorFactory.Instance.WhiteRGB,
                                                                   ColorFactory.Instance.BlackRGB,
                                                                   6.0,
                                                                   6.0);

                    await FeatureClassHelper.CreateVisCodeRenderer(GetLayerFromMapByName(OutputLayerName) as FeatureLayer,
                                                                   VisibilityLibrary.Properties.Resources.VisCodeFieldName,
                                                                   1,
                                                                   2,
                                                                   ColorFactory.Instance.GreenRGB,
                                                                   ColorFactory.Instance.RedRGB,
                                                                   5.0,
                                                                   3.0);
                    //await Reset(true);

                    //string groupName = "LLOS Group";
                    //if (executionCounter > 0)
                    //    groupName = string.Format("{0}_{1}", groupName, executionCounter.ToString());

                    //await FeatureClassHelper.CreateGroupLayer(layerList, groupName);

                    // for now we are not resetting after a run of the tool
                    //await Reset(true);


                    List<Layer> lyrList = new List<Layer>();
                    lyrList.Add(observersLayer);
                    lyrList.Add(targetsLayer);
                    lyrList.Add(outputLayer);
                    lyrList.Add(sightLinesLayer);

                    await FeatureClassHelper.MoveLayersToGroupLayer(lyrList, FeatureDatasetName);
                    var envelope = await QueuedTask.Run(() => outputLayer.QueryExtent());
                    await ZoomToExtent(envelope);

                    var surfaceEnvelope = await GetSurfaceEnvelope();
                    await DisplayOutOfExtentMsg(surfaceEnvelope);
                    success = true;
                }
                else
                {
                    success = false;
                }
            }
            catch (Exception ex)
            {
                success = false;
                Debug.Print(ex.Message);
            }

            return success;
        }

        private int GetLayerSuffix(string layerName, Geodatabase geodatabase)
        {
            int counter = 0;
            var enterpriseFCNames = geodatabase.GetDefinitions<FeatureClassDefinition>().Where(i => i.GetName().StartsWith(layerName)).Select(i => i.GetName()).ToList();
            foreach (var fcName in enterpriseFCNames)
            {
                int n;
                bool isNumeric = int.TryParse(Regex.Match(fcName, @"\d+$").Value, out n);
                if (isNumeric)
                    counter = counter < n ? n : counter;
            }
            counter = enterpriseFCNames.Count > 0 ? counter + 1 : 0;
            return counter;
        }

        private async Task DisplayOutOfExtentMsg(Envelope surfaceEnvelope)
        {
            await QueuedTask.Run(() =>
            {
                var observerIDCollection = LLOS_ObserversOutOfExtent.Select(x => x.ID).ToList<int>();
                var targetIDCollection = LLOS_TargetsOutOfExtent.Select(x => x.ID).ToList<int>();
                var observerString = string.Empty;
                var targetString = string.Empty;
                foreach (var item in observerIDCollection)
                {
                    if (observerString == "")
                        observerString = item.ToString();
                    else
                        observerString = observerString + "," + item.ToString();
                }
                foreach (var item in targetIDCollection)
                {
                    if (targetString == "")
                        targetString = item.ToString();
                    else
                        targetString = targetString + "," + item.ToString();
                }
                if (observerIDCollection.Any() || targetIDCollection.Any())
                {
                    if ((observerIDCollection.Count + targetIDCollection.Count) <= 10)
                    {
                        var msgString = string.Empty;
                        if (observerIDCollection.Any())
                        {
                            msgString = "Observers lying outside the extent of elevation surface are: " + observerString;
                        }
                        if (targetIDCollection.Any())
                        {
                            if (msgString != "")
                                msgString = msgString + "\n";
                            msgString = msgString + "Targets lying outside the extent of elevation surface are: " + targetString;
                        }
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(msgString,
                                                    "Unable To Process For Few Locations");
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(VisibilityLibrary.Properties.Resources.LLOSPointsOutsideOfSurfaceExtent,
                        VisibilityLibrary.Properties.Resources.MsgCalcCancelled);
                    }
                }
            });
        }

        private async Task ReadSelectedLayers()
        {
            LLOS_ObserversInExtent.Clear();
            LLOS_ObserversOutOfExtent.Clear();
            LLOS_TargetsInExtent.Clear();
            LLOS_TargetsOutOfExtent.Clear();

            var surfaceEnvelope = await GetSurfaceEnvelope();
            var selectedFeatures = await QueuedTask.Run(() => { return MapView.Active.Map.GetSelection(); });
            await QueuedTask.Run(() =>
            {
                var selectedFeaturesCollections = selectedFeatures.Where(x => x.Key.Name == SelectedLLOS_ObserverLyrName)
                                            .Select(x => x.Value).FirstOrDefault();
                ReadPointFromLayer(surfaceEnvelope, LLOS_ObserversInExtent, LLOS_ObserversOutOfExtent, SelectedLLOS_ObserverLyrName, selectedFeaturesCollections);
            });
            await QueuedTask.Run(() =>
            {
                var selectedFeaturesCollections = selectedFeatures.Where(x => x.Key.Name == SelectedLLOS_TargetLyrName)
                                            .Select(x => x.Value).FirstOrDefault();
                ReadPointFromLayer(surfaceEnvelope, LLOS_TargetsInExtent, LLOS_TargetsOutOfExtent, SelectedLLOS_TargetLyrName, selectedFeaturesCollections, "target");
            });
        }

        internal override void OnDisplayCoordinateTypeChanged(object obj)
        {
            var list = TargetAddInPoints.ToList();
            var inExtentList = TargetInExtentPoints.ToList();
            var outExtentList = TargetOutExtentPoints.ToList();

            TargetAddInPoints.Clear();
            TargetInExtentPoints.Clear();
            TargetOutExtentPoints.Clear();

            foreach (var item in list)
                TargetAddInPoints.Add(item);

            foreach (var item in inExtentList)
                TargetInExtentPoints.Add(item);

            foreach (var item in outExtentList)
                TargetOutExtentPoints.Add(item);

            // and update observers
            base.OnDisplayCoordinateTypeChanged(obj);
        }
    }
}
