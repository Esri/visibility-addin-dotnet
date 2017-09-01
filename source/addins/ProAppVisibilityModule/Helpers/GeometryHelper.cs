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
        /// Returns a polygon with a range fan(circular ring sector - like a donut wedge or wiper blade swipe with inner and outer radius)
        /// from the input parameters
        /// Input Angles must be 0-360 degrees
        /// </summary>
        public static Geometry ConstructRangeFan(MapPoint centerPoint,
            double innerDistanceInMapUnits, double outerDistanceInMapUnits,
            double horizontalStartAngleInBearing, double horizontalEndAngleInBearing,
            SpatialReference sr, double incrementAngleStep = 1.0)
        {
            // Check inputs
            if ((centerPoint == null) || (sr == null) ||
                (innerDistanceInMapUnits < 0.0) || (outerDistanceInMapUnits < 0.0) ||
                (horizontalStartAngleInBearing < 0.0) || (horizontalStartAngleInBearing > 360.0) ||
                (horizontalEndAngleInBearing < 0.0) || (horizontalEndAngleInBearing > 360.0))
                return null;

            // Tricky - if angle cuts across 360, need to adjust for this case (ex. Angle: 270->90)
            if (horizontalStartAngleInBearing > horizontalEndAngleInBearing)
                horizontalStartAngleInBearing = -(360.0 - horizontalStartAngleInBearing);

            double deltaAngle = Math.Abs(horizontalStartAngleInBearing - horizontalEndAngleInBearing);

            // if full circle(or greater), return donut section with inner/outer rings
            if ((deltaAngle == 0.0) || (deltaAngle >= 360.0))
            {
                // Just add 2 concentric circle buffers
                PolygonBuilder donutPb = new PolygonBuilder();

                EllipticArcSegment circularArcOuter = 
                    EllipticArcBuilder.CreateEllipticArcSegment((Coordinate2D)centerPoint, 
                    outerDistanceInMapUnits, esriArcOrientation.esriArcClockwise, sr);

                donutPb.AddPart(new List<Segment> { circularArcOuter });

                if (innerDistanceInMapUnits > 0.0)
                {
                    EllipticArcSegment circularArcInner =
                        EllipticArcBuilder.CreateEllipticArcSegment((Coordinate2D)centerPoint,
                        innerDistanceInMapUnits, esriArcOrientation.esriArcCounterClockwise, sr);

                    donutPb.AddPart(new List<Segment> { circularArcInner });
                }

                return donutPb.ToGeometry();
            }

            // Otherwise if range fan, construct that
            var points = new List<MapPoint>();

            MapPoint startPoint = null;

            if (innerDistanceInMapUnits <= 0.0)
            {
                startPoint = centerPoint;
                points.Add(startPoint);
            }

            double minAngle = Math.Min(horizontalStartAngleInBearing, horizontalEndAngleInBearing);
            double maxAngle = Math.Max(horizontalStartAngleInBearing, horizontalEndAngleInBearing);

            // don't let this create more than 360 points per arc
            if ((deltaAngle / incrementAngleStep) > 360.0)
                incrementAngleStep = deltaAngle / 360.0;

            // Draw Outer Arc of Ring
            // Implementation Note: because of the unique shape of this ring, 
            // it was easier to manually create these points than use EllipticArcBuilder 
            for (double angle = minAngle; angle <= maxAngle; angle += incrementAngleStep)
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
                for (double angle = maxAngle; angle >= minAngle; angle -= incrementAngleStep)
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

            return pb.ToGeometry();
        }
    }

}
