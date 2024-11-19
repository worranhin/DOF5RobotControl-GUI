using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.ViewModel
{
    internal class MyData : INotifyPropertyChanged
    {

        public MyData()
        {
            _colorName = "Blue";
            ColorName = "Blue";
        }

        private string _colorName;
        public string ColorName
        {
            get { return _colorName; }
            set
            {
                if (_colorName != value)
                {
                    _colorName = value;
                    OnpropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnpropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
