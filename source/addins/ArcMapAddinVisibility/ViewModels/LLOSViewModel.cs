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
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections;
using System.Windows;

// Esri
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Carto;

// Solution
using VisibilityLibrary.Helpers;
using ArcMapAddinVisibility.Models;

namespace ArcMapAddinVisibility.ViewModels
{
    public class LLOSViewModel : LOSBaseViewModel
    {
        public LLOSViewModel()
        {
            DisplayProgressBarLLOS = Visibility.Hidden;
            // commands
            SubmitCommand = new RelayCommand(OnSubmitCommand);

            ClearGraphicsVisible = true;

        }

        #region Properties

        private Visibility _displayProgressBarLLOS;
        public Visibility DisplayProgressBarLLOS
        {
            get
            {
                return _displayProgressBarLLOS;
            }
            set
            {
                _displayProgressBarLLOS = value;
                RaisePropertyChanged(() => DisplayProgressBarLLOS);
            }
        }

        #endregion

        #region Commands

        public RelayCommand SubmitCommand { get; set; }

        /// <summary>
        /// Method to handle the Submit/OK button command
        /// </summary>
        /// <param name="obj">null</param>
        private void OnSubmitCommand(object obj)
        {
            var savedCursor = System.Windows.Forms.Cursor.Current;
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            System.Windows.Forms.Application.DoEvents();

            CreateMapElement();

            System.Windows.Forms.Cursor.Current = savedCursor;
        }

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

            ValidateLLOS_LayerSelection();
        }

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

        internal override void OnNewMapPointEvent(object obj)
        {
            base.OnNewMapPointEvent(obj);

            if (!IsActiveTab)
                return;

            var point = obj as IPoint;

            if (point == null || !IsValidPoint(point))
                return;

            if (ToolMode == MapPointToolMode.Target)
            {
                var color = new RgbColorClass() { Red = 255 } as IColor;
                var guid = AddGraphicToMap(point, color, true, esriSimpleMarkerStyle.esriSMSSquare);
                var addInPoint = new AddInPoint() { Point = point, GUID = guid };
                TargetAddInPoints.Insert(0, addInPoint);
            }

            ValidateLLOS_LayerSelection();
        }

        internal override void Reset(bool toolReset)
        {
            base.Reset(toolReset);

            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return;

            // reset target points
            TargetAddInPoints.Clear();
        }

        public override bool CanCreateElement
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(SelectedSurfaceName)
                    && (ObserverAddInPoints.Any() || LLOS_ObserversInExtent.Any() || LLOS_ObserversOutOfExtent.Any())
                    && (TargetAddInPoints.Any() || LLOS_TargetsInExtent.Any() || LLOS_TargetsOutOfExtent.Any())
                    && TargetOffset.HasValue);
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
        internal override void CreateMapElement()
        {
            try
            {
                IsRunning = true;
                IPolyline longestLine = new PolylineClass();

                ReadSelectedLayerPoints();
                if (!CanCreateElement || ArcMap.Document == null || ArcMap.Document.FocusMap == null || string.IsNullOrWhiteSpace(SelectedSurfaceName))
                    return;

                // take your observer and target points and get lines of sight
                var observerPoints = new ObservableCollection<AddInPoint>(LLOS_ObserversInExtent.Select(x => x.AddInPoint).Union(ObserverAddInPoints));
                var targetPoints = new ObservableCollection<AddInPoint>(LLOS_TargetsInExtent.Select(x => x.AddInPoint).Union(TargetAddInPoints));
                var surface = GetSurfaceFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName);

                if (surface == null)
                    return;

                ILayer surfaceLayer = GetLayerFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName);

                // Issue warning if layer is ImageServerLayer
                if (surfaceLayer is IImageServerLayer)
                {
                    MessageBoxResult mbr = MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgLayerIsImageService,
                        VisibilityLibrary.Properties.Resources.CaptionLayerIsImageService, MessageBoxButton.YesNo);

                    if (mbr == MessageBoxResult.No)
                    {
                        System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgTryAgain, VisibilityLibrary.Properties.Resources.MsgCalcCancelled);
                        return;
                    }
                }

                // Determine if selected surface is projected or geographic
                var geoDataset = surfaceLayer as IGeoDataset;
                if (geoDataset == null)
                {
                    System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.MsgTryAgain, VisibilityLibrary.Properties.Resources.CaptionError);
                    return;
                }

                SelectedSurfaceSpatialRef = geoDataset.SpatialReference;

                if (SelectedSurfaceSpatialRef is IGeographicCoordinateSystem)
                {
                    MessageBox.Show(VisibilityLibrary.Properties.Resources.LLOSUserPrompt, VisibilityLibrary.Properties.Resources.LLOSUserPromptCaption);
                    return;
                }

                if (ArcMap.Document.FocusMap.SpatialReference.FactoryCode != geoDataset.SpatialReference.FactoryCode)
                {
                    MessageBox.Show(VisibilityLibrary.Properties.Resources.LOSDataFrameMatch, VisibilityLibrary.Properties.Resources.LOSSpatialReferenceCaption);
                    return;
                }

                SelectedSurfaceSpatialRef = geoDataset.SpatialReference;

                var geoBridge = (IGeoDatabaseBridge2)new GeoDatabaseHelperClass();

                IPoint pointObstruction = null;
                IPolyline polyVisible = null;
                IPolyline polyInvisible = null;
                bool targetIsVisible = false;

                double finalObserverOffset = GetOffsetInZUnits(ObserverOffset.Value, surface.ZFactor, OffsetUnitType);
                double finalTargetOffset = GetOffsetInZUnits(TargetOffset.Value, surface.ZFactor, OffsetUnitType);

                var DictionaryTargetObserverCount = new Dictionary<IPoint, int>();

                foreach (var observerPoint in observerPoints)
                {
                    // keep track of visible targets for this observer
                    var CanSeeAtLeastOneTarget = false;

                    var z1 = surface.GetElevation(observerPoint.Point) + finalObserverOffset;

                    if (double.IsNaN(z1))
                    {
                        System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.LLOSPointsOutsideOfSurfaceExtent, VisibilityLibrary.Properties.Resources.MsgCalcCancelled);
                        return;
                    }

                    foreach (var targetPoint in targetPoints)
                    {
                        var z2 = surface.GetElevation(targetPoint.Point) + finalTargetOffset;

                        if (double.IsNaN(z2))
                        {
                            System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.LLOSPointsOutsideOfSurfaceExtent, VisibilityLibrary.Properties.Resources.MsgCalcCancelled);
                            return;
                        }

                        var fromPoint = new PointClass() { Z = z1, X = observerPoint.Point.X, Y = observerPoint.Point.Y, ZAware = true } as IPoint;
                        var toPoint = new PointClass() { Z = z2, X = targetPoint.Point.X, Y = targetPoint.Point.Y, ZAware = true } as IPoint;

                        geoBridge.GetLineOfSight(surface, fromPoint, toPoint,
                            out pointObstruction, out polyVisible, out polyInvisible, out targetIsVisible, false, false);

                        var pcol = new PolylineClass() as IPointCollection;
                        pcol.AddPoint(fromPoint);
                        pcol.AddPoint(toPoint);
                        IPolyline pcolPolyline = pcol as IPolyline;

                        longestLine = (longestLine != null && longestLine.Length < pcolPolyline.Length) ? pcolPolyline : longestLine;

                        // set the flag if we can see at least one target
                        if (targetIsVisible)
                        {
                            CanSeeAtLeastOneTarget = true;

                            // update target observer count
                            UpdateTargetObserverCount(DictionaryTargetObserverCount, targetPoint.Point);
                        }

                        // First Add "SightLine" so it appears behind others
                        // Black = Not visible -or- White = Visible
                        if (targetIsVisible)
                            AddGraphicToMap(pcolPolyline, new RgbColorClass() { RGB = 0xFFFFFF }, false,
                                size: 6); //  white line
                        else
                            AddGraphicToMap(pcolPolyline, new RgbColorClass() { RGB = 0x000000 }, false,
                                size: 6); //  black line

                        if (polyVisible != null)
                        {
                            AddGraphicToMap(polyVisible, new RgbColorClass() { Green = 255 }, size: 5);
                        }

                        if (polyInvisible != null)
                        {
                            AddGraphicToMap(polyInvisible, new RgbColorClass() { Red = 255 }, size: 3);
                        }

                        if (polyVisible == null && polyInvisible == null)
                        {
                            if (targetIsVisible)
                                AddGraphicToMap(pcol as IPolyline, new RgbColorClass() { Green = 255 }, size: 3);
                            else
                                AddGraphicToMap(pcol as IPolyline, new RgbColorClass() { Red = 255 }, size: 3);
                        }
                    }

                    // visualize observer

                    // add blue dot
                    AddGraphicToMap(observerPoint.Point, new RgbColorClass() { Blue = 255 }, size: 10);

                    if (CanSeeAtLeastOneTarget)
                    {
                        // add green dot
                        AddGraphicToMap(observerPoint.Point, new RgbColorClass() { Green = 255 });
                    }
                    else
                    {
                        // add red dot
                        AddGraphicToMap(observerPoint.Point, new RgbColorClass() { Red = 255 });
                    }
                }

                VisualizeTargets(DictionaryTargetObserverCount, targetPoints);

                if ((ObserverAddInPoints.Any() || LLOS_ObserversInExtent.Any())
                    && (TargetAddInPoints.Any() || LLOS_TargetsInExtent.Any()))
                {
                    ZoomToExtent(longestLine);
                }

                DisplayOutOfExtentMsg();

                //display points present out of extent
                var colorObserver = new RgbColorClass() { Blue = 255 };
                var colorTarget = new RgbColorClass() { Red = 255 };
                var colorObserverBorder = new RgbColorClass() { Red = 255, Blue = 255, Green = 255 };
                var colorTargetBorder = new RgbColorClass() { Red = 0, Blue = 0, Green = 0 };
                foreach (var point in LLOS_ObserversOutOfExtent)
                {
                    AddGraphicToMap(point.AddInPoint.Point, colorObserver, markerStyle: esriSimpleMarkerStyle.esriSMSX, size: 10, borderColor: colorObserverBorder);
                }
                foreach (var point in LLOS_TargetsOutOfExtent)
                {
                    AddGraphicToMap(point.AddInPoint.Point, colorTarget, markerStyle: esriSimpleMarkerStyle.esriSMSX, size: 10, borderColor: colorTargetBorder);
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Windows.Forms.MessageBox.Show(VisibilityLibrary.Properties.Resources.ExceptionSomethingWentWrong,
                                                     VisibilityLibrary.Properties.Resources.CaptionError);
            }
            finally
            {
                IsRunning = false;
                ClearLLOSCollections();
                ValidateLLOS_LayerSelection();
            }
        }

        private void DisplayOutOfExtentMsg()
        {
            var observerIDCollection = LLOS_ObserversOutOfExtent.Select(x=>x.ID).ToList<int>();
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
                    System.Windows.MessageBox.Show(msgString,
                                                "Unable To Process For Few Locations");
                }
                else
                {
                    System.Windows.MessageBox.Show(VisibilityLibrary.Properties.Resources.LLOSPointsOutsideOfSurfaceExtent,
                    VisibilityLibrary.Properties.Resources.MsgCalcCancelled);
                }

            }
        }

        private void VisualizeTargets(Dictionary<IPoint, int> dict, ObservableCollection<AddInPoint> targetPoints)
        {
            // visualize targets
            foreach (var targetPoint in targetPoints)
            {
                if (dict.ContainsKey(targetPoint.Point))
                {
                    // add green circle
                    AddGraphicToMap(targetPoint.Point, new RgbColorClass() { Green = 255 }, size: 10);
                    // add label
                    AddTextToMap(dict[targetPoint.Point].ToString(), targetPoint.Point, new RgbColorClass(), size: 10);
                }
                else
                {
                    // add red circle
                    AddGraphicToMap(targetPoint.Point, new RgbColorClass() { Red = 255 }, size: 10);
                }
            }
        }

        private void UpdateTargetObserverCount(Dictionary<IPoint, int> dict, IPoint targetPoint)
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

        private void ReadSelectedLayerPoints()
        {
            LLOS_ObserversInExtent.Clear();
            LLOS_ObserversOutOfExtent.Clear();
            LLOS_TargetsInExtent.Clear();
            LLOS_TargetsOutOfExtent.Clear();

            var observerColor = new RgbColor() { Blue = 255 } as IColor;
            ReadSelectedLyrPoints(LLOS_ObserversInExtent, LLOS_ObserversOutOfExtent, SelectedLLOS_ObserverLyrName, observerColor);

            var targetColor = new RgbColor() { Red = 255 } as IColor;
            ReadSelectedLyrPoints(LLOS_TargetsInExtent, LLOS_TargetsOutOfExtent, SelectedLLOS_TargetLyrName, observerColor);            
        }
    }
}
