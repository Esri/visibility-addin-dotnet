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
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core;
using VisibilityLibrary;
using VisibilityLibrary.Helpers;
using ProAppVisibilityModule.Helpers;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ProAppVisibilityModule.ViewModels
{
    public class ProRLOSViewModel : ProLOSBaseViewModel
    {
        #region Properties

        private int executionCounter = 0;
        private string _ObserversLayerName = VisibilityLibrary.Properties.Resources.RLOSObserversLayerName;
        public string ObserversLayerName
        {
            get
            {
                if (executionCounter > 0)
                {
                    _ObserversLayerName = string.Format("{0}_{1}", VisibilityLibrary.Properties.Resources.RLOSObserversLayerName, executionCounter);
                }
                return _ObserversLayerName;
            }
            set { }
        }

        private string _FeatureDatasetName = VisibilityLibrary.Properties.Resources.RLOSFeatureDatasetName;
        public string FeatureDatasetName
        {
            get
            {
                if (executionCounter > 0)
                {
                    _FeatureDatasetName = string.Format("{0}_{1}", VisibilityLibrary.Properties.Resources.RLOSFeatureDatasetName, executionCounter);
                }
                return _FeatureDatasetName;
            }
            set { }
        }

        private string _RLOSConvertedPolygonsLayerName = VisibilityLibrary.Properties.Resources.RLOSConvertedPolygonsLayerName;
        public string RLOSConvertedPolygonsLayerName
        {
            get
            {
                if (executionCounter > 0)
                {
                    _RLOSConvertedPolygonsLayerName = string.Format("{0}_{1}", VisibilityLibrary.Properties.Resources.RLOSConvertedPolygonsLayerName, executionCounter);
                }
                return _RLOSConvertedPolygonsLayerName;
            }
            set { }
        }

        private string _RLOSOutputLayerName = VisibilityLibrary.Properties.Resources.RLOSOutputLayerName;
        public string RLOSOutputLayerName
        {
            get
            {
                if (executionCounter > 0)
                {
                    _RLOSOutputLayerName = string.Format("{0}_{1}", VisibilityLibrary.Properties.Resources.RLOSOutputLayerName, executionCounter);
                }
                return _RLOSOutputLayerName;
            }
            set { }
        }

        private string _RLOSMaskLayerName = VisibilityLibrary.Properties.Resources.RLOSMaskLayerName;
        public string RLOSMaskLayerName
        {
            get
            {
                if (executionCounter > 0)
                {
                    _RLOSMaskLayerName = string.Format("{0}_{1}", VisibilityLibrary.Properties.Resources.RLOSMaskLayerName, executionCounter);
                }
                return _RLOSMaskLayerName;
            }
            set { }
        }

        private double _SurfaceOffset = 0.0;
        public double SurfaceOffset
        {
            get { return _SurfaceOffset; }
            set
            {
                if (value < 0.0)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEMustBePositive);

                _SurfaceOffset = value;
                RaisePropertyChanged(() => SurfaceOffset);
            }
        }

        private double _MinDistance = 0.0;
        public double MinDistance
        {
            get { return _MinDistance; }
            set
            {
                if (value < 0.0)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEMustBePositive);

                if (value > MaxDistance)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AENumMustBeLess);

                _MinDistance = value;
                RaisePropertyChanged(() => MinDistance);
            }
        }

        private double _MaxDistance = 1000.0;
        public double MaxDistance
        {
            get { return _MaxDistance; }
            set
            {
                if (value < 0.0)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEMustBePositive);

                if (value < MinDistance)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AENumMustBeGreater);

                _MaxDistance = value;
                RaisePropertyChanged(() => MaxDistance);
            }
        }

        private double _LeftHorizontalFOV = 0.0;
        public double LeftHorizontalFOV
        {
            get { return _LeftHorizontalFOV; }
            set
            {
                var checkAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, value);

                if (checkAngleInDegrees < 0.0 || checkAngleInDegrees > 360.0)
                    throw new ArgumentException(string.Format(VisibilityLibrary.Properties.Resources.AENumRange, 0, GetAngularDistanceFromTo(AngularTypes.DEGREES, AngularUnitType, 360.0)));

                _LeftHorizontalFOV = value;
                RaisePropertyChanged(() => LeftHorizontalFOV);
            }
        }
        private double _RightHorizontalFOV = 360.0;
        public double RightHorizontalFOV
        {
            get { return _RightHorizontalFOV; }
            set
            {
                var checkAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, value);

                if (checkAngleInDegrees < 0.0 || checkAngleInDegrees > 360.0)
                    throw new ArgumentException(string.Format(VisibilityLibrary.Properties.Resources.AENumRange, 0, GetAngularDistanceFromTo(AngularTypes.DEGREES, AngularUnitType, 360.0)));

                _RightHorizontalFOV = value;
                RaisePropertyChanged(() => RightHorizontalFOV);
            }
        }
        private double _BottomVerticalFOV = -90.0;
        public double BottomVerticalFOV
        {
            get { return _BottomVerticalFOV; }
            set
            {
                var checkAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, value);

                if (checkAngleInDegrees < -90.0 || checkAngleInDegrees > 0.0)
                    throw new ArgumentException(string.Format(VisibilityLibrary.Properties.Resources.AENumRange, GetAngularDistanceFromTo(AngularTypes.DEGREES, AngularUnitType, -90.0), 0.0));

                _BottomVerticalFOV = value;
                RaisePropertyChanged(() => BottomVerticalFOV);
            }
        }

        private double _TopVerticalFOV = 90.0;
        public double TopVerticalFOV
        {
            get { return _TopVerticalFOV; }
            set
            {
                var checkAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, value);

                if (checkAngleInDegrees < -0.0 || checkAngleInDegrees > 90.0)
                    throw new ArgumentException(string.Format(VisibilityLibrary.Properties.Resources.AENumRange, 0.0, GetAngularDistanceFromTo(AngularTypes.DEGREES, AngularUnitType, 90.0)));

                _TopVerticalFOV = value;
                RaisePropertyChanged(() => TopVerticalFOV);
            }
        }

        public bool ShowNonVisibleData { get; set; }
        public int RunCount { get; set; }

        private Visibility _displayProgressBar = Visibility.Collapsed;
        public Visibility DisplayProgressBar
        {
            get
            {
                return _displayProgressBar;
            }
            set
            {
                _displayProgressBar = value;
                RaisePropertyChanged(() => DisplayProgressBar);
            }
        }

        #endregion

        #region Commands

        public RelayCommand SubmitCommand { get; set; }

        private async void OnSubmitCommand(object obj)
        {
            DisplayProgressBar = Visibility.Visible;
            await CreateMapElement();
            DisplayProgressBar = Visibility.Hidden;
        }

        private async void OnCancelCommand(object obj)
        {
            await Reset(true);
        }

        private async void OnClearCommand(object obj)
        {
            await Reset(true);
        }

        #endregion

        /// <summary>
        /// One and only constructor
        /// </summary>
        public ProRLOSViewModel()
        {
            ShowNonVisibleData = false;
            RunCount = 1;
            DisplayProgressBar = Visibility.Hidden;

            // commands
            SubmitCommand = new RelayCommand(OnSubmitCommand);
            ClearGraphicsCommand = new RelayCommand(OnClearCommand);
            CancelCommand = new RelayCommand(OnCancelCommand);
        }

        #region overrides

        internal override void OnDeletePointCommand(object obj)
        {
            base.OnDeletePointCommand(obj);
        }

        internal override void OnDeleteAllPointsCommand(object obj)
        {
            base.OnDeleteAllPointsCommand(obj);
        }

        public override bool CanCreateElement
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(SelectedSurfaceName)
                    && ObserverAddInPoints.Any());
            }
        }

        /// <summary>
        /// Where all of the work is done.  Override from TabBaseViewModel
        /// </summary>
        internal override async Task CreateMapElement()
        {
            try
            {
                IsRunning = true;

                if (!CanCreateElement || MapView.Active == null || MapView.Active.Map == null || string.IsNullOrWhiteSpace(SelectedSurfaceName))
                    return;

                bool success = await ExecuteVisibilityRLOS();

                if (!success)
                    MessageBox.Show("LLOS computations did not complete correctly.\nPlease check your parameters and try again.",
                        VisibilityLibrary.Properties.Resources.CaptionError);

                DeactivateTool(VisibilityMapTool.ToolId);

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
            }
        }

        #endregion overrides

        #region Private

        private async Task<bool> ExecuteVisibilityRLOS()
        {
            bool success = false;

            try
            {
                // Check surface spatial reference
                var surfaceSR = await GetSpatialReferenceFromLayer(SelectedSurfaceName);
                if (surfaceSR == null || !surfaceSR.IsProjected)
                {
                    MessageBox.Show(VisibilityLibrary.Properties.Resources.RLOSUserPrompt, VisibilityLibrary.Properties.Resources.RLOSUserPromptCaption);
                    return false;
                }

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

                using (Geodatabase geodatabase = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(CoreModule.CurrentProject.DefaultGeodatabasePath))))
                {
                    executionCounter = 0;
                    var enterpriseDefinitionNames = geodatabase.GetDefinitions<FeatureDatasetDefinition>().Where(i => i.GetName().StartsWith(VisibilityLibrary.Properties.Resources.RLOSFeatureDatasetName)).Select(i => i.GetName()).ToList();
                    foreach (var defName in enterpriseDefinitionNames)
                    {
                        int n;
                        bool isNumeric = int.TryParse(Regex.Match(defName, @"\d+$").Value, out n);
                        if (isNumeric)
                            executionCounter = executionCounter < n ? n : executionCounter;
                    }
                    executionCounter = enterpriseDefinitionNames.Count > 0 ? executionCounter + 1 : 0;
                }

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

                // add observer points to feature layer

                await FeatureClassHelper.CreatingFeatures(ObserversLayerName, ObserverAddInPoints, GetAsMapZUnits(surfaceSR, ObserverOffset.Value));

                // update with surface information

                success = await FeatureClassHelper.AddSurfaceInformation(ObserversLayerName, SelectedSurfaceName, VisibilityLibrary.Properties.Resources.ZFieldName);
                if (!success)
                    return false;

                // Visibility

                var observerOffsetInMapZUnits = GetAsMapZUnits(surfaceSR, ObserverOffset.Value);
                var surfaceOffsetInMapZUnits = GetAsMapZUnits(surfaceSR, SurfaceOffset);
                var minDistanceInMapUnits = GetAsMapUnits(surfaceSR, MinDistance);
                var maxDistanceInMapUnits = GetAsMapUnits(surfaceSR, MaxDistance);
                var horizontalStartAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, LeftHorizontalFOV);
                var horizontalEndAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, RightHorizontalFOV);
                var verticalUpperAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, TopVerticalFOV);
                var verticalLowerAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, BottomVerticalFOV);

                await FeatureClassHelper.UpdateShapeWithZ(ObserversLayerName, VisibilityLibrary.Properties.Resources.ZFieldName, observerOffsetInMapZUnits);

                await CreateMask(RLOSMaskLayerName, minDistanceInMapUnits, maxDistanceInMapUnits, horizontalStartAngleInDegrees,
                    horizontalEndAngleInDegrees, surfaceSR);

                string maxRangeMaskFeatureClassName = CoreModule.CurrentProject.DefaultGeodatabasePath + System.IO.Path.DirectorySeparatorChar + FeatureDatasetName + System.IO.Path.DirectorySeparatorChar + RLOSMaskLayerName;
                var environments = Geoprocessing.MakeEnvironmentArray(mask: maxRangeMaskFeatureClassName, overwriteoutput: true);

                var rlosOutputLayer = CoreModule.CurrentProject.DefaultGeodatabasePath + System.IO.Path.DirectorySeparatorChar + RLOSOutputLayerName;

                success = await FeatureClassHelper.CreateVisibility(SelectedSurfaceName, ObserversLayerName,
                    rlosOutputLayer,
                    observerOffsetInMapZUnits, surfaceOffsetInMapZUnits,
                    minDistanceInMapUnits, maxDistanceInMapUnits,
                    horizontalStartAngleInDegrees, horizontalEndAngleInDegrees,
                    verticalUpperAngleInDegrees, verticalLowerAngleInDegrees,
                    ShowNonVisibleData,
                    environments,
                    false);

                if (!success)
                    return false;

                var rlosConvertedPolygonsLayer = CoreModule.CurrentProject.DefaultGeodatabasePath + System.IO.Path.DirectorySeparatorChar + FeatureDatasetName + System.IO.Path.DirectorySeparatorChar + RLOSConvertedPolygonsLayerName;

                string rangeFanMaskFeatureClassName = string.Empty;
                if ((MinDistance > 0) || !((horizontalStartAngleInDegrees == 0.0) && (horizontalEndAngleInDegrees == 360.0)))
                {
                    string RLOSRangeFanMaskLayerName = "RangeFan_" + RLOSMaskLayerName;
                    rangeFanMaskFeatureClassName = CoreModule.CurrentProject.DefaultGeodatabasePath + System.IO.Path.DirectorySeparatorChar + FeatureDatasetName + System.IO.Path.DirectorySeparatorChar + RLOSRangeFanMaskLayerName;

                    await CreateMask(RLOSRangeFanMaskLayerName, minDistanceInMapUnits, maxDistanceInMapUnits, horizontalStartAngleInDegrees,
                        horizontalEndAngleInDegrees, surfaceSR, true);
                }

                success = await FeatureClassHelper.IntersectOutput(rlosOutputLayer, rlosConvertedPolygonsLayer, false, "Value", rangeFanMaskFeatureClassName);
                if (!success)
                    return false;

                await FeatureClassHelper.CreateUniqueValueRenderer(GetLayerFromMapByName(RLOSConvertedPolygonsLayerName) as FeatureLayer, ShowNonVisibleData, RLOSConvertedPolygonsLayerName);

                await FeatureClassHelper.Delete(RLOSOutputLayerName);
                //await FeatureClassHelper.Delete(RLOSMaskLayerName);

                // Eventually we will add the new layers to a new group layer for each run
                // Currently not working in current release of Pro.  
                // From Roshan Herbert - I just spoke with the Dev who wrote the MoveLayer method. Apparently this a known issue. 
                //                       We have bug to fix this and plan to fix it in the next release.
                List<Layer> layerList = new List<Layer>();
                layerList.Add(GetLayerFromMapByName(ObserversLayerName));
                layerList.Add(GetLayerFromMapByName(RLOSConvertedPolygonsLayerName));
                await FeatureClassHelper.MoveLayersToGroupLayer(layerList, FeatureDatasetName);

                //string groupName = "RLOS Group";
                //if (executionCounter > 0)
                //    groupName = string.Format("{0}_{1}", groupName, executionCounter.ToString());

                //await FeatureClassHelper.CreateGroupLayer(layerList, groupName);

                // for now we are not resetting after a run of the tool
                //await Reset(true);

                // Get the extent of the output layer and zoom to extent
                var layer = GetLayerFromMapByName(RLOSConvertedPolygonsLayerName);
                if (layer != null)
                {
                    var envelope = await QueuedTask.Run(() => layer.QueryExtent());
                    await ZoomToExtent(envelope);
                }

                success = true;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Method used to create a mask for geoprocessing environment
        /// Will buffer around each observer at the max distance to create mask
        /// </summary>
        /// <param name="maskFeatureClassName"></param>
        /// <param name="bufferDistance"></param>
        /// <returns>Task</returns>
        private async Task CreateMask(string maskFeatureClassName,
            double minDistanceInMapUnits, double maxDistanceInMapUnits,
            double horizontalStartAngleInDegrees, double horizontalEndAngleInDegrees,
            SpatialReference surfaceSR, bool constructRangeFans = false)
        {
            // create new
            await FeatureClassHelper.CreateLayer(FeatureDatasetName, maskFeatureClassName, "POLYGON", false, false);

            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(FeatureClassHelper.FgdbFileToConnectionPath(CoreModule.CurrentProject.DefaultGeodatabasePath)))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(maskFeatureClassName))
                    using (FeatureClassDefinition fcDefinition = enterpriseFeatureClass.GetDefinition())
                    {
                        EditOperation editOperation = new EditOperation();
                        editOperation.Callback(context =>
                        {
                            try
                            {
                                var shapeFieldName = fcDefinition.GetShapeField();

                                foreach (var observer in ObserverAddInPoints)
                                {
                                    using (var rowBuffer = enterpriseFeatureClass.CreateRowBuffer())
                                    {
                                        // Either the field index or the field name can be used in the indexer.
                                        // project the point here or the buffer tool may use an angular unit and run forever
                                        var point = GeometryEngine.Instance.Project(observer.Point, surfaceSR);
                                        Geometry polygon = null;

                                        if (constructRangeFans)
                                        {
                                            polygon = GeometryHelper.ConstructRangeFan(point as MapPoint,
                                                minDistanceInMapUnits, maxDistanceInMapUnits, horizontalStartAngleInDegrees,
                                                horizontalEndAngleInDegrees, surfaceSR);
                                        }
                                        else
                                        {
                                            polygon = GeometryEngine.Instance.Buffer(point, maxDistanceInMapUnits);
                                        }

                                        rowBuffer[shapeFieldName] = polygon;

                                        Feature feature = enterpriseFeatureClass.CreateRow(rowBuffer);
                                        feature.Store();
                                        context.Invalidate(feature);
                                    } // using
                                } // for each 
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

        // TODO: Move repeated method GetAngularDistanceFromTo to common visibility library
        /// <summary>
        /// Method to convert to/from different types of angular units
        /// </summary>
        /// <param name="fromType">DistanceTypes</param>
        /// <param name="toType">DistanceTypes</param>
        private double GetAngularDistanceFromTo(AngularTypes fromType, AngularTypes toType, double input)
        {
            double angularDistance = input;

            try
            {
                if (fromType == AngularTypes.DEGREES && toType == AngularTypes.GRADS)
                    angularDistance *= 1.11111;
                else if (fromType == AngularTypes.DEGREES && toType == AngularTypes.MILS)
                    angularDistance *= 17.777777777778;
                else if (fromType == AngularTypes.GRADS && toType == AngularTypes.DEGREES)
                    angularDistance /= 1.11111;
                else if (fromType == AngularTypes.GRADS && toType == AngularTypes.MILS)
                    angularDistance *= 16;
                else if (fromType == AngularTypes.MILS && toType == AngularTypes.DEGREES)
                    angularDistance /= 17.777777777778;
                else if (fromType == AngularTypes.MILS && toType == AngularTypes.GRADS)
                    angularDistance /= 16;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return Math.Round(angularDistance, 1);
        }

        #endregion Private
    }
}
