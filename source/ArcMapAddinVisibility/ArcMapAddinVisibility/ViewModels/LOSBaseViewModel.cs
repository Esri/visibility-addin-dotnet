using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;

namespace ArcMapAddinVisibility.ViewModels
{
    public class LOSBaseViewModel : TabBaseViewModel
    {
        public LOSBaseViewModel()
        {
            ObserverPoints = new ObservableCollection<IPoint>();
            ToolMode = MapPointToolMode.Unknown;
        }

        internal enum MapPointToolMode : int
        {
            Unknown = 0,
            Observer = 1,
            Target = 2
        }

        internal MapPointToolMode ToolMode { get; set; }
        public ObservableCollection<IPoint> ObserverPoints { get; set; }

        public ISurface GetSurfaceFromMapByName(IMap map, string name)
        {
            for (int x = 0; x < map.LayerCount; x++)
            {
                var layer = map.get_Layer(x);

                if (layer == null || layer.Name != name)
                    continue;

                var rasterLayer = layer as IRasterLayer;
                if (rasterLayer == null)
                {
                    var tin = layer as ITinLayer;
                    if (tin != null)
                        return tin.Dataset as ISurface;

                    continue;
                }

                var rs = new RasterSurfaceClass() as IRasterSurface;

                rs.PutRaster(rasterLayer.Raster, 0);

                var surface = rs as ISurface;
                if (surface != null)
                    return surface;
            }

            return null;
        }

        public List<string> GetSurfaceNamesFromMap(IMap map)
        {
            var list = new List<string>();

            for (int x = 0; x < map.LayerCount; x++)
            {
                try
                {
                    var layer = map.get_Layer(x);

                    var rasterLayer = layer as IRasterLayer;
                    if (rasterLayer == null)
                    {
                        var tin = layer as ITinLayer;
                        if(tin == null)
                            continue;

                        list.Add(layer.Name);
                        continue;
                    }

                    var rs = new RasterSurfaceClass() as IRasterSurface;

                    rs.PutRaster(rasterLayer.Raster, 0);

                    var surface = rs as ISurface;
                    if (surface != null)
                        list.Add(layer.Name);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return list;
        }

        internal override void OnActivateTool(object obj)
        {
            var mode = obj.ToString();
            ToolMode = MapPointToolMode.Unknown;

            if (string.IsNullOrWhiteSpace(mode))
                return;

            if (mode == Properties.Resources.ToolModeObserver)
                ToolMode = MapPointToolMode.Observer;
            else if (mode == Properties.Resources.ToolModeTarget)
                ToolMode = MapPointToolMode.Target;

            base.OnActivateTool(obj);
        }

        internal override void OnNewMapPointEvent(object obj)
        {
            // lets test this out
            if (!IsActiveTab)
                return;

            var point = obj as IPoint;

            if (point == null)
                return;

            // ok, we have a point
            if (ToolMode == MapPointToolMode.Observer)
                ObserverPoints.Insert(0, point);
        }

        //public ISurface GetSurfaceFromMap(IMap map)
        //{
        //    if (map == null)
        //        return null;

        //    for (int x = 0; x < map.LayerCount; x++ )
        //    {
        //        var layer = map.get_Layer(x);

        //        var rasterLayer = layer as IRasterLayer;
        //        if (rasterLayer == null)
        //            continue;

        //        var rs = new RasterSurfaceClass() as IRasterSurface;

        //        rs.PutRaster(rasterLayer.Raster, 0);

        //        var surface = rs as ISurface;
        //        if (surface != null)
        //            return surface;
        //    }

        //    return null;
        //}
    }
}
