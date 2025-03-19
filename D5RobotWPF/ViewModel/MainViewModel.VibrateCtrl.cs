using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.ViewModel
{
    partial class MainViewModel
    {
        /***** 振动相关字段/属性 *****/
        [ObservableProperty]
        private bool _isVibrating = false;
        [ObservableProperty]
        private bool _isVibrateHorizontal = true;
        [ObservableProperty]
        private bool _isVibrateVertical = false;
        [ObservableProperty]
        private double _vibrateAmplitude = 0.05;
        [ObservableProperty]
        private double _vibrateFrequency = 10.0;

        /***** 处理振动相关 UI 逻辑 *****/

        [RelayCommand]
        private void ToggleVibrate()
        {
            if (!IsVibrating)
            {
                try
                {
                    _robotControlService.StartVibrate(IsVibrateHorizontal, IsVibrateVertical, VibrateAmplitude, VibrateFrequency);
                    IsVibrating = true;
                }
                catch (ArgumentException exc)
                {
                    if (exc.ParamName == "vibrateVertical")
                        _popUpService.Show(exc.Message, "Error while toggle vibration");
                    else
                        throw;
                }
            }
            else
            {
                _robotControlService.StopVibrate();
                IsVibrating = false;
            }
        }
    }
}
