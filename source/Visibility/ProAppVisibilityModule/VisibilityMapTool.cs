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
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using VisibilityLibrary.Helpers;

namespace ProAppVisibilityModule
{
    internal class VisibilityMapTool : MapTool
    {
        protected override Task OnToolActivateAsync(bool active)
        {
            return base.OnToolActivateAsync(active);
        }

        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
        {
            if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
                return;

            try
            {
                QueuedTask.Run(() =>
                {
                    var mp = MapView.Active.ClientToMap(e.ClientPoint);
                    Mediator.NotifyColleagues(VisibilityLibrary.Constants.NEW_MAP_POINT, mp);
                });
            }
            catch (Exception ex)
            {

            }

            base.OnToolMouseDown(e);
        }
    }
}
