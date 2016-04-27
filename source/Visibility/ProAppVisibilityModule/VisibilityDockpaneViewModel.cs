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

using System.Windows.Controls;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using VisibilityLibrary.Helpers;
using VisibilityLibrary.Views;
using VisibilityLibrary.Models;
using ProAppVisibilityModule.ViewModels;

namespace ProAppVisibilityModule
{
    internal class VisibilityDockpaneViewModel : DockPane
    {
        private const string _dockPaneID = "ProAppVisibilityModule_VisibilityDockpane";

        protected VisibilityDockpaneViewModel() 
        {
            LLOSView = new VisibilityLLOSView();
            LLOSView.DataContext = new ProLLOSViewModel();

            RLOSView = new VisibilityRLOSView();
            RLOSView.DataContext = new ProRLOSViewModel();

            VisibilityConfig.AddInConfig.LoadConfiguration();
        }

        object selectedTab = null;
        /// <summary>
        /// Property to notify when tab selection changes
        /// </summary>
        public object SelectedTab
        {
            get { return selectedTab; }
            set
            {
                if (selectedTab == value)
                    return;

                selectedTab = value;
                var tabItem = selectedTab as TabItem;
                if (tabItem.Content != null && (tabItem.Content as UserControl).Content != null)
                    Mediator.NotifyColleagues(VisibilityLibrary.Constants.TAB_ITEM_SELECTED, ((tabItem.Content as UserControl).Content as UserControl).DataContext);
            }
        }

        #region Views

        public VisibilityLLOSView LLOSView { get; set;}
        public VisibilityRLOSView RLOSView { get; set; }

        #endregion

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "My DockPane";
        public string Heading
        {
            get { return _heading; }
            set
            {
                SetProperty(ref _heading, value, () => Heading);
            }
        }
    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class VisibilityDockpane_ShowButton : ArcGIS.Desktop.Framework.Contracts.Button
    {
        protected override void OnClick()
        {
            VisibilityDockpaneViewModel.Show();
        }
    }
}
