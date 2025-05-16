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
            try
            {
                _cameraCtrlService.OpenCamera();

                _camWindow = App.Current.Services.GetRequiredService<CameraWindow>();
                _camWindow.Closed += (sender, e) => _cameraCtrlService.CloseCamera();
                _camWindow.Show();

                CameraIsOpened = true;
            } catch (InvalidOperationException ex)
            {
                _popUpService.Show("请检查相机网卡是否打开，并尝试重启应用\n" + ex.ToString(), "Error when Open Camera");
            }
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
    }
}
