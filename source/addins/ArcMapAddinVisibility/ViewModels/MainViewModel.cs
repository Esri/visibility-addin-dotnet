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
using ESRI.ArcGIS.Carto;
using VisibilityLibrary;
using VisibilityLibrary.Helpers;
using VisibilityLibrary.Views;
using VisibilityLibrary.ViewModels;
using VisibilityLibrary.Models;

namespace ArcMapAddinVisibility.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public MainViewModel()
        {
            // set some views
            _llosView = new VisibilityLLOSView();
            _llosView.DataContext = new LLOSViewModel();
            _rlosView = new VisibilityRLOSView();
            _rlosView.DataContext = new RLOSViewModel();

            Events_ActiveViewChanged();

            // listen to some map events
            ArcMap.Events.ActiveViewChanged += Events_ActiveViewChanged;

            VisibilityConfig.AddInConfig.LoadConfiguration();
        }
        private IMap map = null;
        void Events_ActiveViewChanged()
        {
            map = ArcMap.Document.FocusMap as IMap;

            if (map == null)
                return;

            // hook events

            var viewEvents = map as IActiveViewEvents_Event;
            if (viewEvents == null)
                return;

            viewEvents.FocusMapChanged += viewEvents_FocusMapChanged;
            viewEvents.ItemAdded += viewEvents_ItemAdded;
            viewEvents.ItemDeleted += viewEvents_ItemDeleted;

            NotifyMapTOCUpdated();
        }

        void viewEvents_ItemDeleted(object Item)
        {
            NotifyMapTOCUpdated();
        }

        void viewEvents_ItemAdded(object Item)
        {
            NotifyMapTOCUpdated();
        }

        void viewEvents_FocusMapChanged()
        {
            NotifyMapTOCUpdated();
        }

        private void NotifyMapTOCUpdated()
        {
            Mediator.NotifyColleagues(Constants.MAP_TOC_UPDATED, null);
        }

        #region Properties

        object selectedTab = null;
        public object SelectedTab
        {
            get { return selectedTab; }
            set
            {
                if (selectedTab == value)
                    return;

                selectedTab = value;
                var tabItem = selectedTab as TabItem;
                Mediator.NotifyColleagues(Constants.TAB_ITEM_SELECTED, ((tabItem.Content as UserControl).Content as UserControl).DataContext);
            }
        }

        #endregion

        #region Views

        private VisibilityLLOSView _llosView;
        public VisibilityLLOSView LLOSView
        {
            get { return _llosView; }
            set
            {
                _llosView = value;
            }
        }
        private VisibilityRLOSView _rlosView;
        public VisibilityRLOSView RLOSView
        {
            get { return _rlosView; }
            set
            {
                _rlosView = value;
            }
        }

        #endregion

    }
}
