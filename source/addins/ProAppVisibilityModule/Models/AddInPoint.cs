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

using ArcGIS.Core.Geometry;
using VisibilityLibrary.Helpers;
using ProAppVisibilityModule.Helpers;
using CoordinateConversionLibrary.Models;
using CoordinateConversionLibrary.Helpers;

namespace ProAppVisibilityModule.Models
{
    public class AddInPoint : VisibilityLibrary.Helpers.NotificationObject
    {
        public AddInPoint()
        {

        }

        private MapPoint point = null;
        public MapPoint Point
        {
            get
            {
                return point;
            }
            set
            {
                point = value;

                RaisePropertyChanged(() => Point);
                RaisePropertyChanged(() => Text);
            }
        }
        public string Text
        {
            get
            {                
                string outFormattedString = string.Empty;
                CoordinateType ccType = ConversionUtils.GetCoordinateString(MapPointHelper.GetMapPointAsDisplayString(Point), out outFormattedString);
                return outFormattedString;
            }
        }

        private string guid = string.Empty;
        public string GUID
        {
            get
            {
                return guid;
            }
            set
            {
                guid = value;
                RaisePropertyChanged(() => GUID);
            }
        }


    }
}
