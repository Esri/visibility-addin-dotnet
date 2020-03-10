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
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using ProAppVisibilityModule.Helpers;
using System.IO;
using ProAppVisibilityModule.Common.Enums;

namespace ProAppVisibilityModule.Models
{
    public class VisibilityConfig : NotificationObject
    {
        public VisibilityConfig()
        {
        }

        public static VisibilityConfig AddInConfig = new VisibilityConfig();

        private CoordinateTypes displayCoordinateType = CoordinateTypes.None;
        public CoordinateTypes DisplayCoordinateType
        {
            get { return displayCoordinateType; }
            set
            {
                displayCoordinateType = value;
                RaisePropertyChanged(() => DisplayCoordinateType);
                DisplayCoordinateTypeChange();
            }
        }

        #region Public methods

        public void SaveConfiguration()
        {
            XmlWriter writer = null;

            try
            {
                var filename = GetConfigFilename();

                XmlSerializer x = new XmlSerializer(GetType());
                writer = new XmlTextWriter(filename, Encoding.UTF8);

                x.Serialize(writer, this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        public void LoadConfiguration()
        {
            TextReader tr = null;

            try
            {
                var filename = GetConfigFilename();

                if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
                    return;

                XmlSerializer x = new XmlSerializer(GetType());
                tr = new StreamReader(filename);
                var temp = x.Deserialize(tr) as VisibilityConfig;

                if (temp == null)
                    return;

                DisplayCoordinateType = temp.DisplayCoordinateType;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                if (tr != null)
                    tr.Close();
            }
        }

        #endregion Public methods

        #region Private methods

        private string GetConfigFilename()
        {
            return this.GetType().Assembly.Location + ".config";
        }

        private void DisplayCoordinateTypeChange()
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
                        llosVM.DisplayCoordinateTypeChanged.Execute(null);
                    }
                    else if (tabItem.Header.Equals(Properties.Resources.LabelTabRLOS))
                    {
                        Views.VisibilityRLOSView vsRLOS = (tabItem.Content as System.Windows.Controls.UserControl).Content as Views.VisibilityRLOSView;
                        ViewModels.ProRLOSViewModel rlosVM = vsRLOS.DataContext as ViewModels.ProRLOSViewModel;
                        rlosVM.DisplayCoordinateTypeChanged.Execute(null);
                    }
                }
            }
        }

        #endregion Private methods

    }
}
