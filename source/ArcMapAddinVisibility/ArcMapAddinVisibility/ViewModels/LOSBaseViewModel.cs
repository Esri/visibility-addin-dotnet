using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.DataSourcesRaster;
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
            AngularUnitType = AngularTypes.DEGREES;

            ObserverPoints = new ObservableCollection<IPoint>();
            ToolMode = MapPointToolMode.Unknown;
            SurfaceLayerNames = new ObservableCollection<string>();
            SelectedSurfaceName = string.Empty;

            Mediator.Register(Constants.MAP_TOC_UPDATED, OnMapTocUpdated);

            DeletePointCommand = new RelayCommand(OnDeletePointCommand);
            DeleteAllPointsCommand = new RelayCommand(OnDeleteAllPointsCommand); 

            GuidPointDictionary = new Dictionary<string, IPoint>();
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
        public Dictionary<string, IPoint> GuidPointDictionary { get; set; } 
        public AngularTypes AngularUnitType { get; set; }

        #endregion

        #region Commands

        public RelayCommand DeletePointCommand { get; set; }
        public RelayCommand DeleteAllPointsCommand { get; set; }

        /// <summary>
        /// Command method to delete points
        /// </summary>
        /// <param name="obj"></param>
        internal virtual void OnDeletePointCommand(object obj)
        {
            // remove observer points
            var items = obj as IList;
            var points = items.Cast<IPoint>().ToList();

            if (points == null)
                return;

            DeletePoints(points);
        }

        internal virtual void OnDeleteAllPointsCommand(object obj)
        {
            DeletePoints(ObserverPoints.ToList<IPoint>());
        }

        private void DeletePoints(List<IPoint> pointList)
        {
            if (pointList == null || !pointList.Any())
                return;

            // temp list of point's graphic element's guids
            var guidList = new List<string>();

            foreach (var point in pointList)
            {
                ObserverPoints.Remove(point);

                // remove graphic
                var kvp = GuidPointDictionary.FirstOrDefault(i => i.Value == point);

                guidList.Add(kvp.Key);
            }

            RemoveGraphics(guidList);

            foreach (var guid in guidList)
            {
                if (GuidPointDictionary.ContainsKey(guid))
                    GuidPointDictionary.Remove(guid);
            }
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Method called when the map TOC is updated
        /// Reset surface names
        /// </summary>
        /// <param name="obj">not used</param>
        private void OnMapTocUpdated(object obj)
        {
            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return;

            var map = ArcMap.Document.FocusMap;

            ResetSurfaceNames(map);
        }

        /// <summary>
        /// Override this method to implement a "Mode" to separate the input of
        /// observer points and target points
        /// </summary>
        /// <param name="obj">ToolMode string from resource file</param>
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

        /// <summary>
        /// Override this event to collect observer points based on tool mode
        /// </summary>
        /// <param name="obj">MapPointToolMode</param>
        internal override void OnNewMapPointEvent(object obj)
        {
            if (!IsActiveTab)
                return;

            var point = obj as IPoint;

            if (point == null || !IsValidPoint(point, true))
                return;

            // ok, we have a point
            if (ToolMode == MapPointToolMode.Observer)
            {
                // in tool mode "Observer" we add observer points
                // otherwise ignore
                ObserverPoints.Insert(0, point);
                var color = new RgbColorClass() { Blue = 255 } as IColor;
                var guid = AddGraphicToMap(point, color, true);
                UpdatePointDictionary(point, guid);
            }
        }
        /// <summary>
        /// Method to check to see point is withing the currently selected surface
        /// returns true if there is no surface selected or point is contained by layer AOI
        /// returns false if the point is not contained in the layer AOI
        /// </summary>
        /// <param name="point">IPoint to validate</param>
        /// <param name="showPopup">boolean to show popup message or not</param>
        /// <returns></returns>
        internal bool IsValidPoint(IPoint point, bool showPopup = false)
        {
            var validPoint = true;

            if (!string.IsNullOrWhiteSpace(SelectedSurfaceName) && ArcMap.Document != null && ArcMap.Document.FocusMap != null)
            {
                validPoint = IsPointWithinExtent(point, GetLayerFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName).AreaOfInterest);

                if (validPoint == false && showPopup)
                    System.Windows.Forms.MessageBox.Show(Properties.Resources.MsgOutOfAOI);
            }

            return validPoint;
        }

        #endregion

        /// <summary>
        /// Enumeration used for the different tool modes
        /// </summary>
        internal enum MapPointToolMode : int
        {
            Unknown = 0,
            Observer = 1,
            Target = 2
        }
        /// <summary>
        /// Dictionary to keep track of the related graphic element with IPoints (Observers and Targets)
        /// </summary>
        /// <param name="point">IPoint</param>
        /// <param name="guid">guid string of the related graphic element</param>
        internal void UpdatePointDictionary(IPoint point, string guid)
        {
            if (!GuidPointDictionary.ContainsKey(guid))
                GuidPointDictionary.Add(guid, point);
        }
        /// <summary>
        /// Method used to check to see if a point is contained by an envelope
        /// </summary>
        /// <param name="point">IPoint</param>
        /// <param name="env">IEnvelope</param>
        /// <returns></returns>
        internal bool IsPointWithinExtent(IPoint point, IEnvelope env)
        {
            var relationOp = env as IRelationalOperator;

            if (relationOp == null)
                return false;

            return relationOp.Contains(point);
        }

        /// <summary>
        /// Method to get a z offset distance in the correct units for the map
        /// </summary>
        /// <param name="map">IMap</param>
        /// <param name="offset">the input offset</param>
        /// <param name="zFactor">ISurface z factor</param>
        /// <param name="distanceType">the "from" distance unit type</param>
        /// <returns></returns>
        internal double GetOffsetInZUnits(IMap map, double offset, double zFactor, DistanceTypes distanceType)
        {
            if (map.SpatialReference == null)
                return offset;

            double offsetInMapUnits = 0.0;
            DistanceTypes distanceTo = DistanceTypes.Meters; // default to meters

            var pcs = map.SpatialReference as IProjectedCoordinateSystem;

            if (pcs != null)
            {
                // need to convert the offset from the input distance type to the spatial reference linear type
                // then apply the zFactor
                distanceTo = GetDistanceType(pcs.CoordinateUnit.FactoryCode);
            }

            offsetInMapUnits = GetDistanceFromTo(distanceType, distanceTo, offset);

            var result = offsetInMapUnits / zFactor;

            return result;
        }

        /// <summary>
        /// Method to get a ISurface from a map with layer name
        /// </summary>
        /// <param name="map">IMap that contains surface layer</param>
        /// <param name="name">Name of the layer that you are looking for</param>
        /// <returns>ISurface</returns>
        public ISurface GetSurfaceFromMapByName(IMap map, string name)
        {
            for (int x = 0; x < map.LayerCount; x++)
            {
                var layer = map.get_Layer(x);

                if (layer == null || layer.Name != name)
                    continue;

                    var tin = layer as ITinLayer;
                    if (tin != null)
                {
                        return tin.Dataset as ISurface;
                }

                var rasterSurface = new RasterSurfaceClass() as IRasterSurface;
                ISurface surface = null;

                var mosaicLayer = layer as IMosaicLayer;
                var rasterLayer = layer as IRasterLayer;

                if (mosaicLayer != null)
                {
                    rasterSurface.PutRaster(mosaicLayer.PreviewLayer.Raster, 0);
                }
                else if (rasterLayer != null)
                {
                    rasterSurface.PutRaster(rasterLayer.Raster, 0);
                }

                surface = rasterSurface as ISurface;

                if (surface != null)
                    return surface;
            }

            return null;
        }
        /// <summary>
        /// returns ILayer if found in the map layer collection
        /// </summary>
        /// <param name="map">IMap</param>
        /// <param name="name">string name of layer</param>
        /// <returns></returns>
        public ILayer GetLayerFromMapByName(IMap map, string name)
        {
            for (int x = 0; x < map.LayerCount; x++)
            {
                var layer = map.get_Layer(x);

                if (layer == null || layer.Name != name)
                    continue;

                return layer;
            }

            return null;
        }

        /// <summary>
        /// Method to get all the names of the raster/tin layers that support ISurface
        /// we use this method to populate a combobox for input selection of surface layer
        /// </summary>
        /// <param name="map">IMap</param>
        /// <returns></returns>
        public List<string> GetSurfaceNamesFromMap(IMap map, bool IncludeTinLayers = false)
        {
            var list = new List<string>();

            for (int x = 0; x < map.LayerCount; x++)
            {
                try
                {
                    var layer = map.get_Layer(x);

                    if (layer == null)
                        continue;

                    var tin = layer as ITinLayer;

                    if (tin != null)
                    {
                        if (IncludeTinLayers)
                            list.Add(layer.Name);
                        
                        continue;
                    }

                    var rasterSurface = new RasterSurfaceClass() as IRasterSurface; 
                    ISurface surface = null;

                    var ml = layer as IMosaicLayer;
                    
                    if(ml != null)
                    {
                        if(ml.PreviewLayer != null && ml.PreviewLayer.Raster != null)
                        {
                            rasterSurface.PutRaster(ml.PreviewLayer.Raster, 0);

                            surface = rasterSurface as ISurface;
                            if (surface != null)
                        list.Add(layer.Name);
                        }
                        continue;
                    }

                    var rasterLayer = layer as IRasterLayer;
                    if (rasterLayer != null)
                    {
                        rasterSurface.PutRaster(rasterLayer.Raster, 0);

                        surface = rasterSurface as ISurface;
                    if (surface != null)
                        list.Add(layer.Name);
                        continue;
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return list;
        }

        /// <summary>
        /// Override to add aditional items in the class to reset tool
        /// </summary>
        /// <param name="toolReset"></param>
        internal override void Reset(bool toolReset)
        {
            base.Reset(toolReset);

            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return;

            // reset surface names OC
            ResetSurfaceNames(ArcMap.Document.FocusMap);

            // reset observer points
            ObserverPoints.Clear();

            ClearTempGraphics();

            GuidPointDictionary.Clear();
        }

        /// <summary>
        /// Method used to reset the currently selected surfacename 
        /// Use when toc items or map changes, on tab selection changed, etc
        /// </summary>
        /// <param name="map">IMap</param>
        internal void ResetSurfaceNames(IMap map)
        {
            // keep the current selection if it's still valid
            var tempName = SelectedSurfaceName;

            SurfaceLayerNames.Clear();

            foreach (var name in GetSurfaceNamesFromMap(map, (this.GetType() == typeof(LLOSViewModel))? true:false))
                SurfaceLayerNames.Add(name);

            if (SurfaceLayerNames.Contains(tempName))
                SelectedSurfaceName = tempName;
            else if (SurfaceLayerNames.Any())
                SelectedSurfaceName = SurfaceLayerNames[0];
            else
                SelectedSurfaceName = string.Empty;

            RaisePropertyChanged(() => SelectedSurfaceName);
        }
    }
}
