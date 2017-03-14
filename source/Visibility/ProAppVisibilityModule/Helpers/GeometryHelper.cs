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
using System.Collections.Generic;

using ArcGIS.Core.Geometry;

namespace ProAppVisibilityModule.Helpers
{
    /// <summary>
    /// Geometry Helper Utilities
    /// </summary>
    public static class GeometryHelper
    {
        /// <summary>
        /// Returns a polygon with a circular ring sector (like a wiper blade swipe with inner and outer radius)
        /// from the input parameters
        /// </summary>
        public static Geometry constructCircularRingSector(MapPoint centerPoint,
            double innerDistanceInMapUnits, double outerDistanceInMapUnits,
            double horizontalStartAngleInBearing, double horizontalEndAngleInBearing,
            SpatialReference sr)
        {
            var points = new List<MapPoint>();

            MapPoint startPoint = null;

            if (innerDistanceInMapUnits == 0.0)
            {
                startPoint = centerPoint;
                points.Add(startPoint);
            }

            // Tricky - if angle cuts across 360, need to adjust for this case (ex. Angle: 270->90)
            if (horizontalStartAngleInBearing > horizontalEndAngleInBearing)
                horizontalStartAngleInBearing = -(360.0 - horizontalStartAngleInBearing);

            double minAngle = Math.Min(horizontalStartAngleInBearing, horizontalEndAngleInBearing);
            double maxAngle = Math.Max(horizontalStartAngleInBearing, horizontalEndAngleInBearing);
            double step = 5.0;

            // Draw Outer Arc of Ring
            // Implementation Note: because of the unique shape of this ring, 
            // it was easier to manually create these points than use EllipticArcBuilder 
            for (double angle = minAngle; angle <= maxAngle; angle += step)
            {
                double cartesianAngle = (450 - angle) % 360;
                double angleInRadians = cartesianAngle * (Math.PI / 180.0);
                double x = centerPoint.X + (outerDistanceInMapUnits * Math.Cos(angleInRadians));
                double y = centerPoint.Y + (outerDistanceInMapUnits * Math.Sin(angleInRadians));

                MapPoint pointToAdd = MapPointBuilder.CreateMapPoint(x, y, sr);
                points.Add(pointToAdd);

                if (startPoint == null)
                    startPoint = pointToAdd;
            }

            if (innerDistanceInMapUnits > 0.0)
            {
                // Draw Inner Arc of Ring - if inner distance set
                for (double angle = maxAngle; angle >= minAngle; angle -= step)
                {
                    double cartesianAngle = (450 - angle) % 360;
                    double angleInRadians = cartesianAngle * (Math.PI / 180.0);
                    double x = centerPoint.X + (innerDistanceInMapUnits * Math.Cos(angleInRadians));
                    double y = centerPoint.Y + (innerDistanceInMapUnits * Math.Sin(angleInRadians));

                    points.Add(MapPointBuilder.CreateMapPoint(x, y, sr));
                }
            }

            // close Polygon
            points.Add(startPoint);

            PolygonBuilder pb = new PolygonBuilder();
            pb.AddPart(points);

            // TRICKY: Observer Point must be included in GP Tool Mask, 
            // so if masking mimimum distance, add observer point back in
            if (innerDistanceInMapUnits > 0.0)
            {
                // Buffer 1% of distance
                var observerBuffer = GeometryEngine.Buffer(centerPoint, outerDistanceInMapUnits * 0.01);

                // Tricky/Workaround: GP mask did not work with Multiparts with arcs so had to convert to densified polygon
                var observerBufferDensified = GeometryEngine.DensifyByLength(observerBuffer, outerDistanceInMapUnits * 0.002);
                var observerBufferPolygon = observerBufferDensified as Polygon;

                pb.AddPart(observerBufferPolygon.Points);
            }

            return pb.ToGeometry();
        }
    }

}
