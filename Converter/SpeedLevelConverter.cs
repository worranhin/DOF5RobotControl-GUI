using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DOF5RobotControl_GUI.Converter
{
    internal class SpeedLevelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int level = (int)value!;
            return level switch
            {
                0 => "Slow(0.01mm/s, 0.01degree/s)",
                1 => "Medium(0.1mm/s, 0.1degree/s)",
                2 => "Fast(1mm/s, 1degree/s)",
                _ => "Error occurred",
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
