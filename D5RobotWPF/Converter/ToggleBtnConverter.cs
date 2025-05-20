using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DOF5RobotControl_GUI.Converter
{
    class ToggleBtnConverter(string startText, string stopText) : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? stopText : startText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class FeedBtnConverter : ToggleBtnConverter
    {
        public FeedBtnConverter() : base("开始振动进给", "停止振动进给") { }
    }
}
