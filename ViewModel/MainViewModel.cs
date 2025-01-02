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
using D5R;
using System.Windows;

namespace DOF5RobotControl_GUI.ViewModel
{
    partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _systemConnected = false;
        [ObservableProperty]
        private string[] _portsAvailable = Array.Empty<string>();
        [ObservableProperty]
        private string _selectedPort = "";
        [ObservableProperty]
        private RoboticState _targetState = new(0, 0, 0, 0, 0);
        [ObservableProperty]
        private RoboticState _currentState = new(0, 0, 0, 0, 0);

        /***** Jog 相关 *****/
        public IEnumerable<JogHandler.JogMode> JogModes => Enum.GetValues(typeof(JogHandler.JogMode)).Cast<JogHandler.JogMode>();
        [ObservableProperty]
        private JogHandler.JogMode _jogMode = JogHandler.JogMode.OneStep;
        /***** Jog 相关结束 *****/

        [RelayCommand]
        private void SetTargetJoints(Joints joints)
        {
            TargetState.SetFromD5RJoints(joints);
        }

        [RelayCommand]
        private void SetTargetJointsFromCurrent()
        {
            var joints = CurrentState.ToD5RJoints();
            TargetState.SetFromD5RJoints(joints);
        }

        ///// 处理振动相关 UI 逻辑 /////

        internal VibrateHelper? VibrateHelper;
        [ObservableProperty]
        private bool _isVibrating = false;
        [RelayCommand]
        private void ToggleVibrate()
        {
            if (VibrateHelper == null)
            {
                MessageBox.Show("While toggle vibrate: vibrateHelper is null!");
                return;
            }

            if (!IsVibrating)
            {
                VibrateHelper.Start();
                IsVibrating = true;
            }
            else
            {
                VibrateHelper.Stop();
                IsVibrating = false;
            }
        }

        ///// 处理振动结束 /////
    }
}
