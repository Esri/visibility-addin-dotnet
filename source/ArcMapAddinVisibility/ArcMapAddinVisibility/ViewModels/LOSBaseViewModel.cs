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
using ArcMapAddinVisibility.Helpers;

namespace ArcMapAddinVisibility.ViewModels
{
    public class LOSBaseViewModel : TabBaseViewModel
    {
        public LOSBaseViewModel()
        {
            ObserverOffset = 2.0;
            TargetOffset = 0.0;
            OffsetUnitType = DistanceTypes.Meters;
            AngularUnitType = AngularTypes.Degrees;

            ObserverPoints = new ObservableCollection<IPoint>();
            ToolMode = MapPointToolMode.Unknown;
            SurfaceLayerNames = new ObservableCollection<string>();
            SelectedSurfaceName = string.Empty;

            Mediator.Register(Constants.MAP_TOC_UPDATED, OnMapTocUpdated);
        }

        #region Properties

        private double? observerOffset;
        public double? ObserverOffset 
        {
            get { return observerOffset; }
            set
            {
                observerOffset = value;
                RaisePropertyChanged(() => ObserverOffset);

                if (!observerOffset.HasValue)
                    throw new ArgumentException(Properties.Resources.AEInvalidInput);
            }
        }
        private double? targetOffset;
        public double? TargetOffset 
        {
            get { return targetOffset; } 
            set
            {
                targetOffset = value;
                RaisePropertyChanged(() => TargetOffset);

                if (!targetOffset.HasValue)
                    throw new ArgumentException(Properties.Resources.AEInvalidInput);
            }
        }

        internal MapPointToolMode ToolMode { get; set; }
        public ObservableCollection<IPoint> ObserverPoints { get; set; }
        public ObservableCollection<string> SurfaceLayerNames { get; set; }
        public string SelectedSurfaceName { get; set; }
        public DistanceTypes OffsetUnitType { get; set; }
        public AngularTypes AngularUnitType { get; set; }

        #endregion

        #region Commands


        #endregion

        #region Event handlers

        private void OnMapTocUpdated(object obj)
        {
            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return;

            var map = ArcMap.Document.FocusMap;

            var tempName = SelectedSurfaceName;

            SurfaceLayerNames.Clear();
            foreach (var name in GetSurfaceNamesFromMap(map))
                SurfaceLayerNames.Add(name);
            if (SurfaceLayerNames.Contains(tempName))
                SelectedSurfaceName = tempName;
            else if (SurfaceLayerNames.Any())
                SelectedSurfaceName = SurfaceLayerNames[0];
            else
                SelectedSurfaceName = string.Empty;

            RaisePropertyChanged(() => SelectedSurfaceName);
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
            {
                ObserverPoints.Insert(0, point);
                //TODO change color
                AddGraphicToMap(point, true);
            }
        }

        #endregion

        internal enum MapPointToolMode : int
        {
            Unknown = 0,
            Observer = 1,
            Target = 2
        }

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

        internal override void Reset(bool toolReset)
        {
            base.Reset(toolReset);

            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return;

            // reset surface names OC
            var names = GetSurfaceNamesFromMap(ArcMap.Document.FocusMap);

            SurfaceLayerNames.Clear();

            foreach (var name in names)
                SurfaceLayerNames.Add(name);

            if (SurfaceLayerNames.Any())
                SelectedSurfaceName = SurfaceLayerNames[0];

            RaisePropertyChanged(() => SelectedSurfaceName);

            // reset observer points
            ObserverPoints.Clear();

            ClearTempGraphics();
        }
    }
}
