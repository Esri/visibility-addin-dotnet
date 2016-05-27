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
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core;
using VisibilityLibrary;
using VisibilityLibrary.Helpers;
using ProAppVisibilityModule.Helpers;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;

namespace ProAppVisibilityModule.ViewModels
{
    public class ProRLOSViewModel : ProLOSBaseViewModel
    {
        #region Properties

        private double _SurfaceOffset = 0.0;
        public double SurfaceOffset 
        {
            get { return _SurfaceOffset; }
            set
            {
                if(value < 0.0)
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
                if(value < 0.0)
                    throw new ArgumentException(VisibilityLibrary.Properties.Resources.AEMustBePositive);

                if(value > MaxDistance)
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
                if(value < 0.0)
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
                if(value < 0.0 || value > 360.0)
                    throw new ArgumentException(string.Format(VisibilityLibrary.Properties.Resources.AENumRange, 0, 360));

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
                if (value < 0.0 || value > 360.0)
                    throw new ArgumentException(string.Format(VisibilityLibrary.Properties.Resources.AENumRange, 0, 360));

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
                if (value < -90.0 || value > 0.0)
                    throw new ArgumentException(string.Format(VisibilityLibrary.Properties.Resources.AENumRange, -90.0, 0.0));

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
                if (value < 0.0 || value > 90.0)
                    throw new ArgumentException(string.Format(VisibilityLibrary.Properties.Resources.AENumRange, 0.0, 90.0));

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

                DeactivateTool("ProAppVisibilityModule_MapTool");

                await ExecuteVisibilityRLOS();

                //await base.CreateMapElement();
            }
            catch (Exception ex)
            {
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

        private async Task ExecuteVisibilityRLOS()
        {
            try
            {
                var surfaceSR = await GetSpatialReferenceFromLayer(SelectedSurfaceName);

                if(surfaceSR == null || !surfaceSR.IsProjected)
                {
                    MessageBox.Show(VisibilityLibrary.Properties.Resources.RLOSUserPrompt, VisibilityLibrary.Properties.Resources.RLOSUserPromptCaption);
                    return;
                }

                await FeatureClassHelper.CreateLayer(VisibilityLibrary.Properties.Resources.ObserversLayerName, "POINT", true, true);

                // add fields for observer offset

                await FeatureClassHelper.AddFieldToLayer(VisibilityLibrary.Properties.Resources.ObserversLayerName, VisibilityLibrary.Properties.Resources.OffsetFieldName, "DOUBLE");
                await FeatureClassHelper.AddFieldToLayer(VisibilityLibrary.Properties.Resources.ObserversLayerName, VisibilityLibrary.Properties.Resources.OffsetWithZFieldName, "DOUBLE");

                // add observer points to feature layer

                await FeatureClassHelper.CreatingFeatures(VisibilityLibrary.Properties.Resources.ObserversLayerName, ObserverAddInPoints, GetAsMapZUnits(surfaceSR, ObserverOffset.Value));

                // update with surface information

                await FeatureClassHelper.AddSurfaceInformation(VisibilityLibrary.Properties.Resources.ObserversLayerName, SelectedSurfaceName, VisibilityLibrary.Properties.Resources.ZFieldName);

                // Visibility

                var observerOffsetInMapZUnits = GetAsMapZUnits(surfaceSR, ObserverOffset.Value);
                var surfaceOffsetInMapZUnits = GetAsMapZUnits(surfaceSR, SurfaceOffset);
                var minDistanceInMapUnits = GetAsMapUnits(surfaceSR, MinDistance);
                var maxDistanceInMapUnits = GetAsMapUnits(surfaceSR, MaxDistance);
                var horizontalStartAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, LeftHorizontalFOV);
                var horizontalEndAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, RightHorizontalFOV);
                var verticalUpperAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, TopVerticalFOV);
                var verticalLowerAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, BottomVerticalFOV);

                await FeatureClassHelper.UpdateShapeWithZ(VisibilityLibrary.Properties.Resources.ObserversLayerName, VisibilityLibrary.Properties.Resources.ZFieldName, observerOffsetInMapZUnits);
                
                string maskFeatureClassName = CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + VisibilityLibrary.Properties.Resources.RLOSMaskLayerName;

                await CreateMask(VisibilityLibrary.Properties.Resources.RLOSMaskLayerName, maxDistanceInMapUnits, surfaceSR);

                var environments = Geoprocessing.MakeEnvironmentArray(mask: maskFeatureClassName, overwriteoutput: true);
                var rlosOutputLayer = CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + VisibilityLibrary.Properties.Resources.RLOSOutputLayerName;

                await FeatureClassHelper.CreateVisibility(SelectedSurfaceName, VisibilityLibrary.Properties.Resources.ObserversLayerName,
                    rlosOutputLayer,
                    observerOffsetInMapZUnits, surfaceOffsetInMapZUnits,
                    minDistanceInMapUnits, maxDistanceInMapUnits,
                    horizontalStartAngleInDegrees, horizontalEndAngleInDegrees,
                    verticalUpperAngleInDegrees, verticalLowerAngleInDegrees,
                    ShowNonVisibleData,
                    environments,
                    false);

                var rlosConvertedPolygonsLayer = CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + VisibilityLibrary.Properties.Resources.RLOSConvertedPolygonsLayerName;

                await FeatureClassHelper.IntersectOutput(rlosOutputLayer, rlosConvertedPolygonsLayer, false, "Value");

                // workaround to get unique value renderer to function when we have vis or non vis data
                await FeatureClassHelper.UpdateFieldWithValue(VisibilityLibrary.Properties.Resources.RLOSConvertedPolygonsLayerName, true);

                await FeatureClassHelper.CreateUniqueValueRenderer(GetLayerFromMapByName(VisibilityLibrary.Properties.Resources.RLOSConvertedPolygonsLayerName) as FeatureLayer, ShowNonVisibleData);

                await FeatureClassHelper.UpdateFieldWithValue(VisibilityLibrary.Properties.Resources.RLOSConvertedPolygonsLayerName, false);

                // for now we are not resetting after a run of the tool
                //await Reset(true);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        /// <summary>
        /// Method used to create a mask for geoprocessing environment
        /// Will buffer around each observer at the max distance to create mask
        /// </summary>
        /// <param name="maskFeatureClassName"></param>
        /// <param name="bufferDistance"></param>
        /// <returns>Task</returns>
        private async Task CreateMask(string maskFeatureClassName, double bufferDistance, SpatialReference surfaceSR)
        {
            // create new
            await FeatureClassHelper.CreateLayer(maskFeatureClassName, "POLYGON", false, false);

            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
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
                                        var point = GeometryEngine.Project(observer.Point, surfaceSR);
                                        var polygon = GeometryEngine.Buffer(point, bufferDistance);
                                        rowBuffer[shapeFieldName] = polygon;

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
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

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
                Console.WriteLine(ex);
            }

            return angularDistance;
        }

        #endregion Private
    }
}
