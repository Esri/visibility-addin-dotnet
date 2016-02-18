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
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using System.Collections.ObjectModel;

namespace ArcMapAddinVisibility.ViewModels
{
    public class LLOSViewModel : LOSBaseViewModel
    {
        public LLOSViewModel()
        {
            SurfaceLayerNames = new ObservableCollection<string>();
        }

        #region Properties

        public ObservableCollection<string> SurfaceLayerNames { get; set; }
        public string SelectedSurfaceName { get; set; }

        #endregion

        internal override void Reset(bool toolReset)
        {
            base.Reset(toolReset);

            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return;

            var names = GetSurfaceNamesFromMap(ArcMap.Document.FocusMap);

            SurfaceLayerNames.Clear();

            foreach (var name in names)
                SurfaceLayerNames.Add(name);

            if (SurfaceLayerNames.Any())
                SelectedSurfaceName = SurfaceLayerNames[0];

            RaisePropertyChanged(() => SelectedSurfaceName);
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

            var z1 = surface.GetElevation(Point1) + 2;
            var z2 = surface.GetElevation(Point2) + 2;

            geoBridge.GetLineOfSight(surface, 
                new PointClass() { Z = z1, X = Point1.X, Y = Point1.Y, ZAware=true },
                new PointClass() { Z = z2, X = Point2.X, Y = Point2.Y, ZAware=true }, 
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
