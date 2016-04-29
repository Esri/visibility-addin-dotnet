using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        public static async Task CreateLayer(string featureclassName, string featureclassType)
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
            arguments.Add("ENABLED");

            arguments.Add(MapView.Active.Map.SpatialReference);

            IGPResult result = await Geoprocessing.ExecuteToolAsync("CreateFeatureclass_management", Geoprocessing.MakeValueArray(arguments.ToArray()));
        }

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
            IGPResult result = await Geoprocessing.ExecuteToolAsync("ConstructSightLines_3d", Geoprocessing.MakeValueArray(arguments.ToArray()), flags: GPExecuteToolFlags.Default);

            if (result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }
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

            IGPResult result = await Geoprocessing.ExecuteToolAsync("LineOfSight_3d", Geoprocessing.MakeValueArray(arguments.ToArray()), flags: GPExecuteToolFlags.Default);

            if(result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }

        public static async Task Delete(string name)
        {
            List<object> arguments = new List<object>();
            // in_surface
            arguments.Add(name);
            // in_line_feature_class
            arguments.Add("");

            IGPResult result = await Geoprocessing.ExecuteToolAsync("Delete_management", Geoprocessing.MakeValueArray(arguments.ToArray()));

            if (result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }

    }

    /// <summary>
    /// Extension method to find and locate attribute fields
    /// </summary>
    public static class FieldExtensions
    {
        /// <summary>
        ///     Find the field with the provided field name.
        /// </summary>
        /// <param name="table">Table or FeatureClass containing the field.</param>
        /// <param name="fieldName">
        ///     The name of the field to be retrieved.
        /// </param>
        /// <returns>
        ///     The field with the provided name. If no field of the given name is found, a null reference
        ///     is returned.
        /// </returns>
        public static Task<Field> GetFieldByNameAsync(this Table table, string fieldName)
        {
            Field foundField = null;

            if (String.IsNullOrEmpty(fieldName))
                return Task.FromResult(foundField);

            return QueuedTask.Run(() =>
            {
                IReadOnlyList<Field> fields = ((TableDefinition)table.GetDefinition()).GetFields();
                return fields.FirstOrDefault(a => a.Name.Equals(fieldName));
            });
        }

        /// <summary>
        ///     Find the first field of the provided field type.
        /// </summary>
        /// <param name="table">Table or FeatureClass containing the field.</param>
        /// <param name="fieldType">
        ///     The type of field to be retrieved.
        ///     <remarks>Some types can only exist once per table.</remarks>
        /// </param>
        /// <returns>
        ///     The first occurrence of the field type is returned. If no field of the given type is found, a null reference
        ///     is returned.
        /// </returns>
        public static Task<Field> GetFieldByTypeAsync(this Table table, FieldType fieldType)
        {
            return QueuedTask.Run(() =>
            {
                IReadOnlyList<Field> fields = ((TableDefinition)table.GetDefinition()).GetFields();
                return fields.FirstOrDefault(a => a.FieldType == fieldType);
            });
        }

        /// <summary>
        ///     Returns the ObjectID field from a table or feature class.
        /// </summary>
        /// <param name="table">Table or FeatureClass containing the ObjectID field.</param>
        /// <returns>The ObjectID field.</returns>
        public static Task<Field> GetOIDFieldAsync(this Table table)
        {
            return table.GetFieldByTypeAsync(FieldType.OID);
        }

        /// <summary>
        ///     Returns the field index of the shape/geometry field.
        /// </summary>
        /// <param name="table">FeatureClass containing the shape field.</param>
        /// <returns>The index of the shape field.</returns>
        public static Task<int> GetShapeFieldIndexAsync(this Table table)
        {
            return QueuedTask.Run(() =>
            {
                int fieldIndex = -1;

                var fcDefinition = table.GetDefinition() as FeatureClassDefinition;
                if (fcDefinition != null)
                {
                    fieldIndex = fcDefinition.FindField(fcDefinition.GetShapeField());
                }

                return fieldIndex;
            });
        }

        /// <summary>
        ///     Returns the name of the shape/geometry field.
        /// </summary>
        /// <param name="table">FeatureClass containing the shape field.</param>
        /// <returns>The name of the shape field.</returns>
        public static Task<string> GetShapeFieldNameAsync(this Table table)
        {
            return QueuedTask.Run(() =>
            {
                string shapeFieldName = String.Empty;

                var fcDefinition = table.GetDefinition() as FeatureClassDefinition;
                if (fcDefinition != null)
                {
                    shapeFieldName = fcDefinition.GetShapeField();
                }

                return shapeFieldName;
            });
        }
    }
}
