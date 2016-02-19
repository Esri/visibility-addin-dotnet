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
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using ArcMapAddinVisibility.Helpers;

namespace ArcMapAddinVisibility.ViewModels
{
    public class LLOSViewModel : LOSBaseViewModel
    {
        public LLOSViewModel()
        {
            TargetPoints = new ObservableCollection<IPoint>();

            // commands
            SubmitCommand = new RelayCommand(OnSubmitCommand);
        }

        #region Properties

        public ObservableCollection<IPoint> TargetPoints { get; set; }

        #endregion

        #region Commands

        public RelayCommand SubmitCommand { get; set; }

        private void OnSubmitCommand(object obj)
        {
            CreateMapElement();
        }

        #endregion

        internal override void OnNewMapPointEvent(object obj)
        {
            base.OnNewMapPointEvent(obj);

            if (!IsActiveTab)
                return;

            var point = obj as IPoint;

            if (point == null)
                return;

            if (ToolMode == MapPointToolMode.Target)
            {
                TargetPoints.Insert(0, point);
                //TODO change color
                AddGraphicToMap(point, true);
            }
        }

        internal override void Reset(bool toolReset)
        {
            base.Reset(toolReset);

            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return;

            // reset target points
            TargetPoints.Clear();
        }

        public override bool CanCreateElement
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(SelectedSurfaceName) 
                    && ObserverPoints.Any() 
                    && TargetPoints.Any()
                    && ObserverOffset.HasValue
                    && TargetOffset.HasValue);
            }
        }

        internal override void CreateMapElement()
        {
            if (!CanCreateElement || ArcMap.Document == null || ArcMap.Document.FocusMap == null || string.IsNullOrWhiteSpace(SelectedSurfaceName))
                return;

 	        base.CreateMapElement();
            
            // take your two points and get line of sight

            var surface = GetSurfaceFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName);

            if(surface == null)
                return;

            var geoBridge = new GeoDatabaseHelperClass() as IGeoDatabaseBridge2;

            if (geoBridge == null)
                return;

            IPoint pointObstruction = null;
            IPolyline polyVisible = null;
            IPolyline polyInvisible = null;
            bool targetIsVisible = false;

            foreach (var observerPoint in ObserverPoints)
            {
                foreach (var targetPoint in TargetPoints)
                {
                    //TODO add your offsets here, will need to convert to map z units
                    var z1 = surface.GetElevation(observerPoint) + ObserverOffset.Value;
                    var z2 = surface.GetElevation(targetPoint) + TargetOffset.Value;

                    geoBridge.GetLineOfSight(surface,
                        new PointClass() { Z = z1, X = observerPoint.X, Y = observerPoint.Y, ZAware = true },
                        new PointClass() { Z = z2, X = targetPoint.X, Y = targetPoint.Y, ZAware = true },
                        out pointObstruction, out polyVisible, out polyInvisible, out targetIsVisible, false, false);

                    if (polyVisible == null)
                        return;

                    var rgbColor = new ESRI.ArcGIS.Display.RgbColorClass() as IRgbColor;
                    rgbColor.Green = 255;
                    AddGraphicToMap(polyVisible, rgbColor as IColor);

                    if (polyInvisible == null)
                        return;

                    var rgbColor2 = new ESRI.ArcGIS.Display.RgbColorClass() as IRgbColor;
                    rgbColor2.Red = 255;
                    AddGraphicToMap(polyInvisible, rgbColor2);
                }
            }
        }
    }
}
