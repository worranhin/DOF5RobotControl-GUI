using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DOF5RobotControl_GUI.Model;
using GxIAPINET;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using VisionLibrary;
using Windows.Graphics.Printing.Workflow;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DOF5RobotControl_GUI.ViewModel
{
    internal class TopImgRequestMessage : RequestMessage<ImageSource?> { }

    internal class BottomImgRequestMessage : RequestMessage<ImageSource?> { }

    public partial class CameraViewModel : ObservableObject
    {
        private enum CaptureTaskCameraSelect
        {
            TopCamera,
            BottomCamera
        };

        //const string TopCameraMac = "00-21-49-03-4D-95";
        //const string BottomCameraMac = "00-21-49-03-4D-94";

        [ObservableProperty]
        private bool _topCameraConnected = false;
        [ObservableProperty]
        private ImageSource? _topImageSource;

        [ObservableProperty]
        private bool _bottomCameraConnected = false;
        [ObservableProperty]
        private ImageSource? _bottomImageSource;

        private CancellationTokenSource? captureCancelSource;
        //private CancellationToken gxCameraTaskCancelToken;
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
            TopCamera.Instance.Open(true);
            BottomCamera.Instance.Open(true);
            TopCamera.Instance.FrameReceived += TopFrameReceived;
            BottomCamera.Instance.FrameReceived += BottomFrameReceived;
        }

        private void TopFrameReceived(object? sender, GxCamera.Frame e)
        {
            PixelFormat pf = PixelFormats.Gray8; // 下面转成 bitmap 格式
            int rawStride = (e.Width * pf.BitsPerPixel + 7) / 8;
            dispatcher.Invoke(() =>
            {
                BitmapSource bitmap = BitmapSource.Create(e.Width, e.Height, 96, 96, pf, null, e.Buffer, rawStride);
                TopImageSource = bitmap;
            });
        }

        private void BottomFrameReceived(object? sender, GxCamera.Frame e)
        {
            PixelFormat pf = PixelFormats.Gray8; // 下面转成 bitmap 格式
            int rawStride = (e.Width * pf.BitsPerPixel + 7) / 8;
            dispatcher.Invoke(() =>
            {
                BitmapSource bitmap = BitmapSource.Create(e.Width, e.Height, 96, 96, pf, null, e.Buffer, rawStride);
                BottomImageSource = bitmap;
            });
        }

        ~CameraViewModel()
        {
            captureCancelSource?.Cancel();
            captureCancelSource?.Dispose();
            captureCancelSource = null;

            WeakReferenceMessenger.Default.UnregisterAll(this);
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

        /// <summary>
        /// 开始捕获相机图像，并更新显示的图像
        /// </summary>
        private async Task StartCaptureImage()
        {
            try
            {
                var topCamera = TopCamera.Instance;
                topCamera.Open();

                var bottomCamera = BottomCamera.Instance;
                bottomCamera.Open();

                captureCancelSource = new();
                var token = captureCancelSource.Token;

                await Task.Run(async () =>
                {
                    const int period = 200; // 每次循环的间隔，实际刷新率可能大于这个值

                    while (!token.IsCancellationRequested)
                    {
                        //var topImg = topCamera.GetBitmapFrame();

                        try
                        {
                            var topFrame = topCamera.GetRawFrame();
                            dispatcher.Invoke(() =>
                            {
                                PixelFormat pf = PixelFormats.Gray8; // 下面转成 bitmap 格式
                                int rawStride = (topFrame.Width * pf.BitsPerPixel + 7) / 8;
                                BitmapSource bitmap = BitmapSource.Create(topFrame.Width, topFrame.Height, 96, 96, pf, null, topFrame.Buffer, rawStride);
                                TopImageSource = bitmap;
                            });
                        }
                        catch (InvalidOperationException ex)
                        {
                            Debug.WriteLine("Error when get top frame: " + ex.Message);
                        }

                        dispatcher.Invoke(() =>
                        {
                            try
                            {
                                var bottomImg = bottomCamera.GetBitmapFrame();
                                BottomImageSource = bottomImg;
                            }
                            catch (InvalidOperationException ex)
                            {
                                Debug.WriteLine("Error when update Image: " + ex.Message);
                            }
                        });

                        await Task.Delay(period);
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "开始捕获图像时发生错误");
                //throw;
            }

            //try
            //{
            //    gxCameraTaskCancelSource = new();
            //    gxCameraTaskCancelToken = gxCameraTaskCancelSource.Token;
            //    await Task.Run(GxLibTask);
            //}
            //finally
            //{
            //    gxCameraTaskCancelSource?.Dispose();
            //    gxCameraTaskCancelSource = null;
            //}
        }

        /// <summary>
        /// 停止捕获相机图像
        /// </summary>
        public void StopCaptureImage()
        {
            captureCancelSource?.Cancel();
            captureCancelSource?.Dispose();
            captureCancelSource = null;

            TopCamera.Instance.Close();
            BottomCamera.Instance.Close();
        }
    }
}
