using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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

        private readonly ICameraControlService cameraControlService;
        private readonly IYoloDetectionService yoloDetectionService;

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


        public CameraViewModel(ICameraControlService cameraControlService, IYoloDetectionService yoloDetectionService)
        {
            this.cameraControlService = cameraControlService;
            this.yoloDetectionService = yoloDetectionService;

            this.cameraControlService.RegisterCallback(TopFrameReceived, BottomFrameReceived);
        }

        ~CameraViewModel()
        {
            cameraControlService.UnRegisterCallback(TopFrameReceived, BottomFrameReceived);
        }

        private void TopFrameReceived(object? sender, CamFrame e)
        {
            //PixelFormat pf = PixelFormats.Gray8; // 下面转成 bitmap 格式
            //int rawStride = (e.Width * pf.BitsPerPixel + 7) / 8;
            //dispatcher.Invoke(() =>
            //{
            //    BitmapSource bitmap = BitmapSource.Create(e.Width, e.Height, 96, 96, pf, null, e.Buffer, rawStride);
            //    TopImageSource = bitmap;
            //});

            //Image img = yoloDetectionService.Plot(e);
            //var img = yoloDetectionService.PlotTopAsync(e).Result;
            var img = yoloDetectionService.PlotTop(e);
            try
            {
                if (img is Image<Rgba32> img32)
                {
                    byte[] procRaw = new byte[img.Width * img.Height * 4];
                    img32.CopyPixelDataTo(procRaw);

                    PixelFormat pf = PixelFormats.Bgra32; // 下面转成 bitmap 格式
                    int rawStride = (e.Width * pf.BitsPerPixel + 7) / 8;
                    dispatcher.Invoke(() =>
                    {
                        BitmapSource bitmap = BitmapSource.Create(img32.Width, img32.Height, 96, 96, pf, null, procRaw, rawStride);
                        TopImageSource = bitmap;
                    });
                }
            }
            finally
            {
                img.Dispose();
            }
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
