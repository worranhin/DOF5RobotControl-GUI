using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DOF5RobotControl_GUI.ViewModel
{
    public partial class CameraViewModel : ObservableObject
    {
        private enum CaptureTaskCameraSelect
        {
            TopCamera,
            BottomCamera
        };

        [ObservableProperty]
        private bool _topCameraConnected = false;
        [ObservableProperty]
        private ImageSource? _topImageSource;

        [ObservableProperty]
        private bool _bottomCameraConnected = false;
        [ObservableProperty]
        private ImageSource? _bottomImageSource;

        private readonly Dispatcher dispatcher = Application.Current.Dispatcher;

        // 图像处理相关
        [ObservableProperty]
        bool _isProcessingImg = false;
        [ObservableProperty]
        double _dPx = double.NaN;
        [ObservableProperty]
        double _dPy = double.NaN;
        [ObservableProperty]
        double _dPz = double.NaN;
        [ObservableProperty]
        double _dRy = double.NaN;
        [ObservableProperty]
        double _dRz = double.NaN;


        public CameraViewModel()
        {
            App.Current.Services.GetService<ICameraControlService>()?.RegisterCallback(TopFrameReceived, BottomFrameReceived);
        }

        ~CameraViewModel()
        {
            App.Current.Services.GetService<ICameraControlService>()?.UnRegisterCallback(TopFrameReceived, BottomFrameReceived);
        }

        private void TopFrameReceived(object? sender, CamFrame e)
        {
            PixelFormat pf = PixelFormats.Gray8; // 下面转成 bitmap 格式
            int rawStride = (e.Width * pf.BitsPerPixel + 7) / 8;
            dispatcher.Invoke(() =>
            {
                BitmapSource bitmap = BitmapSource.Create(e.Width, e.Height, 96, 96, pf, null, e.Buffer, rawStride);
                TopImageSource = bitmap;
            });
        }

        private void BottomFrameReceived(object? sender, CamFrame e)
        {
            PixelFormat pf = PixelFormats.Gray8; // 下面转成 bitmap 格式
            int rawStride = (e.Width * pf.BitsPerPixel + 7) / 8;
            dispatcher.Invoke(() =>
            {
                BitmapSource bitmap = BitmapSource.Create(e.Width, e.Height, 96, 96, pf, null, e.Buffer, rawStride);
                BottomImageSource = bitmap;
            });
        }

        [RelayCommand]
        private async Task GetErrorAsync()
        {
            IsProcessingImg = true;

            if (TopImageSource is not BitmapSource topBitmap || BottomImageSource is not BitmapSource bottomBitmap)
            {
                Debug.WriteLine("ImageSource is not right.");
                return;
            }

            try
            {
                (DPx, DPy, DRz) = await ImageProcessor.ProcessTopImgAsync(topBitmap);
                DPz = await ImageProcessor.ProcessBottomImgAsync(bottomBitmap);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error when get error");
            }

            IsProcessingImg = false;
        }
    }
}
