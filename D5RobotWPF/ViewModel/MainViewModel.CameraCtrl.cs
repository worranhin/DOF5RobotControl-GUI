using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.ViewModel
{
    public partial class MainViewModel
    {
        [ObservableProperty]
        private int _topCamMoveDistance = 0;
        [ObservableProperty]
        private int _bottomCamMoveAngle = 0;

        [RelayCommand]
        private void TopCamMove()
        {
            _cameraCtrlService.MoveTopCamera(TopCamMoveDistance);
        }

        [RelayCommand]
        private void BottomCamMove()
        {
            _cameraCtrlService.MoveBottomCamera(BottomCamMoveAngle);
        }
    }
}
