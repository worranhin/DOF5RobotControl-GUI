using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DOF5RobotControl_GUI
{
    class MainViewModel : ObservableObject
    {
        private bool _systemConnected = false;
        public bool SystemConnected
        {
            get => _systemConnected;
            set => SetProperty(ref _systemConnected, value);
        }

        private string[] _portsAvailable = [];
        public string[] PortsAvailable
        {
            get => _portsAvailable;
            set => SetProperty(ref _portsAvailable, value);
        }

        private string _selectedPort = "";
        public string SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        private JointsPosition _targetPosition = new(0, 0, 0, 0, 0);
        public JointsPosition TargetPosition
        {
            get => _targetPosition;
            set => SetProperty(ref _targetPosition, value);
        }
    }
}
