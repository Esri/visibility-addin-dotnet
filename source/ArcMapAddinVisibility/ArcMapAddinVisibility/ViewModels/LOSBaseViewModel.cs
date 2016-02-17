using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Analyst3D;

namespace ArcMapAddinVisibility.ViewModels
{
    public class LOSBaseViewModel : TabBaseViewModel
    {
        public LOSBaseViewModel()
        {

        }

        public ISurface GetSurfaceFromMap(IMap map)
        {
            if (map == null)
                return null;

            for (int x = 0; x < map.LayerCount; x++ )
            {
                var layer = map.get_Layer(x);

                var rasterLayer = layer as IRasterLayer;
                if (rasterLayer == null)
                    continue;

                var rs = new RasterSurfaceClass() as IRasterSurface;

                rs.PutRaster(rasterLayer.Raster, 0);

                var surface = rs as ISurface;
                if (surface != null)
                    return surface;
            }

            return null;
        }
    }
}
