using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using VisibilityLibrary.Models;
using VisibilityLibrary;

namespace ProAppVisibilityModule.Helpers
{
    public static class MapPointHelper
    {
    
        public static string GetMapPointAsDisplayString(MapPoint mp)
        {
            if (mp == null)
                return "NA";

            var result = string.Format("{0:0.0} {1:0.0}", mp.Y, mp.X);

            // .ToGeoCoordinate function calls will fail if there is no Spatial Reference
            if (mp.SpatialReference == null)
                return result;

            ToGeoCoordinateParameter tgparam = null;
            
            try
            {
                switch (VisibilityConfig.AddInConfig.DisplayCoordinateType)
                {
                    case CoordinateTypes.DD:
                        tgparam = new ToGeoCoordinateParameter(GeoCoordinateType.DD);
                        tgparam.NumDigits = 6;
                        result = mp.ToGeoCoordinateString(tgparam);
                        break;
                    case CoordinateTypes.DDM:
                        tgparam = new ToGeoCoordinateParameter(GeoCoordinateType.DDM);
                        tgparam.NumDigits = 4;
                        result = mp.ToGeoCoordinateString(tgparam);
                        break;
                    case CoordinateTypes.DMS:
                        tgparam = new ToGeoCoordinateParameter(GeoCoordinateType.DMS);
                        tgparam.NumDigits = 2;
                        result = mp.ToGeoCoordinateString(tgparam);
                        break;
                    case CoordinateTypes.GARS:
                        tgparam = new ToGeoCoordinateParameter(GeoCoordinateType.GARS);
                        result = mp.ToGeoCoordinateString(tgparam);
                        break;
                    case CoordinateTypes.MGRS:
                        tgparam = new ToGeoCoordinateParameter(GeoCoordinateType.MGRS);
                        result = mp.ToGeoCoordinateString(tgparam);
                        break;
                    case CoordinateTypes.USNG:
                        tgparam = new ToGeoCoordinateParameter(GeoCoordinateType.USNG);
                        tgparam.NumDigits = 5;
                        result = mp.ToGeoCoordinateString(tgparam);
                        break;
                    case CoordinateTypes.UTM:
                        tgparam = new ToGeoCoordinateParameter(GeoCoordinateType.UTM);
                        tgparam.GeoCoordMode = ToGeoCoordinateMode.UtmNorthSouth;
                        result = mp.ToGeoCoordinateString(tgparam);
                        break;
                    default:
                        break;
                }
            }
            catch(Exception ex)
            {
                // do nothing
            }
            return result;
        }
    }


}
