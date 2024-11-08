using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.ViewModel
{
    internal class ManualControlViewModel : ObservableObject
    {
        private int _speedMode;
        public int SpeedMode
        {
            get => _speedMode;
            set => SetProperty(ref _speedMode, value);
        }

        private bool _gamepadConnected = false;
        public bool GamepadConnected
        {
            get => _gamepadConnected;
            set => SetProperty(ref _gamepadConnected, value);
        }
    }
}
