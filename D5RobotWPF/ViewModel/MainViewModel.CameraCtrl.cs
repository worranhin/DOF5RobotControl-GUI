using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace DOF5RobotControl_GUI.ViewModel
{
    public partial class MainViewModel
    {
        [ObservableProperty]
        private int _topCamMoveDistance = 0;
        [ObservableProperty]
        private int _bottomCamMoveAngle = 0;

        /***** 相机控制命令 *****/

        /// <summary>
        /// 打开相机
        /// </summary>
        [RelayCommand]
        private void OpenCamera()
        {
            _cameraCtrlService.OpenCamera();
            CameraWindow window = App.Current.Services.GetRequiredService<CameraWindow>();
            window.Closed += (sender, e) => _cameraCtrlService.CloseCamera();
            window.Show();
        }

        [RelayCommand]
        private async Task CameraGotoJawVaultAsync()
        {
            try
            {
                _cameraCtrlService.MoveTopCamera(TopCamMoveDistance);
                await Task.Delay(100); // 需要延时一小段时间才能确保通讯正常
                _cameraCtrlService.MoveBottomCamera(BottomCamMoveAngle);
            }
            catch (InvalidOperationException exc)
            {
                _popUpService.Show(exc.Message);
            }
        }

        [RelayCommand]
        private async Task CameraGotoPartsVaultAsync()
        {
            try
            {
                _cameraCtrlService.MoveTopCamera(-TopCamMoveDistance);
                await Task.Delay(100);
                _cameraCtrlService.MoveBottomCamera(-BottomCamMoveAngle);
            }
            catch (InvalidOperationException exc)
            {
                _popUpService.Show(exc.Message);
            }
        }

        [RelayCommand]
        private void TopCamMove()
        {
            try
            {
                _cameraCtrlService.MoveTopCamera(TopCamMoveDistance);
            }
            catch (InvalidOperationException exc)
            {
                _popUpService.Show(exc.Message);
            }
        }

        [RelayCommand]
        private void BottomCamMove()
        {
            try
            {
                _cameraCtrlService.MoveBottomCamera(BottomCamMoveAngle);
            }
            catch (InvalidOperationException exc)
            {
                _popUpService.Show(exc.Message);
            }
        }
    }
}
