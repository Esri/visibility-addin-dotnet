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
using ProAppVisibilityModule.Helpers;
using ArcGIS.Core.Geometry;

namespace ProAppVisibilityModule
{
    internal class VisibilityMapTool : MapTool
    {
        private const string MAP_POINT_TOOL_ACTIVATED = "MAP_POINT_TOOL_ACTIVATED";
        private const string MAP_POINT_TOOL_DEACTIVATED = "MAP_POINT_TOOL_DEACTIVATED";
        private const string NEW_MAP_POINT = "NEW_MAP_POINT";
        private const string MOUSE_MOVE_POINT = "MOUSE_MOVE_POINT";
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
            ToolActiveDeactive(active, MAP_POINT_TOOL_ACTIVATED);
            return base.OnToolActivateAsync(active);
        }

        protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
        {
            ToolActiveDeactive(hasMapViewChanged, MAP_POINT_TOOL_DEACTIVATED);
            return base.OnToolDeactivateAsync(hasMapViewChanged);
        }

        protected override Task<bool> OnSketchCompleteAsync(ArcGIS.Core.Geometry.Geometry geometry)
        {
            try
            {
                var mp = geometry as MapPoint;
                VisibilitySketchMouseEvents(mp, NEW_MAP_POINT);
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
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        VisibilitySketchMouseEvents(mp, MOUSE_MOVE_POINT);
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
            VisibilityDockpaneViewModel vsDKVM = VisibilityModule.VisibiltyVM;
            if (vsDKVM != null)
            {
                System.Windows.Controls.TabItem tabItem = vsDKVM.SelectedTab as System.Windows.Controls.TabItem;
                if (tabItem != null)
                {
                    if (tabItem.Header.Equals(Properties.Resources.LabelTabLLOS))
                    {
                        Views.VisibilityLLOSView vsLLOS = (tabItem.Content as System.Windows.Controls.UserControl).Content as Views.VisibilityLLOSView;
                        ViewModels.ProLLOSViewModel llosVM = vsLLOS.DataContext as ViewModels.ProLLOSViewModel;
                        if (mouseevent.Equals(NEW_MAP_POINT))
                        {
                            llosVM.OnMapClickEvent(mp);
                            llosVM.NewMapPoint.Execute(mp);
                        }
                        else if (mouseevent.Equals(MOUSE_MOVE_POINT))
                        {
                            llosVM.MouseMovePoint.Execute(mp);
                        }
                    }
                    else if (tabItem.Header.Equals(Properties.Resources.LabelTabRLOS))
                    {
                        Views.VisibilityRLOSView vsRLOS = (tabItem.Content as System.Windows.Controls.UserControl).Content as Views.VisibilityRLOSView;
                        ViewModels.ProRLOSViewModel rlosVM = vsRLOS.DataContext as ViewModels.ProRLOSViewModel;
                        if (mouseevent.Equals(NEW_MAP_POINT))
                        {
                            rlosVM.OnMapClickEvent(mp);
                            rlosVM.NewMapPoint.Execute(mp);
                        }
                        else if (mouseevent.Equals(MOUSE_MOVE_POINT))
                        {
                            rlosVM.MouseMovePoint.Execute(mp);
                        }
                    }
                }
            }
        }


        private void ToolActiveDeactive(bool activeOrDeactive, string mouseevent)
        {
            VisibilityDockpaneViewModel vsVM = VisibilityModule.VisibiltyVM;
            if (vsVM != null)
            {
                Views.VisibilityLLOSView vsLLOS = vsVM.LLOSView;
                if(vsLLOS != null)
                {
                    ViewModels.ProLLOSViewModel llosVM = vsLLOS.DataContext as ViewModels.ProLLOSViewModel;
                    if (mouseevent.Equals(MAP_POINT_TOOL_ACTIVATED))
                    {
                        llosVM.MapPointToolActivated.Execute(activeOrDeactive);
                    }
                    else if (mouseevent.Equals(MAP_POINT_TOOL_DEACTIVATED))
                    {
                        llosVM.MapPointToolDeActivated.Execute(activeOrDeactive);
                    }
                }
                

                Views.VisibilityRLOSView vsRLOS = vsVM.RLOSView;
                if(vsRLOS != null)
                {
                    ViewModels.ProRLOSViewModel rlosVM = vsRLOS.DataContext as ViewModels.ProRLOSViewModel;
                    if (mouseevent.Equals(MAP_POINT_TOOL_ACTIVATED))
                    {
                        rlosVM.MapPointToolActivated.Execute(activeOrDeactive);
                    }
                    else if (mouseevent.Equals(MAP_POINT_TOOL_DEACTIVATED))
                    {
                        rlosVM.MapPointToolDeActivated.Execute(activeOrDeactive);
                    }
                }
            }
        }

    }
}
