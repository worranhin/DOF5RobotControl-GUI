using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace DOF5RobotControl_GUI.ViewModel
{
    public partial class MainViewModel
    {
        CameraWindow? _camWindow;

        [ObservableProperty]
        bool _cameraIsOpened = false;

        [ObservableProperty]
        private int _topCamMoveDistance = 0;
        [ObservableProperty]
        private int _bottomCamMoveAngle = 0;

        /***** 相机控制命令 *****/

        [RelayCommand]
        private void ToggleCamera()
        {
            if (!CameraIsOpened)
                OpenCamera();
            else
                CloseCamera();
        }

        /// <summary>
        /// 打开相机
        /// </summary>
        private void OpenCamera()
        {
            _cameraCtrlService.OpenCamera();

            _camWindow = App.Current.Services.GetRequiredService<CameraWindow>();
            _camWindow.Closed += (sender, e) => _cameraCtrlService.CloseCamera();
            _camWindow.Show();
            
            CameraIsOpened = true;
        }

        /// <summary>
        /// 关闭相机及其窗口
        /// </summary>
        internal void CloseCamera()
        {
            _camWindow?.Close();
            if (_cameraCtrlService.CameraIsOpened)
                _cameraCtrlService.CloseCamera();
            
            CameraIsOpened = false;
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
