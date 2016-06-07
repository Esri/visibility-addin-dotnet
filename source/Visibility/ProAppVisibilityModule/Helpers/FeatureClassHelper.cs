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

using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProAppVisibilityModule.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProAppVisibilityModule.Helpers
{
    public class FeatureClassHelper
    {
        /// <summary>
        /// Create a feature class in the default geodatabase of the project.
        /// </summary>
        /// <param name="featureclassName">Name of the feature class to be created.</param>
        /// <param name="featureclassType">Type of feature class to be created. Options are:
        /// <list type="bullet">
        /// <item>POINT</item>
        /// <item>MULTIPOINT</item>
        /// <item>POLYLINE</item>
        /// <item>POLYGON</item></list></param>
        /// <returns></returns>
        public static async Task CreateLayer(string featureclassName, string featureclassType, bool zEnabled, bool addToMap)
        {
            List<object> arguments = new List<object>();
            // store the results in the default geodatabase
            arguments.Add(CoreModule.CurrentProject.DefaultGeodatabasePath);
            // name of the feature class
            arguments.Add(featureclassName);
            // type of geometry
            arguments.Add(featureclassType);
            // no template
            arguments.Add("");
            // m values
            arguments.Add("DISABLED");
            // z values
            if(zEnabled)
                arguments.Add("ENABLED");
            else
                arguments.Add("DISABLED");

            arguments.Add(MapView.Active.Map.SpatialReference);

            var environments = Geoprocessing.MakeEnvironmentArray(overwriteoutput: true);

            IGPResult result = await Geoprocessing.ExecuteToolAsync("CreateFeatureclass_management", 
                Geoprocessing.MakeValueArray(arguments.ToArray()), 
                environments, 
                null,
                null,
                addToMap ? GPExecuteToolFlags.Default : GPExecuteToolFlags.None);
        }

        /// <summary>
        /// Add a field to a layer
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldName"></param>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        public static async Task AddFieldToLayer(string tableName, string fieldName, string fieldType)
        {
            List<object> arguments = new List<object>();
            // in_table
            arguments.Add(tableName);
            // field_name
            arguments.Add(fieldName);
            // field_type
            arguments.Add(fieldType);
            // field_precision
            arguments.Add("");
            // field_scale
            arguments.Add("");
            // field_length
            arguments.Add("");
            // field_alias
            arguments.Add("");
            // field_is_nullable
            arguments.Add("");
            // field_is_required
            arguments.Add("");
            // field_domain
            arguments.Add("");

            IGPResult result = await Geoprocessing.ExecuteToolAsync("AddField_management", Geoprocessing.MakeValueArray(arguments.ToArray()));
        }

        /// <summary>
        /// Create sight lines
        /// </summary>
        /// <param name="observersFeatureLayer"></param>
        /// <param name="targetsFeatureLayer"></param>
        /// <param name="outLineFeatureLayer"></param>
        /// <param name="observerOffsetFieldName"></param>
        /// <param name="targetOffsetFieldName"></param>
        /// <returns></returns>
        public static async Task CreateSightLines(string observersFeatureLayer, 
                                                    string targetsFeatureLayer, 
                                                    string outLineFeatureLayer, 
                                                    string observerOffsetFieldName, 
                                                    string targetOffsetFieldName)
        {
            List<object> arguments = new List<object>();
            // in_observer_points
            arguments.Add(observersFeatureLayer);
            // in_target_features
            arguments.Add(targetsFeatureLayer);
            // out_line_feature_class
            arguments.Add(outLineFeatureLayer);
            // observer_height_field (Optional)
            arguments.Add(observerOffsetFieldName);
            // target_height_field (Optional)
            arguments.Add(targetOffsetFieldName);
            // join_field (Optional)
            arguments.Add("");
            // sample_distance (Optional)
            arguments.Add("1");
            // output_the_direction (Optional) bool
            arguments.Add("");

            var environments = Geoprocessing.MakeEnvironmentArray(overwriteoutput: true);

            IGPResult result = await Geoprocessing.ExecuteToolAsync("ConstructSightLines_3d", Geoprocessing.MakeValueArray(arguments.ToArray()), environments, flags: GPExecuteToolFlags.Default);

            if (result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }

        /// <summary>
        /// Add surface information to feature class
        /// </summary>
        /// <param name="featureClass"></param>
        /// <param name="surface"></param>
        /// <param name="outProperty"></param>
        /// <returns></returns>
        public static async Task AddSurfaceInformation(string featureClass, string surface, string outProperty)
        {
            //AddSurfaceInformation_3d
            List<object> arguments = new List<object>();
            // in_feature_class
            arguments.Add(featureClass);
            // in_surface
            arguments.Add(surface);
            // out_property
            arguments.Add(outProperty);

            IGPResult result = await Geoprocessing.ExecuteToolAsync("AddSurfaceInformation_3d", Geoprocessing.MakeValueArray(arguments.ToArray()), flags: GPExecuteToolFlags.Default);

            if (result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }

        /// <summary>
        /// Create LOS 
        /// </summary>
        /// <param name="surfaceName"></param>
        /// <param name="lineFeatureClassName"></param>
        /// <param name="outLOSFeatureClass"></param>
        /// <returns></returns>
        public static async Task CreateLOS(string surfaceName, 
                                            string lineFeatureClassName, 
                                            string outLOSFeatureClass)
        {
            List<object> arguments = new List<object>();
            // in_surface
            arguments.Add(surfaceName);
            // in_line_feature_class
            arguments.Add(lineFeatureClassName);
            // out_los_feature_class
            arguments.Add(outLOSFeatureClass);
            // out_obstruction_feature_class (Optional)
            //arguments.Add("");
            //// use_curvature (Optional)
            //arguments.Add("");
            //// use_refraction (Optional)
            //arguments.Add("");
            //// refraction_factor (Optional)
            //arguments.Add("");
            //// pyramid_level_resolution (Optional)
            //arguments.Add("");
            //// in_features (optional) multipatch features
            //arguments.Add("");

            var environments = Geoprocessing.MakeEnvironmentArray(overwriteoutput: true);

            IGPResult result = await Geoprocessing.ExecuteToolAsync("LineOfSight_3d", Geoprocessing.MakeValueArray(arguments.ToArray()), environments, flags: GPExecuteToolFlags.Default);

            if(result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }

        /// <summary>
        /// Method used to delete tables, layers, etc
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task Delete(string name)
        {
            List<object> arguments = new List<object>();
            // in_data
            arguments.Add(name);
            // data_type
            arguments.Add("");

            IGPResult result = await Geoprocessing.ExecuteToolAsync("Delete_management", Geoprocessing.MakeValueArray(arguments.ToArray()));

            if (result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }

        /// <summary>
        /// Method to run Raster to polygon
        /// </summary>
        /// <param name="inputRasterLayer"></param>
        /// <param name="outputPolygonLayer"></param>
        /// <param name="simplify"></param>
        /// <param name="rasterField"></param>
        /// <returns></returns>
        public static async Task IntersectOutput(string inputRasterLayer, string outputPolygonLayer, bool simplify, string rasterField)
        {
            //RasterToPolygon_conversion (in_raster, out_polygon_features, {simplify}, {raster_field})
            List<object> arguments = new List<object>();
            // in_raster
            arguments.Add(inputRasterLayer);
            // out_polygon_features
            arguments.Add(outputPolygonLayer);
            // {simplify}
            arguments.Add(simplify);
            // {raster_field}
            arguments.Add(rasterField);

            var environments = Geoprocessing.MakeEnvironmentArray(overwriteoutput: true);

            IGPResult result = await Geoprocessing.ExecuteToolAsync("RasterToPolygon_conversion", Geoprocessing.MakeValueArray(arguments.ToArray()), environments);

            if (result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }

        /// <summary>
        /// Method to run Visibility
        /// </summary>
        /// <param name="surfaceName">projected surface name</param>
        /// <param name="observerFeatureClassName">observer feature class name</param>
        /// <param name="outRLOSFeatureClass">output feature class</param>
        /// <param name="observerOffset">observer offset in map z units</param>
        /// <param name="surfaceOffset">surface offset in map z units</param>
        /// <param name="minDistance">Minimum distance in map linear units</param>
        /// <param name="maxDistance">Maximum distance in map linear units</param>
        /// <param name="horizontalStartAngle">start angle in degrees, 0 to 360, 0 at north</param>
        /// <param name="horizontalEndAngle">end angle in degrees, 0 to 360, 0 at north</param>
        /// <param name="verticalUpperAngle">upper angle in degrees, 0 to 90</param>
        /// <param name="verticalLowerAngle">lower angle in degrees, -90 to 0</param>
        /// <param name="showNonVisibleData">true or false, if true, renders as Red, if false, renders as transparent</param>
        /// <param name="environments">geoprocessing environments</param>
        /// <param name="addToMap">add to map or not</param>
        /// <returns></returns>
        public static async Task CreateVisibility(string surfaceName, string observerFeatureClassName, string outRLOSFeatureClass, 
                                                    double observerOffset, double surfaceOffset, 
                                                    double minDistance, double maxDistance,
                                                    double horizontalStartAngle, double horizontalEndAngle,
                                                    double verticalUpperAngle, double verticalLowerAngle,
                                                    bool showNonVisibleData,
                                                    System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, string>> environments,
                                                    bool addToMap)
        {
            //Visibility (in_raster, in_observer_features, {out_agl_raster}, {analysis_type}, {nonvisible_cell_value}, {z_factor}, 
            // {curvature_correction}, {refractivity_coefficient}, {surface_offset}, {observer_elevation}, {observer_offset}, {inner_radius}, 
            // {outer_radius}, {horizontal_start_angle}, {horizontal_end_angle}, {vertical_upper_angle}, {vertical_lower_angle})
            List<object> arguments = new List<object>();
            // in_raster
            arguments.Add(surfaceName);
            // in_observer_features
            arguments.Add(observerFeatureClassName);
            // out_rlos_feature_class
            arguments.Add(outRLOSFeatureClass);
            // out_agl_raster
            arguments.Add("");
            // analysis_type
            arguments.Add("FREQUENCY");
            // nonvisible_cell_value
            arguments.Add("ZERO");
            // z_factor
            arguments.Add(1.0);
            // curvature_correction
            arguments.Add("FALSE");
            // refractivity_coefficient
            arguments.Add(""); // default is 0.13
            // surface_offset
            arguments.Add(surfaceOffset); // or field OFFSETB
            // observer_elevation
            arguments.Add(""); // or field SPOT
            // observer_offset
            arguments.Add(observerOffset); // or field OFFSETA
            // inner_radius
            arguments.Add(minDistance);
            // outer_radius
            arguments.Add(maxDistance);
            // horizontal_start_angle
            arguments.Add(horizontalStartAngle);
            // horizontal_end_angle
            arguments.Add(horizontalEndAngle);
            // vertical_upper_angle
            arguments.Add(verticalUpperAngle);
            // vertical_lower_angle
            arguments.Add(verticalLowerAngle);

            IGPResult result = await Geoprocessing.ExecuteToolAsync("Visibility_3d", Geoprocessing.MakeValueArray(arguments.ToArray()), environments,
                null, null, addToMap ? GPExecuteToolFlags.Default : GPExecuteToolFlags.None);

            if (result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }

        /// <summary>
        /// Method used to create a unique value renderer for a feature layer
        /// </summary>
        /// <param name="featureLayer"></param>
        /// <param name="showNonVisData">flag to show non visible data as RED or transparent</param>
        /// <returns></returns>
        public static async Task CreateUniqueValueRenderer(FeatureLayer featureLayer, bool showNonVisData)
        {
            await QueuedTask.Run(() =>
                {
                    // color ramp
                    CIMICCColorSpace colorSpace = new CIMICCColorSpace()
                    {
                        URL = "Default RGB"
                    };

                    CIMContinuousColorRamp continuousColorRamp = new CIMLinearContinuousColorRamp();
                    continuousColorRamp.FromColor = CIMColor.CreateRGBColor(0, 255, 0); // green
                    continuousColorRamp.ToColor = CIMColor.CreateRGBColor(160, 32, 240);// purple
                    continuousColorRamp.ColorSpace = colorSpace;

                    UniqueValueRendererDefinition uvRendererDef = new UniqueValueRendererDefinition()
                    {
                        ColorRamp = continuousColorRamp,
                        UseDefaultSymbol = true,
                        ValueFields = new string[] { "gridcode" },
                        DefaultSymbol = showNonVisData ? new CIMSymbolReference() { Symbol = SymbolFactory.ConstructPolygonSymbol(ColorFactory.Red) } : 
                                                         new CIMSymbolReference() { Symbol = SymbolFactory.ConstructPolygonSymbol(ColorFactory.CreateRGBColor(0,0,0,0)) }
                    };
                    var renderer = featureLayer.CreateRenderer(uvRendererDef);
                    featureLayer.SetRenderer(renderer);

                    featureLayer.SetTransparency(50.0);
                });
        }

        /// <summary>
        /// Method used to create point features from AddInPoints
        /// </summary>
        /// <param name="featureClassName">feature class name to update</param>
        /// <param name="collection">AddInPoints collection</param>
        /// <param name="offset">offset in z units</param>
        /// <returns></returns>
        public static async Task CreatingFeatures(string featureClassName, ObservableCollection<AddInPoint> collection, double offsetInZUnits)
        {
            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName))
                    using (FeatureClassDefinition fcDefinition = enterpriseFeatureClass.GetDefinition())
                    {
                        EditOperation editOperation = new EditOperation();
                        editOperation.Callback(context =>
                        {
                            try
                            {
                                var shapeFieldName = fcDefinition.GetShapeField();

                                foreach (var item in collection)
                                {
                                    using (var rowBuffer = enterpriseFeatureClass.CreateRowBuffer())
                                    {
                                        // Either the field index or the field name can be used in the indexer.
                                        rowBuffer[VisibilityLibrary.Properties.Resources.OffsetFieldName] = offsetInZUnits;
                                        var point = MapPointBuilder.CreateMapPoint(item.Point.X, item.Point.Y, 0.0, item.Point.SpatialReference);
                                        rowBuffer[shapeFieldName] = point;

                                        using (var feature = enterpriseFeatureClass.CreateRow(rowBuffer))
                                        {
                                            //To Indicate that the attribute table has to be updated
                                            context.Invalidate(feature);
                                        }
                                    }
                                }
                            }
                            catch (GeodatabaseException exObj)
                            {
                                message = exObj.Message;
                            }
                        }, enterpriseFeatureClass);

                        creationResult = await editOperation.ExecuteAsync();
                        if (!creationResult)
                            message = editOperation.ErrorMessage;

                        await Project.Current.SaveEditsAsync();
                    }
                });
                if (!creationResult)
                    MessageBox.Show(message);

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        /// <summary>
        /// Method used to update point geometry Z data with offset
        /// also updates the 'Z' field
        /// </summary>
        /// <param name="featureClassName">feature class name</param>
        /// <param name="zFieldName">'Z' field name</param>
        /// <param name="offsetInMapZUnits">double of offset in map z units</param>
        /// <returns></returns>
        public static async Task UpdateShapeWithZ(string featureClassName, string zFieldName, double offsetInMapZUnits)
        {
            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName))
                    using (FeatureClassDefinition fcDefinition = enterpriseFeatureClass.GetDefinition())
                    {
                        int zFieldIndex = fcDefinition.FindField(zFieldName);

                        EditOperation editOperation = new EditOperation();
                        editOperation.Callback(context =>
                        {
                            try
                            {
                                var shapeFieldName = fcDefinition.GetShapeField();

                                using (RowCursor rowCursor = enterpriseFeatureClass.Search(null, false))
                                {
                                    while (rowCursor.MoveNext())
                                    {
                                        using (Feature feature = (Feature)rowCursor.Current)
                                        {
                                            context.Invalidate(feature);
                                            var mp = (MapPoint)feature[shapeFieldName];
                                            var z = (Double)feature[zFieldIndex] + offsetInMapZUnits;
                                            feature[VisibilityLibrary.Properties.Resources.OffsetWithZFieldName] = z;
                                            feature.SetShape(MapPointBuilder.CreateMapPoint(mp.X, mp.Y, z, mp.SpatialReference));

                                            feature.Store();

                                            context.Invalidate(feature);
                                        }
                                    }
                                }
                            }
                            catch (GeodatabaseException exObj)
                            {
                                message = exObj.Message;
                            }
                        }, enterpriseFeatureClass);

                        creationResult = await editOperation.ExecuteAsync();
                        if (!creationResult)
                            message = editOperation.ErrorMessage;

                        await Project.Current.SaveEditsAsync();
                    }
                });
                if (!creationResult)
                    MessageBox.Show(message);

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        /// <summary>
        /// Method used to update the gridcode field
        /// This is a workaround for getting a work unique value renderer
        /// that works with the flag ShowNonVisData
        /// </summary>
        /// <param name="rlosConvertedPolygonsLayer"></param>
        /// <param name="add"></param>
        /// <returns></returns>
        internal static async Task UpdateFieldWithValue(string rlosConvertedPolygonsLayer, bool add)
        {
            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(rlosConvertedPolygonsLayer))
                    using (FeatureClassDefinition fcDefinition = enterpriseFeatureClass.GetDefinition())
                    {
                        int gridcodeFieldIndex = fcDefinition.FindField("gridcode");

                        EditOperation editOperation = new EditOperation();
                        editOperation.Callback(context =>
                        {
                            try
                            {
                                using (RowCursor rowCursor = enterpriseFeatureClass.Search(null, false))
                                {
                                    while (rowCursor.MoveNext())
                                    {
                                        using (Feature feature = (Feature)rowCursor.Current)
                                        {
                                            context.Invalidate(feature);
                                            var gridcode = (int)feature[gridcodeFieldIndex];
                                            feature[gridcodeFieldIndex] = add ? ++gridcode : --gridcode;

                                            feature.Store();

                                            context.Invalidate(feature);
                                        }
                                    }
                                }
                            }
                            catch (GeodatabaseException exObj)
                            {
                                message = exObj.Message;
                            }
                        }, enterpriseFeatureClass);

                        creationResult = await editOperation.ExecuteAsync();
                        if (!creationResult)
                            message = editOperation.ErrorMessage;

                        await Project.Current.SaveEditsAsync();
                    }
                });
                if (!creationResult)
                    MessageBox.Show(message);

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        internal static async Task<List<int>> GetSourceOIDs()
        {
            var sourceOIDs = new List<int>();
            try
            {
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(VisibilityLibrary.Properties.Resources.LOSOutputLayerName))
                    {
                        var filter = new QueryFilter();
                        filter.WhereClause = "TarIsVis = 1 AND VisCode = 1";

                        var cursor = enterpriseFeatureClass.Search(filter, true);

                        while (cursor.MoveNext())
                        {
                            var sourceOID = (int)cursor.Current["SourceOID"];
                            if (!sourceOIDs.Contains(sourceOID))
                                sourceOIDs.Add(sourceOID);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // do nothing
            }

            return sourceOIDs;
        }

        internal static async Task<VisibilityStats> GetVisibilityStats(List<int> sourceOIDs)
        {
            var visibilityStats = new VisibilityStats();

            try
            {
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(VisibilityLibrary.Properties.Resources.SightLinesLayerName))
                    {
                        var filter = new QueryFilter();
                        filter.WhereClause = string.Format("OID IN ({0})", string.Join(",", sourceOIDs));

                        var cursor = enterpriseFeatureClass.Search(filter, true);

                        while (cursor.MoveNext())
                        {
                            var observerOID = (int)cursor.Current["OID_OBSERV"];
                            var targetOID = (int)cursor.Current["OID_TARGET"];

                            if (!visibilityStats.ObserverOIDs.Contains(observerOID))
                                visibilityStats.ObserverOIDs.Add(observerOID);

                            if(visibilityStats.TargetOIDVisCounts.ContainsKey(targetOID))
                            {
                                visibilityStats.TargetOIDVisCounts[targetOID]++;
                            }
                            else
                            {
                                visibilityStats.TargetOIDVisCounts.Add(targetOID, 1);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // do nothing
            }

            return visibilityStats;
        }

        internal static async Task UpdateLayersWithVisibilityStats(VisibilityStats visStats)
        {
            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    {
                        // do the observers layer
                        using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(VisibilityLibrary.Properties.Resources.ObserversLayerName))
                        using (FeatureClassDefinition fcDefinition = enterpriseFeatureClass.GetDefinition())
                        {
                            int tarIsVisFieldIndex = fcDefinition.FindField(VisibilityLibrary.Properties.Resources.TarIsVisFieldName);
                            int oidFieldIndex = fcDefinition.FindField(fcDefinition.GetObjectIDField());

                            EditOperation editOperation = new EditOperation();
                            editOperation.Callback(context =>
                            {
                                try
                                {
                                    using (RowCursor rowCursor = enterpriseFeatureClass.Search(null, false))
                                    {
                                        while (rowCursor.MoveNext())
                                        {
                                            using (Feature feature = (Feature)rowCursor.Current)
                                            {
                                                context.Invalidate(feature);

                                                var oid = (int)feature[oidFieldIndex];
                                                if (visStats.ObserverOIDs.Contains(oid))
                                                    feature[tarIsVisFieldIndex] = 1;
                                                else
                                                    feature[tarIsVisFieldIndex] = 0;

                                                feature.Store();

                                                context.Invalidate(feature);
                                            }
                                        }
                                    }
                                }
                                catch (GeodatabaseException exObj)
                                {
                                    message = exObj.Message;
                                }
                            }, enterpriseFeatureClass);

                            creationResult = await editOperation.ExecuteAsync();
                            if (!creationResult)
                                message = editOperation.ErrorMessage;

                            await Project.Current.SaveEditsAsync();
                        }
                        // do the targets layer
                        using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(VisibilityLibrary.Properties.Resources.TargetsLayerName))
                        using (FeatureClassDefinition fcDefinition = enterpriseFeatureClass.GetDefinition())
                        {
                            int numOfObserversFieldIndex = fcDefinition.FindField(VisibilityLibrary.Properties.Resources.NumOfObserversFieldName);
                            int oidFieldIndex = fcDefinition.FindField(fcDefinition.GetObjectIDField());

                            EditOperation editOperation = new EditOperation();
                            editOperation.Callback(context =>
                            {
                                try
                                {
                                    using (RowCursor rowCursor = enterpriseFeatureClass.Search(null, false))
                                    {
                                        while (rowCursor.MoveNext())
                                        {
                                            using (Feature feature = (Feature)rowCursor.Current)
                                            {
                                                context.Invalidate(feature);

                                                var oid = (int)feature[oidFieldIndex];
                                                if (visStats.TargetOIDVisCounts.ContainsKey(oid))
                                                    feature[numOfObserversFieldIndex] = visStats.TargetOIDVisCounts[oid];
                                                else
                                                    feature[numOfObserversFieldIndex] = 0;

                                                feature.Store();

                                                context.Invalidate(feature);
                                            }
                                        }
                                    }
                                }
                                catch (GeodatabaseException exObj)
                                {
                                    message = exObj.Message;
                                }
                            }, enterpriseFeatureClass);

                            creationResult = await editOperation.ExecuteAsync();
                            if (!creationResult)
                                message = editOperation.ErrorMessage;

                            await Project.Current.SaveEditsAsync();
                        }

                    }
                });
                if (!creationResult)
                    MessageBox.Show(message);

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }
    }

    public class VisibilityStats
    {
        public VisibilityStats()
        { }

        private List<int> _ObserverOIDs = new List<int>();
        public List<int> ObserverOIDs
        {
            get { return _ObserverOIDs; }
        }

        private Dictionary<int, int> _TargetOIDVisCounts = new Dictionary<int, int>();
        public Dictionary<int, int> TargetOIDVisCounts
        {
            get { return _TargetOIDVisCounts; }
        }
    }

}
