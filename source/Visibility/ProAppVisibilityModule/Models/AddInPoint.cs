using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using VisibilityLibrary.Helpers;
using ProAppVisibilityModule.Helpers;

namespace ArcMapAddinVisibility.Models
{
    public class AddInPoint : NotificationObject
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
            get { return MapPointHelper.GetMapPointAsDisplayString(Point); }
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
