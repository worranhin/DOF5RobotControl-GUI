﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using GxIAPINET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Threading.Tasks;
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

        [ObservableProperty]
        bool _isDisplayYoloBox = false;

        [ObservableProperty]
        private bool _isDisplayError = false;

        [ObservableProperty]
        private int _topCamMoveDistance = 0;
        [ObservableProperty]
        private int _bottomCamMoveAngle = 0;

        readonly ICameraControlService cameraControlService;
        readonly IYoloDetectionService yoloDetectionService;
        readonly IProcessImageService processImageService;
        readonly IPopUpService popUpService;

        bool isTopFrameProcessing = false;
        bool isBottomFrameProcessing = false;

        public CameraViewModel(ICameraControlService cameraControlService, 
            IYoloDetectionService yoloDetectionService, IProcessImageService processImageService, IPopUpService popUpService)
        {
            this.cameraControlService = cameraControlService;
            this.yoloDetectionService = yoloDetectionService;
            this.processImageService = processImageService;
            this.popUpService = popUpService;

            this.cameraControlService.RegisterCallback(TopFrameReceived, BottomFrameReceived);
        }

        ~CameraViewModel()
        {
            cameraControlService.UnRegisterCallback(TopFrameReceived, BottomFrameReceived);
        }

        [RelayCommand]
        private void ToggleDisplayError()
        {
            if (IsDisplayError)
            {
                var topImg = cameraControlService.GetTopFrame();
                var bottomImg = cameraControlService.GetBottomFrame();
                processImageService.Init(topImg, bottomImg);
            }
        }

        [RelayCommand]
        private async Task CameraGotoJawVaultAsync()
        {
            try
            {
                cameraControlService.MoveTopCamera(TopCamMoveDistance);
                await Task.Delay(100); // 需要延时一小段时间才能确保通讯正常
                cameraControlService.MoveBottomCamera(BottomCamMoveAngle);
            }
            catch (InvalidOperationException exc)
            {
                popUpService.Show(exc.Message);
            }
        }

        [RelayCommand]
        private async Task CameraGotoPartsVaultAsync()
        {
            try
            {
                cameraControlService.MoveTopCamera(-TopCamMoveDistance);
                await Task.Delay(100);
                cameraControlService.MoveBottomCamera(-BottomCamMoveAngle);
            }
            catch (InvalidOperationException exc)
            {
                popUpService.Show(exc.Message);
            }
        }

        [RelayCommand]
        private void TopCamMove()
        {
            try
            {
                cameraControlService.MoveTopCamera(TopCamMoveDistance);
            }
            catch (InvalidOperationException exc)
            {
                popUpService.Show(exc.Message);
            }
        }

        [RelayCommand]
        private void BottomCamMove()
        {
            try
            {
                cameraControlService.MoveBottomCamera(BottomCamMoveAngle);
            }
            catch (InvalidOperationException exc)
            {
                popUpService.Show(exc.Message);
            }
        }

        private async void TopFrameReceived(object? sender, CamFrame e)
        {
            if (isTopFrameProcessing)
                return;

            isTopFrameProcessing = true;

            if (IsDisplayYoloBox)
            {
                using var img = yoloDetectionService.TopPlot(e);

                if (img is Image<Rgba32> img32)
                {
                    byte[] procRaw = new byte[img.Width * img.Height * 4];
                    img32.CopyPixelDataTo(procRaw);

                    PixelFormat pf = PixelFormats.Bgra32; // 下面转成 bitmap 格式
                    int rawStride = (e.Width * pf.BitsPerPixel + 7) / 8;
                    dispatcher.Invoke(() =>
                    {
                        BitmapSource bitmap = BitmapSource.Create(img32.Width, img32.Height, 96, 96, pf, null, procRaw, rawStride);  // 这个 Bitmap 需要在 UI 线程创建
                        TopImageSource = bitmap;
                    });
                }
            }
            else
            {
                PixelFormat pf = PixelFormats.Gray8; // 下面转成 bitmap 格式
                int rawStride = (e.Width * pf.BitsPerPixel + 7) / 8;
                dispatcher.Invoke(() =>
                {
                    BitmapSource bitmap = BitmapSource.Create(e.Width, e.Height, 96, 96, pf, null, e.Buffer, rawStride);
                    TopImageSource = bitmap;
                });
            }

            if (IsDisplayError)
            {
                try
                {
                    (DPx, DPy, DRz) = await processImageService.GetJawErrorAsync(e);
                }
                catch (InvalidOperationException ex)
                {
                    popUpService.Show(ex.ToString(), "Error when get error");
                    IsDisplayError = false;
                }
            }

            isTopFrameProcessing = false;
        }

        private async void BottomFrameReceived(object? sender, CamFrame e)
        {
            if (isBottomFrameProcessing)
                return;

            isBottomFrameProcessing = true;

            if (IsDisplayYoloBox)
            {
                using var img = yoloDetectionService.BottomPlot(e);

                if (img is Image<Rgba32> img32)
                {
                    byte[] procRaw = new byte[img.Width * img.Height * 4];
                    img32.CopyPixelDataTo(procRaw);

                    PixelFormat pf = PixelFormats.Bgra32; // 下面转成 bitmap 格式
                    int rawStride = (e.Width * pf.BitsPerPixel + 7) / 8;
                    dispatcher.Invoke(() =>
                    {
                        BitmapSource bitmap = BitmapSource.Create(img32.Width, img32.Height, 96, 96, pf, null, procRaw, rawStride);
                        BottomImageSource = bitmap;
                    });
                }
            }
            else
            {
                PixelFormat pf = PixelFormats.Gray8; // 下面转成 bitmap 格式
                int rawStride = (e.Width * pf.BitsPerPixel + 7) / 8;
                dispatcher.Invoke(() =>
                {
                    BitmapSource bitmap = BitmapSource.Create(e.Width, e.Height, 96, 96, pf, null, e.Buffer, rawStride);
                    BottomImageSource = bitmap;
                });
            }

            if (IsDisplayError)
            {
                try
                {
                    DPz = await processImageService.ProcessBottomImageAsync(e);
                } catch (InvalidOperationException ex)
                {
                    popUpService.Show(ex.ToString(), "Error when get error");
                    IsDisplayError = false;
                }
            }

            isBottomFrameProcessing = false;
        }
    }
}
