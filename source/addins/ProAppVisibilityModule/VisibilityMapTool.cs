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
using ArcGIS.Core.Geometry;

namespace ProAppVisibilityModule
{
    internal class VisibilityMapTool : MapTool
    {
        public VisibilityMapTool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Point;
            UseSnapping = true;
        }

        public static string ToolId
        {
            // Important: this must match the Tool ID used in the DAML
            get { return "ProAppVisibilityModule_MapTool"; }
        }

        protected override Task OnToolActivateAsync(bool active)
        {
            //Mediator.NotifyColleagues(Helpers.Constants.MAP_POINT_TOOL_ACTIVATED, active);
            ToolActiveDeactive(active, Helpers.Constants.MAP_POINT_TOOL_ACTIVATED);
            return base.OnToolActivateAsync(active);
        }

        protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
        {
            //Mediator.NotifyColleagues(Helpers.Constants.MAP_POINT_TOOL_DEACTIVATED, hasMapViewChanged);
            ToolActiveDeactive(hasMapViewChanged, Helpers.Constants.MAP_POINT_TOOL_DEACTIVATED);
            return base.OnToolDeactivateAsync(hasMapViewChanged);
        }

        protected override Task<bool> OnSketchCompleteAsync(ArcGIS.Core.Geometry.Geometry geometry)
        {
            try
            {
                var mp = geometry as MapPoint;
                //Mediator.NotifyColleagues(Helpers.Constants.NEW_MAP_POINT, mp);
                VisibilitySketchMouseEvents(mp, Helpers.Constants.NEW_MAP_POINT);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return base.OnSketchCompleteAsync(geometry);
        }

        /// <summary>
        /// Method to handle the Tool Mouse Move event
        /// Get MapPoint from ClientPoint and notify
        /// </summary>
        /// <param name="e">MapViewMouseEventArgs</param>
        protected override void OnToolMouseMove(MapViewMouseEventArgs e)
        {
            try
            {
                QueuedTask.Run(() =>
                {
                    var mp = MapView.Active.ClientToMap(e.ClientPoint);
                    //Mediator.NotifyColleagues(Helpers.Constants.MOUSE_MOVE_POINT, mp);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        VisibilitySketchMouseEvents(mp, Helpers.Constants.MOUSE_MOVE_POINT);
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            base.OnToolMouseMove(e);
        }

        private void VisibilitySketchMouseEvents(MapPoint mp, string mouseevent)
        {
            VisibilityDockpaneViewModel vsVm = VisibilityModule.VisibuiltyVm;
            if (vsVm != null)
            {
                System.Windows.Controls.TabItem tabItem = vsVm.SelectedTab as System.Windows.Controls.TabItem;
                if (tabItem != null)
                {
                    if (tabItem.Header.Equals(Properties.Resources.LabelTabLLOS))
                    {
                        Views.VisibilityLLOSView vsLlos = (tabItem.Content as System.Windows.Controls.UserControl).Content as Views.VisibilityLLOSView;
                        ViewModels.ProLLOSViewModel llosVm = vsLlos.DataContext as ViewModels.ProLLOSViewModel;
                        llosVm.IsActiveTab = true;
                        if (mouseevent.Equals(Helpers.Constants.NEW_MAP_POINT))
                        {
                            llosVm.NewMapPoint.Execute(mp);
                        }
                        else if (mouseevent.Equals(Helpers.Constants.MOUSE_MOVE_POINT))
                        {
                            llosVm.MouseMovePoint.Execute(mp);
                        }
                    }
                    else if (tabItem.Header.Equals(Properties.Resources.LabelTabRLOS))
                    {
                        Views.VisibilityRLOSView vsRlos = (tabItem.Content as System.Windows.Controls.UserControl).Content as Views.VisibilityRLOSView;
                        ViewModels.ProRLOSViewModel rlosVm = vsRlos.DataContext as ViewModels.ProRLOSViewModel;
                        rlosVm.IsActiveTab = true;
                        if (mouseevent.Equals(Helpers.Constants.NEW_MAP_POINT))
                        {
                            rlosVm.NewMapPoint.Execute(mp);
                        }
                        else if (mouseevent.Equals(Helpers.Constants.MOUSE_MOVE_POINT))
                        {
                            rlosVm.MouseMovePoint.Execute(mp);
                        }
                    }
                }
            }
        }
        
        private void ToolActiveDeactive(bool activeOrDeactive, string mouseevent)
        {
            VisibilityDockpaneViewModel vsVm = VisibilityModule.VisibuiltyVm;
            if (vsVm != null)
            {
                System.Windows.Controls.TabItem tabItem = vsVm.SelectedTab as System.Windows.Controls.TabItem;
                if (tabItem != null)
                {
                    if (tabItem.Header.Equals(Properties.Resources.LabelTabLLOS))
                    {
                        Views.VisibilityLLOSView vsLlos = (tabItem.Content as System.Windows.Controls.UserControl).Content as Views.VisibilityLLOSView;
                        ViewModels.ProLLOSViewModel llosVm = vsLlos.DataContext as ViewModels.ProLLOSViewModel;
                        llosVm.IsActiveTab = true;
                        if (mouseevent.Equals(Helpers.Constants.MAP_POINT_TOOL_ACTIVATED))
                        {
                            llosVm.MapPointToolActivated.Execute(activeOrDeactive);
                        }
                        else if (mouseevent.Equals(Helpers.Constants.MAP_POINT_TOOL_DEACTIVATED))
                        {
                            llosVm.MapPointToolDeActivated.Execute(activeOrDeactive);
                        }
                    }
                    else if (tabItem.Header.Equals(Properties.Resources.LabelTabRLOS))
                    {
                        Views.VisibilityRLOSView vsRlos = (tabItem.Content as System.Windows.Controls.UserControl).Content as Views.VisibilityRLOSView;
                        ViewModels.ProRLOSViewModel rlosVm = vsRlos.DataContext as ViewModels.ProRLOSViewModel;
                        rlosVm.IsActiveTab = true;
                        if (mouseevent.Equals(Helpers.Constants.MAP_POINT_TOOL_ACTIVATED))
                        {
                            rlosVm.MapPointToolActivated.Execute(activeOrDeactive);
                        }
                        else if (mouseevent.Equals(Helpers.Constants.MAP_POINT_TOOL_DEACTIVATED))
                        {
                            rlosVm.MapPointToolDeActivated.Execute(activeOrDeactive);
                        }
                    }
                }
            }
        }
    }
}
