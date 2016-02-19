using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using ESRI.ArcGIS.Geometry;

namespace ArcMapAddinVisibility
{
    [ValueConversion(typeof(IPoint), typeof(String))]
    public class IPointToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return "NA";

            var point = value as IPoint;
            return string.Format("{0:0.0#####} {1:0.0#####}", point.Y, point.X);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
