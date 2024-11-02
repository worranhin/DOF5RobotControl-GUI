using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI
{
    internal class ManualControlData : INotifyPropertyChanged
    {
        private int _speedMode;
        public int SpeedMode
        {
            get { return _speedMode; }
            set
            {
                if (_speedMode != value)
                {
                    _speedMode = value;
                    OnpropertyChanged();
                }
            }
        }

        private bool _gamepadStatus = false;
        public bool GamepadStatus
        {
            get { return _gamepadStatus; }
            set
            {
                if (_gamepadStatus != value)
                {
                    _gamepadStatus = value;
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
