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

using ArcMapAddinVisibility.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcMapAddinVisibility.ViewModels
{
    public class RLOSViewModel : LOSBaseViewModel
    {
        #region Properties

        public double SurfaceOffset { get; set; }
        public double MinDistance { get; set; }
        public double MaxDistance { get; set; }
        public double LeftHorizontalFOV { get; set; }
        public double RightHorizontalFOV { get; set; }
        public double BottomVerticalFOV { get; set; }
        public double TopVerticalFOV { get; set; }

        #endregion

        #region Commands

        public RelayCommand SubmitCommand { get; set; }

        private void OnSubmitCommand(object obj)
        {
            CreateMapElement();
        }

        #endregion

        public RLOSViewModel()
        {
            SurfaceOffset = 0.0;
            MinDistance = 0.0;
            MaxDistance = 100;
            LeftHorizontalFOV = 0.0;
            RightHorizontalFOV = 360.0;
            BottomVerticalFOV = -90.0;
            TopVerticalFOV = 90.0;
        }

        public override bool CanCreateElement
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(SelectedSurfaceName)
                    && ObserverPoints.Any());
            }
        }
    }
}
