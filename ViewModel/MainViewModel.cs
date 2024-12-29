using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DOF5RobotControl_GUI.Model;

namespace DOF5RobotControl_GUI.ViewModel
{
    partial class MainViewModel : ObservableObject
    {
        private bool _systemConnected = false;
        public bool SystemConnected
        {
            get => _systemConnected;
            set => SetProperty(ref _systemConnected, value);
        }

        private string[] _portsAvailable = Array.Empty<string>();
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

        private RoboticState _targetState = new(0, 0, 0, 0, 0);
        public RoboticState TargetState
        {
            get => _targetState;
            set => SetProperty(ref _targetState, value);
        }

        [ObservableProperty]
        private RoboticState _currentState = new(0, 0, 0, 0, 0);

        [RelayCommand]
        private void SetTargetJoints(D5Robot.Joints joints)
        {
            TargetState.SetFromD5RJoints(joints);
        }
    }
}
