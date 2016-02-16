using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ArcMapAddinVisibility
{
    public class MapPointTool : ESRI.ArcGIS.Desktop.AddIns.Tool
    {
        public MapPointTool()
        {
        }

        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }
    }

}
