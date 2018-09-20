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
        public static async Task<bool> CreateLayer(string featureDatasetName, string featureclassName, string featureclassType, bool zEnabled, bool addToMap)
        {
            List<object> arguments = new List<object>();
            // store the results in the default geodatabase
            arguments.Add(CoreModule.CurrentProject.DefaultGeodatabasePath + System.IO.Path.DirectorySeparatorChar + featureDatasetName);
            // name of the feature class
            arguments.Add(featureclassName);
            // type of geometry
            arguments.Add(featureclassType);
            // no template
            arguments.Add("");
            // m values
            arguments.Add("DISABLED");
            // z values
            if (zEnabled)
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

            return isResultGoodAndReportMessages(result, "CreateFeatureclass_management", arguments);
        }

        public static async Task<bool> CreateFeatureDataset(string featureclassName)
        {
            List<object> arguments = new List<object>();
            // store the results in the default geodatabase
            arguments.Add(CoreModule.CurrentProject.DefaultGeodatabasePath);
            // name of the feature class
            arguments.Add(featureclassName);


            arguments.Add(MapView.Active.Map.SpatialReference);

            var environments = Geoprocessing.MakeEnvironmentArray();

            IGPResult result = await Geoprocessing.ExecuteToolAsync("CreateFeatureDataset_management",
                Geoprocessing.MakeValueArray(arguments.ToArray()),
                environments,
                null,
                null,
                GPExecuteToolFlags.None);

            return isResultGoodAndReportMessages(result, "CreateFeatureDataset_management", arguments);
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
        public static async Task JoinField(string inData, string inField, string joinTable, string joinField, string[] fields)
        {
            List<object> arguments = new List<object>();
            // in_data
            arguments.Add(inData);
            // in_field
            arguments.Add(inField);
            // join_table
            arguments.Add(joinTable);
            // join_field
            arguments.Add(joinField);
            // fields
            arguments.Add(fields);

            IGPResult result = await Geoprocessing.ExecuteToolAsync("JoinField_management", Geoprocessing.MakeValueArray(arguments.ToArray()));
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
        public static async Task<bool> CreateSightLines(string observersFeatureLayer,
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

            bool success = isResultGoodAndReportMessages(result, "ConstructSightLines_3d", arguments);
            if (!success)
            {
                // If the tool failed, try to remove the table that was created so execution will run next time
                List<object> args = new List<object>();
                args.Add(outLineFeatureLayer);
                IGPResult result2 = await Geoprocessing.ExecuteToolAsync("Delete_management", Geoprocessing.MakeValueArray(args.ToArray()), environments, flags: GPExecuteToolFlags.Default);
            }

            return success;
        }

        /// <summary>
        /// Add surface information to feature class
        /// </summary>
        /// <param name="featureClass"></param>
        /// <param name="surface"></param>
        /// <param name="outProperty"></param>
        /// <returns></returns>
        public static async Task<bool> AddSurfaceInformation(string featureClass, string surface, string outProperty)
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

            return isResultGoodAndReportMessages(result, "AddSurfaceInformation_3d", arguments);
        }

        /// <summary>
        /// Create LOS 
        /// </summary>
        /// <param name="surfaceName"></param>
        /// <param name="lineFeatureClassName"></param>
        /// <param name="outLOSFeatureClass"></param>
        /// <returns></returns>
        public static async Task<bool> CreateLOS(string surfaceName,
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

            bool success = isResultGoodAndReportMessages(result, "LineOfSight_3d", arguments);
            if (!success)
            {
                // If the tool failed, try to remove the table that was created so execution will run next time
                List<object> args = new List<object>();
                args.Add(outLOSFeatureClass);
                IGPResult result2 = await Geoprocessing.ExecuteToolAsync("Delete_management", Geoprocessing.MakeValueArray(args.ToArray()), environments, flags: GPExecuteToolFlags.Default);
            }

            return success;
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
        /// Method to run raster to polygon and optionally intersect with layer
        /// </summary>
        /// <param name="inputRasterLayer">input raster layer to convert to polygons</param>
        /// <param name="outputPolygonLayer">output layer converted to polygons</param>
        /// <param name="simplify">simplify(smooth) output polygons</param>
        /// <param name="rasterField">field from raster </param>
        /// <param name="intersectMaskFeatureClass">(Optional)If present, the layer to intersect with the output polygon layer</param>
        /// <returns>success</returns>
        public static async Task<bool> IntersectOutput(string inputRasterLayer, string outputPolygonLayer,
            bool simplify, string rasterField, string intersectMaskFeatureClass = "")
        {
            if (string.IsNullOrEmpty(inputRasterLayer) || string.IsNullOrEmpty(outputPolygonLayer))
                return false;

            string rasterToPolyLayer = outputPolygonLayer;

            bool addToMap = true;

            if (!string.IsNullOrEmpty(intersectMaskFeatureClass))
            {
                // If intersect layer present, create a temporary polygon layer & don't add to map 
                rasterToPolyLayer += "_rasterToPoly";
                addToMap = false;
            }

            //RasterToPolygon_conversion (in_raster, out_polygon_features, {simplify}, {raster_field})
            List<object> arguments = new List<object>();
            // in_raster
            arguments.Add(inputRasterLayer);
            // out_polygon_features
            arguments.Add(rasterToPolyLayer);
            // {simplify}
            arguments.Add(simplify);
            // {raster_field}
            arguments.Add(rasterField);

            var environments = Geoprocessing.MakeEnvironmentArray(overwriteoutput: true);

            IGPResult result = await Geoprocessing.ExecuteToolAsync("RasterToPolygon_conversion",
                Geoprocessing.MakeValueArray(arguments.ToArray()), environments,
                null, null, addToMap ? GPExecuteToolFlags.Default : GPExecuteToolFlags.None);

            if (!isResultGoodAndReportMessages(result, "RasterToPolygon_conversion", arguments))
                return false;

            // If intersect layer *not* present, we are done, return
            if (string.IsNullOrEmpty(intersectMaskFeatureClass))
                return true;

            // Intersect_analysis('infeatures';'in_features', 'out_feature_class', 'NO_FID')
            List<object> argumentsIntersect = new List<object>();
            // in_features
            string intersectLayers = rasterToPolyLayer + ';' + intersectMaskFeatureClass;
            argumentsIntersect.Add(intersectLayers);
            // out_polygon_features
            argumentsIntersect.Add(outputPolygonLayer);
            // Don't include FIDs in join (or they will both appear in output)
            argumentsIntersect.Add("NO_FID");

            // if non-empty, intersect with intersectMaskFeatureClass
            result = await Geoprocessing.ExecuteToolAsync("Intersect_analysis",
                Geoprocessing.MakeValueArray(argumentsIntersect.ToArray()), environments);

            return isResultGoodAndReportMessages(result, "Intersect_analysis", arguments);
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
        public static async Task<bool> CreateVisibility(string surfaceName, string observerFeatureClassName, string outRLOSFeatureClass,
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

            return isResultGoodAndReportMessages(result, "Visibility_3d", arguments);
        }

        /// <summary>
        /// Method used to create a unique value renderer for a feature layer
        /// </summary>
        /// <param name="featureLayer"></param>
        /// <param name="showNonVisData">flag to show non visible data as RED or transparent</param>
        /// <returns></returns>
        public static async Task CreateUniqueValueRenderer(FeatureLayer featureLayer, bool showNonVisData, string outputLayerName, bool showClassicViewshed)
        {
            if (featureLayer == null)
                return;

            await QueuedTask.Run(() =>
            {
                var gridcodeUniqueList = new List<int>();

                using (Geodatabase geodatabase = new Geodatabase(FgdbFileToConnectionPath(CoreModule.CurrentProject.DefaultGeodatabasePath)))
                using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(outputLayerName))
                {
                    var filter = new QueryFilter();
                    filter.WhereClause = "1=1";
                    filter.SubFields = "gridcode";

                    var cursor = enterpriseFeatureClass.Search(filter, true);

                    while (cursor.MoveNext())
                    {
                        var gc = (int)cursor.Current["gridcode"];

                        if (!gridcodeUniqueList.Contains(gc))
                            gridcodeUniqueList.Add(gc);
                    }
                }

                gridcodeUniqueList.Sort();

                if (gridcodeUniqueList.Contains(0))
                    gridcodeUniqueList.Remove(0);

                // create classes for each unique 'gridcode' value

                int gcCount = gridcodeUniqueList.Count;

                var colors = GetGradients(gcCount).GetEnumerator();

                //Create the Unique Value Renderer
                CIMUniqueValueRenderer uniqueValueRenderer = new CIMUniqueValueRenderer();

                // set the value field
                uniqueValueRenderer.Fields = new string[] { "gridcode" };

                List<CIMUniqueValueClass> classes = new List<CIMUniqueValueClass>();

                List<CIMUniqueValue> visibleValues = new List<CIMUniqueValue>();
                int cnt = 1;
                foreach (var gc in gridcodeUniqueList)
                {
                    if (showClassicViewshed)
                    {
                        CIMUniqueValue visibleValue = new CIMUniqueValue();
                        visibleValue.FieldValues = new string[] { gc.ToString() };
                        visibleValues.Add(visibleValue);
                    }
                    else
                    {
                        colors.MoveNext();

                    List<CIMUniqueValue> visValues = new List<CIMUniqueValue>();
                    CIMUniqueValue visValue = new CIMUniqueValue();
                    visValue.FieldValues = new string[] { gc.ToString() };
                    visValues.Add(visValue);

                    var visSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(CIMColor.CreateRGBColor(colors.Current.R, colors.Current.G, colors.Current.B));
                    string observerString = cnt == 1 ? " Observer" : " Observers";
                    string label = "Visible by " + cnt.ToString() + observerString;
                    var visClass = new CIMUniqueValueClass()
                    {
                        Values = visValues.ToArray(),
                        Label = label,
                        Visible = true,
                        Editable = true,
                        Symbol = new CIMSymbolReference() { Symbol = visSymbol }
                    };

                        classes.Add(visClass);
                        cnt++;
                    }
                }

                if (showClassicViewshed)
                {
                    var visibleSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(ColorFactory.Instance.GreenRGB);
                    string visibleLabel = "Visible";
                    var visibleClass = new CIMUniqueValueClass()
                    {
                        Values = visibleValues.ToArray(),
                        Label = visibleLabel,
                        Visible = true,
                        Editable = true,
                        Symbol = new CIMSymbolReference() { Symbol = visibleSymbol }
                    };

                    classes.Add(visibleClass);
                }
               
                CIMUniqueValueGroup groupOne = new CIMUniqueValueGroup();
                groupOne.Heading = "";
                groupOne.Classes = classes.ToArray();

                uniqueValueRenderer.Groups = new CIMUniqueValueGroup[] { groupOne };

                //Draw the rest with the default symbol
                uniqueValueRenderer.UseDefaultSymbol = true;
                uniqueValueRenderer.DefaultLabel = "Not Visible";

                uniqueValueRenderer.DefaultSymbol = showNonVisData ? new CIMSymbolReference() { Symbol = SymbolFactory.Instance.ConstructPolygonSymbol(ColorFactory.Instance.RedRGB) } :
                                                                     new CIMSymbolReference() { Symbol = SymbolFactory.Instance.ConstructPolygonSymbol(ColorFactory.Instance.CreateRGBColor(0, 0, 0, 0)) };

                featureLayer.SetRenderer(uniqueValueRenderer);

                featureLayer.SetTransparency(50.0);
            });
        }

        public static IEnumerable<System.Windows.Media.Color> GetGradients(int steps)
        {
            Random randonGen = new Random();
            for (int i = 0; i < steps; i++)
            {
                System.Windows.Media.Color randomColor =
                    System.Windows.Media.Color.FromArgb(
                    (byte)randonGen.Next(255),
                    (byte)randonGen.Next(255),
                    (byte)randonGen.Next(255),
                    (byte)randonGen.Next(255));
                yield return randomColor;
            }

        }
        /// <summary>
        /// Method used to create point features from AddInPoints
        /// </summary>
        /// <param name="featureClassName">feature class name to update</param>
        /// <param name="collection">AddInPoints collection</param>
        /// <param name="offset">offset in z units</param>
        /// <returns></returns>
        public static async Task CreatingFeatures(string featureClassName, ObservableCollection<AddInPoint> collection, double offsetInZUnits, string rendererConfigFieldName = "")
        {
            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(FgdbFileToConnectionPath(CoreModule.CurrentProject.DefaultGeodatabasePath)))
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

                                        if (rendererConfigFieldName != "")
                                        {
                                            rowBuffer[rendererConfigFieldName] = -1;
                                        }
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
                    using (Geodatabase geodatabase = new Geodatabase(FgdbFileToConnectionPath(CoreModule.CurrentProject.DefaultGeodatabasePath)))
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


        internal static async Task<List<int>> GetSourceOIDs(string layerName)
        {
            var sourceOIDs = new List<int>();
            try
            {
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(FgdbFileToConnectionPath(CoreModule.CurrentProject.DefaultGeodatabasePath)))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(layerName))
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
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return sourceOIDs;
        }

        internal static async Task<VisibilityStats> GetVisibilityStats(List<int> sourceOIDs, string layerName)
        {
            var visibilityStats = new VisibilityStats();

            try
            {
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(FgdbFileToConnectionPath(CoreModule.CurrentProject.DefaultGeodatabasePath)))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(layerName))
                    {
                        var filter = new QueryFilter();
                        // if (sourceOIDs.Count > 0)
                        filter.WhereClause = string.Format("OID IN ({0})", string.Join(",", sourceOIDs));
                        // else
                        //     filter.WhereClause = "";

                        var cursor = enterpriseFeatureClass.Search(filter, true);

                        while (cursor.MoveNext())
                        {
                            var observerOID = (int)cursor.Current["OID_OBSERV"];
                            var targetOID = (int)cursor.Current["OID_TARGET"];

                            if (!visibilityStats.ObserverOIDs.Contains(observerOID))
                                visibilityStats.ObserverOIDs.Add(observerOID);

                            if (visibilityStats.TargetOIDVisCounts.ContainsKey(targetOID))
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
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return visibilityStats;
        }

        internal static async Task UpdateLayersWithVisibilityStats(VisibilityStats visStats, string observersLayerName, string targetsLayerName)
        {
            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(FgdbFileToConnectionPath(CoreModule.CurrentProject.DefaultGeodatabasePath)))
                    {
                        // do the observers layer
                        using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(observersLayerName))
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
                        using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(targetsLayerName))
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

        public static async Task CreateObserversRenderer(FeatureLayer featureLayer)
        {
            await QueuedTask.Run(() =>
            {
                //Create the Unique Value Renderer
                CIMUniqueValueRenderer uniqueValueRenderer = new CIMUniqueValueRenderer();

                // set the value field
                uniqueValueRenderer.Fields = new string[] { VisibilityLibrary.Properties.Resources.TarIsVisFieldName };

                List<CIMUniqueValueClass> classes = new List<CIMUniqueValueClass>();

                List<CIMUniqueValue> noVisValues = new List<CIMUniqueValue>();
                CIMUniqueValue noVisValue = new CIMUniqueValue();
                noVisValue.FieldValues = new string[] { "0" };
                noVisValues.Add(noVisValue);

                var noVisSymbol = SymbolFactory.Instance.ConstructPointSymbol();
                var s1 = SymbolFactory.Instance.ConstructMarker(CIMColor.CreateRGBColor(255, 0, 0), 5, SimpleMarkerStyle.Circle);
                var s2 = SymbolFactory.Instance.ConstructMarker(CIMColor.CreateRGBColor(0, 0, 255), 12, SimpleMarkerStyle.Circle);

                noVisSymbol.SymbolLayers = new CIMSymbolLayer[2] { s1, s2 };

                var noVis = new CIMUniqueValueClass()
                {
                    Values = noVisValues.ToArray(),
                    Label = "No Visible Targets",
                    Visible = true,
                    Editable = true,
                    Symbol = new CIMSymbolReference() { Symbol = noVisSymbol }
                };

                classes.Add(noVis);

                // visTar
                List<CIMUniqueValue> visTarValues = new List<CIMUniqueValue>();
                CIMUniqueValue visTarValue = new CIMUniqueValue();
                visTarValue.FieldValues = new string[] { "1" };
                visTarValues.Add(visTarValue);

                var visSymbol = SymbolFactory.Instance.ConstructPointSymbol();
                var vis1 = SymbolFactory.Instance.ConstructMarker(CIMColor.CreateRGBColor(0, 255, 0), 5, SimpleMarkerStyle.Circle);
                var vis2 = SymbolFactory.Instance.ConstructMarker(CIMColor.CreateRGBColor(0, 0, 255), 12, SimpleMarkerStyle.Circle);
                
                visSymbol.SymbolLayers = new CIMSymbolLayer[2] { vis1, vis2 };

                var visTar = new CIMUniqueValueClass()
                {
                    Values = visTarValues.ToArray(),
                    Label = "Has Visible Targets",
                    Visible = true,
                    Editable = true,
                    Symbol = new CIMSymbolReference() { Symbol = visSymbol }
                };

                classes.Add(visTar);

                // out of extent
                List<CIMUniqueValue> outOfExtentValues = new List<CIMUniqueValue>();
                CIMUniqueValue outOfExtentValue = new CIMUniqueValue();
                outOfExtentValue.FieldValues = new string[] { "-1" };
                outOfExtentValues.Add(outOfExtentValue);

                var outExtentSymbol = SymbolFactory.Instance.ConstructPointSymbol();
                var symbol = SymbolFactory.Instance.ConstructMarker(CIMColor.CreateRGBColor(0, 0, 255), 12, SimpleMarkerStyle.X);
                outExtentSymbol.SymbolLayers = new CIMSymbolLayer[1] { symbol };

                var outOfExtent = new CIMUniqueValueClass()
                {
                    Values = outOfExtentValues.ToArray(),
                    Label = "Out Of Extent",
                    Visible = true,
                    Editable = true,
                    Symbol = new CIMSymbolReference() { Symbol = outExtentSymbol }
                };
                classes.Add(outOfExtent);

                CIMUniqueValueGroup groupOne = new CIMUniqueValueGroup();
                groupOne.Heading = "Observers";
                groupOne.Classes = classes.ToArray();

                uniqueValueRenderer.Groups = new CIMUniqueValueGroup[] { groupOne };

                //Draw the rest with the default symbol
                uniqueValueRenderer.UseDefaultSymbol = true;
                uniqueValueRenderer.DefaultLabel = "All other values";

                var defaultColor = CIMColor.CreateRGBColor(215, 215, 215);
                uniqueValueRenderer.DefaultSymbol = new CIMSymbolReference()
                {
                    Symbol = SymbolFactory.Instance.ConstructPointSymbol(defaultColor)
                };

                //var renderer = featureLayer.CreateRenderer(uniqueValueRenderer);
                featureLayer.SetRenderer(uniqueValueRenderer);
            });
        }

        public static async Task CreateTargetsRenderer(FeatureLayer featureLayer)
        {
            await QueuedTask.Run(() =>
            {
                //Create the Unique Value Renderer
                CIMUniqueValueRenderer uniqueValueRenderer = new CIMUniqueValueRenderer();

                // set the value field
                uniqueValueRenderer.Fields = new string[] { VisibilityLibrary.Properties.Resources.NumOfObserversFieldName };

                List<CIMUniqueValueClass> classes = new List<CIMUniqueValueClass>();

                List<CIMUniqueValue> noVisValues = new List<CIMUniqueValue>();
                CIMUniqueValue noVisValue = new CIMUniqueValue();
                noVisValue.FieldValues = new string[] { "0" };
                noVisValues.Add(noVisValue);

                var noVisSymbol = SymbolFactory.Instance.ConstructPointSymbol(CIMColor.CreateRGBColor(255, 0, 0), 14, SimpleMarkerStyle.Circle);

                var noVis = new CIMUniqueValueClass()
                {
                    Values = noVisValues.ToArray(),
                    Label = "Not visible",
                    Visible = true,
                    Editable = true,
                    Symbol = new CIMSymbolReference() { Symbol = noVisSymbol }
                };

                classes.Add(noVis);

                // out of extent
                List<CIMUniqueValue> outOfExtentValues = new List<CIMUniqueValue>();
                CIMUniqueValue outOfExtentValue = new CIMUniqueValue();
                outOfExtentValue.FieldValues = new string[] { "-1" };
                outOfExtentValues.Add(outOfExtentValue);

                var outExtentSymbol = SymbolFactory.Instance.ConstructPointSymbol();
                var symbol = SymbolFactory.Instance.ConstructMarker(CIMColor.CreateRGBColor(255, 0, 0), 12, SimpleMarkerStyle.X);
                outExtentSymbol.SymbolLayers = new CIMSymbolLayer[1] { symbol };

                var outOfExtent = new CIMUniqueValueClass()
                {
                    Values = outOfExtentValues.ToArray(),
                    Label = "Out Of Extent",
                    Visible = true,
                    Editable = true,
                    Symbol = new CIMSymbolReference() { Symbol = outExtentSymbol }
                };
                classes.Add(outOfExtent);

                CIMUniqueValueGroup groupOne = new CIMUniqueValueGroup();
                groupOne.Heading = "Targets";
                groupOne.Classes = classes.ToArray();

                uniqueValueRenderer.Groups = new CIMUniqueValueGroup[] { groupOne };

                //Draw the rest with the default symbol
                uniqueValueRenderer.UseDefaultSymbol = true;
                uniqueValueRenderer.DefaultLabel = "Visible";

                uniqueValueRenderer.DefaultSymbol = new CIMSymbolReference()
                {
                    Symbol = SymbolFactory.Instance.ConstructPointSymbol(CIMColor.CreateRGBColor(0, 255, 0), 14, SimpleMarkerStyle.Circle)
                };

                //var renderer = featureLayer.CreateRenderer(uniqueValueRenderer);
                featureLayer.SetRenderer(uniqueValueRenderer);

                //featureLayer.SetTransparency(50.0);
            });
        }

        internal async static Task CreateRLOSObserversRenderer(FeatureLayer featureLayer)
        {
            await QueuedTask.Run(() =>
            {
                //Create the Unique Value Renderer
                CIMUniqueValueRenderer uniqueValueRenderer = new CIMUniqueValueRenderer();

                // set the value field
                uniqueValueRenderer.Fields = new string[] { VisibilityLibrary.Properties.Resources.IsOutOfExtentFieldName };

                List<CIMUniqueValueClass> classes = new List<CIMUniqueValueClass>();
                // out of extent
                List<CIMUniqueValue> outOfExtentValues = new List<CIMUniqueValue>();
                CIMUniqueValue outOfExtentValue = new CIMUniqueValue();
                outOfExtentValue.FieldValues = new string[] { "-1" };
                outOfExtentValues.Add(outOfExtentValue);

                var outExtentSymbol = SymbolFactory.Instance.ConstructPointSymbol();
                var symbol = SymbolFactory.Instance.ConstructMarker(CIMColor.CreateRGBColor(0, 0, 255), 12, SimpleMarkerStyle.X);
                outExtentSymbol.SymbolLayers = new CIMSymbolLayer[1] { symbol };

                var outOfExtent = new CIMUniqueValueClass()
                {
                    Values = outOfExtentValues.ToArray(),
                    Label = "Out Of Extent",
                    Visible = true,
                    Editable = true,
                    Symbol = new CIMSymbolReference() { Symbol = outExtentSymbol }
                };
                classes.Add(outOfExtent);

                CIMUniqueValueGroup groupOne = new CIMUniqueValueGroup();
                groupOne.Heading = "Out Of Extent";
                groupOne.Classes = classes.ToArray();

                uniqueValueRenderer.Groups = new CIMUniqueValueGroup[] { groupOne };

                //Draw the rest with the default symbol
                uniqueValueRenderer.UseDefaultSymbol = true;
                uniqueValueRenderer.DefaultLabel = "In side Extent";

                uniqueValueRenderer.DefaultSymbol = new CIMSymbolReference()
                {
                    Symbol = SymbolFactory.Instance.ConstructPointSymbol(CIMColor.CreateRGBColor(0, 255, 0), 12, SimpleMarkerStyle.Circle)
                };

                //var renderer = featureLayer.CreateRenderer(uniqueValueRenderer);
                featureLayer.SetRenderer(uniqueValueRenderer);
            });
        }

        public static async Task CreateTargetLayerLabels(FeatureLayer featureLayer)
        {
            await QueuedTask.Run(() =>
            {
                var lc = featureLayer.LabelClasses[0];
                //lc.SetExpression(string.Format("[{0}]", VisibilityLibrary.Properties.Resources.NumOfObserversFieldName));
                string expression = @"Function FindLabel ( [NumOfObservers] )
                                    If (CInt([NumOfObservers])>0) Then
                                        FindLabel = ""<FNT size='8'>"" + [NumOfObservers] + ""</FNT>""
                                    else
                                        FindLabel = """"
                                    End If
                                    End Function";
                lc.SetExpression(expression);
                lc.SetExpressionEngine(LabelExpressionEngine.VBScript);
                if (MapView.Active.Map.GetLabelEngine() == LabelEngine.Standard)
                {
                    lc.SetStandardLabelPlacementProperties(new CIMStandardLabelPlacementProperties()
                    {
                        PointPlacementMethod = StandardPointPlacementMethod.OnTopPoint,
                        FeatureType = LabelFeatureType.Point
                    });
                }
                else
                {
                    lc.SetMaplexLabelPlacementProperties(new CIMMaplexLabelPlacementProperties()
                    {
                        PointPlacementMethod = MaplexPointPlacementMethod.CenteredOnPoint,
                        FeatureType = LabelFeatureType.Point
                    });
                }

                featureLayer.SetLabelVisibility(true);
            });
        }

        public static async Task CreateGroupLayer(List<Layer> layerList, string groupLayerName)
        {
            await QueuedTask.Run(() =>
            {
                GroupLayer groupLayer = LayerFactory.Instance.CreateGroupLayer(MapView.Active.Map, 0, groupLayerName);

                var grpLayerDef = groupLayer.GetDefinition() as CIMGroupLayer;
                if (grpLayerDef == null)
                    return;

                List<string> layerIds = new List<string>();

                foreach (Layer layer in layerList)
                {
                    var layerToAddDef = layer.GetDefinition();
                    layerIds.Add(layerToAddDef.URI);
                }

                grpLayerDef.Layers = layerIds.ToArray();
                groupLayer.SetDefinition(grpLayerDef);

                CIMMap mapDef = MapView.Active.Map.GetDefinition();
                List<string> maplayerIds = new List<string>();

                foreach (Layer layer in MapView.Active.Map.Layers)
                {
                    if (!layerIds.Contains(layer.GetDefinition().URI))
                    {
                        maplayerIds.Add(layer.GetDefinition().URI);
                    }
                }

                mapDef.Layers = maplayerIds.ToArray();
                MapView.Active.Map.SetDefinition(mapDef);
            });
        }

        public static async Task MoveLayersToGroupLayer(List<Layer> layerList, string groupLayerName)
        {
            await QueuedTask.Run(() =>
            {
                GroupLayer groupLayer = LayerFactory.Instance.CreateGroupLayer(MapView.Active.Map,
                                        0,  // add to the top ?
                                        groupLayerName);
                for (int i = layerList.Count - 1; i >= 0; i--)
                {
                    groupLayer.MoveLayer(layerList[i], 0);
                }
            });
            await ArcGIS.Desktop.Framework.FrameworkApplication.Current.Dispatcher.Invoke(async () =>
            {
                await ArcGIS.Desktop.Core.Project.Current.SaveAsync();
            });
        }

        public static async Task CreateSightLinesRenderer(FeatureLayer featureLayer)
        {
            await QueuedTask.Run(() =>
            {
                //Create the Unique Value Renderer
                CIMUniqueValueRenderer uniqueValueRenderer = new CIMUniqueValueRenderer();

                // set the value field
                uniqueValueRenderer.Fields = new string[] { VisibilityLibrary.Properties.Resources.TarIsVisFieldName };

                List<CIMUniqueValueClass> classes = new List<CIMUniqueValueClass>();

                List<CIMUniqueValue> noVisValues = new List<CIMUniqueValue>();
                CIMUniqueValue noVisValue = new CIMUniqueValue();
                noVisValue.FieldValues = new string[] { "0" };
                noVisValues.Add(noVisValue);

                var ss = new CIMSolidStroke();
                ss.Color = ColorFactory.Instance.BlackRGB;
                ss.Width = 6.0;
                ss.Enable = true;
                ss.LineStyle3D = Simple3DLineStyle.Tube;
                //var noVisSymbol = SymbolFactory.ConstructLineSymbol(CIMColor.CreateRGBColor(0, 0, 0), 6.0, SimpleLineStyle.Solid);
                var noVisSymbol = SymbolFactory.Instance.ConstructLineSymbol(ss);

                var noVis = new CIMUniqueValueClass()
                {
                    Values = noVisValues.ToArray(),
                    Label = "Not Visible",
                    Visible = true,
                    Editable = true,
                    Symbol = new CIMSymbolReference() { Symbol = noVisSymbol }
                };

                classes.Add(noVis);

                List<CIMUniqueValue> hasVisValues = new List<CIMUniqueValue>();
                CIMUniqueValue hasVisValue = new CIMUniqueValue();
                hasVisValue.FieldValues = new string[] { "1" };
                hasVisValues.Add(hasVisValue);

                //var hasVisSymbol = SymbolFactory.ConstructLineSymbol(CIMColor.CreateRGBColor(255, 255, 255), 6, SimpleLineStyle.Solid);
                var ss2 = new CIMSolidStroke();
                ss2.Color = ColorFactory.Instance.WhiteRGB;
                ss2.Width = 6.0;
                ss2.Enable = true;
                ss2.LineStyle3D = Simple3DLineStyle.Tube;
                var hasVisSymbol = SymbolFactory.Instance.ConstructLineSymbol(ss2);

                var hasVis = new CIMUniqueValueClass()
                {
                    Values = hasVisValues.ToArray(),
                    Label = "Visible",
                    Visible = true,
                    Editable = true,
                    Symbol = new CIMSymbolReference() { Symbol = hasVisSymbol }
                };

                classes.Add(hasVis);

                CIMUniqueValueGroup groupOne = new CIMUniqueValueGroup();
                groupOne.Heading = "Sight Lines";
                groupOne.Classes = classes.ToArray();

                uniqueValueRenderer.Groups = new CIMUniqueValueGroup[] { groupOne };

                //Draw the rest with the default symbol
                uniqueValueRenderer.UseDefaultSymbol = false;
                featureLayer.SetRenderer(uniqueValueRenderer);
            });
        }

        public static async Task CreateVisCodeRenderer(FeatureLayer featureLayer, string visField,
                                                        int visCodeVisible, int visCodeNotVisible,
                                                        CIMColor visibleColor, CIMColor notVisibleColor,
                                                        double visibleSize, double notVisibleSize)
        {
            await QueuedTask.Run(() =>
            {
                //Create the Unique Value Renderer
                CIMUniqueValueRenderer uniqueValueRenderer = new CIMUniqueValueRenderer();

                // set the value field
                uniqueValueRenderer.Fields = new string[] { visField };

                List<CIMUniqueValueClass> classes = new List<CIMUniqueValueClass>();

                List<CIMUniqueValue> noVisValues = new List<CIMUniqueValue>();
                CIMUniqueValue noVisValue = new CIMUniqueValue();
                noVisValue.FieldValues = new string[] { visCodeNotVisible.ToString() };
                noVisValues.Add(noVisValue);

                var ss = new CIMSolidStroke();
                ss.Color = notVisibleColor;
                ss.Width = notVisibleSize;
                ss.Enable = true;
                ss.LineStyle3D = Simple3DLineStyle.Tube;
                var noVisSymbol = SymbolFactory.Instance.ConstructLineSymbol(ss);

                var noVis = new CIMUniqueValueClass()
                {
                    Values = noVisValues.ToArray(),
                    Label = "Not Visible",
                    Visible = true,
                    Editable = true,
                    Symbol = new CIMSymbolReference() { Symbol = noVisSymbol }
                };


                List<CIMUniqueValue> hasVisValues = new List<CIMUniqueValue>();
                CIMUniqueValue hasVisValue = new CIMUniqueValue();
                hasVisValue.FieldValues = new string[] { visCodeVisible.ToString() };
                hasVisValues.Add(hasVisValue);

                var ss2 = new CIMSolidStroke();
                ss2.Color = visibleColor;
                ss2.Width = visibleSize;
                ss2.Enable = true;
                ss2.LineStyle3D = Simple3DLineStyle.Tube;
                var hasVisSymbol = SymbolFactory.Instance.ConstructLineSymbol(ss2);

                var hasVis = new CIMUniqueValueClass()
                {
                    Values = hasVisValues.ToArray(),
                    Label = "Visible",
                    Visible = true,
                    Editable = true,
                    Symbol = new CIMSymbolReference() { Symbol = hasVisSymbol }
                };

                classes.Add(hasVis);
                classes.Add(noVis);

                CIMUniqueValueGroup groupOne = new CIMUniqueValueGroup();
                groupOne.Heading = visField;
                groupOne.Classes = classes.ToArray();

                uniqueValueRenderer.Groups = new CIMUniqueValueGroup[] { groupOne };

                //Draw the rest with the default symbol
                uniqueValueRenderer.UseDefaultSymbol = false;
                featureLayer.SetRenderer(uniqueValueRenderer);
            });
        }

        private static bool isResultGoodAndReportMessages(IGPResult result, string toolToReport,
            List<object> toolParameters)
        {
            // Return if no errors
            if (!result.IsFailed)
                return true;

            // If failed, provide feedback of what went wrong
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(toolToReport);
            sb.AppendLine(" - GP Tool FAILED:");
            foreach (var msg in result.Messages)
                sb.AppendLine(msg.Text);

            if (toolParameters != null)
            {
                sb.Append("Parameters: ");
                int count = 0;
                foreach (var param in toolParameters)
                    sb.Append(string.Format("{0}:{1} ", count++, param));
                sb.AppendLine();
            }

            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(sb.ToString(),
                    VisibilityLibrary.Properties.Resources.CaptionError);

            return false;
        }

        public static FileGeodatabaseConnectionPath FgdbFileToConnectionPath(string fGdbPath)
        {
            return new FileGeodatabaseConnectionPath(new Uri(CoreModule.CurrentProject.DefaultGeodatabasePath));
        }

    } // end class

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
