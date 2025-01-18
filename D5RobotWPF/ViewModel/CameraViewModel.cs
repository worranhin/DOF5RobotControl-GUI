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
            WeakReferenceMessenger.Default.Register<CameraViewModel, TopImgRequestMessage>(this, (r, m) =>
            {
                m.Reply(r.TopImageSource);
            });

            WeakReferenceMessenger.Default.Register<CameraViewModel, BottomImgRequestMessage>(this, (r, m) =>
            {
                m.Reply(r.BottomImageSource);
            });

            StartCaptureImage();
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


            //var topTask = ImageProcessor.ProcessTopImgAsync(topBitmap);
            //var bottomTask = ImageProcessor.ProcessBottomImgAsync(bottomBitmap);

            //await Task.WhenAll(topTask, bottomTask);
            //(DPx, DPy, DRz) = await topTask;
            //DPz = await bottomTask;

            IsProcessingImg = false;
        }

        private void TestImageSource()
        {

        }

        /// <summary>
        /// 开始捕获相机图像，并更新显示的图像
        /// </summary>
        private void StartCaptureImage()
        {
            try
            {
                var topCamera = TopCamera.Instance;
                topCamera.Open();

                var bottomCamera = BottomCamera.Instance;
                bottomCamera.Open();

                captureCancelSource = new();
                var token = captureCancelSource.Token;

                Task.Run(async () =>
                {
                    const int period = 1000; // 每次循环的间隔，实际刷新率可能大于这个值

                    while (!token.IsCancellationRequested)
                    {
                        var topImg = topCamera.GetBitmapFrame();                        
                        dispatcher.Invoke(() => { TopImageSource = topImg; });

                        var bottomImg = bottomCamera.GetBitmapFrame();
                        dispatcher.Invoke(() => { BottomImageSource = bottomImg; });

                        await Task.Delay(period);
                    }
                });
            } catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "开始捕获图像时发生错误");
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
        }

        /// <summary>
        /// 大恒相机库的初始化和逆初始化处理
        /// </summary>
        //private async Task GxLibTask()
        //{
        //    try
        //    {
        //        // 初始化大恒相机库
        //        IGXFactory.GetInstance().Init();

        //        // 枚举设备
        //        List<IGXDeviceInfo> deviceInfos = new();
        //        IGXFactory.GetInstance().UpdateAllDeviceList(1000, deviceInfos);  // 枚举相机，文档建议在打开相机前先枚举
        //        if (deviceInfos.Count == 0)
        //        {
        //            throw new CGalaxyException(-3, "No device found.");
        //        }

        //        foreach (IGXDeviceInfo info in deviceInfos)
        //        {
        //            Debug.WriteLine(info.GetModelName());
        //            Debug.WriteLine(info.GetVendorName());
        //        }

        //        // 获取 Interface 信息
        //        List<IGXInterfaceInfo> gxInterfaceList = new();
        //        IGXFactory.GetInstance().GetAllInterfaceInfo(gxInterfaceList);
        //        foreach (IGXInterfaceInfo info in gxInterfaceList)
        //        {
        //            Debug.WriteLine(info.GetModelName());
        //            Debug.WriteLine(info.GetVendorName());
        //        }

        //        // 开启两个相机的采集任务
        //        var topCameraTask = Task.Run(() => GxCameraCaptureAsync(CaptureTaskCameraSelect.TopCamera), gxCameraTaskCancelToken);
        //        var bottomCameraTask = Task.Run(() => GxCameraCaptureAsync(CaptureTaskCameraSelect.BottomCamera), gxCameraTaskCancelToken);

        //        //await topCameraTask;
        //        //await bottomCameraTask;
        //        await Task.WhenAll(topCameraTask, bottomCameraTask);
        //    }
        //    catch (CGalaxyException ex)
        //    {
        //        if (ex.GetErrorCode() == -3)
        //        {
        //            dispatcher.Invoke(() =>
        //            {
        //                MessageBox.Show("错误信息：" + ex.Message, "错误：找不到相机");
        //            });
        //        }
        //        else
        //        {
        //            Debug.WriteLine("Error code: " + ex.GetErrorCode().ToString());
        //            Debug.WriteLine("Error message: " + ex.Message);
        //            throw;
        //        }
        //    }
        //    finally
        //    {
        //        IGXFactory.GetInstance().Uninit();
        //    }
        //}

        /// <summary>
        /// 相机采集任务
        /// </summary>
        /// <param name="mac">相机的 MAC 地址</param>
        //private void GxCameraCaptureAsync(CaptureTaskCameraSelect camSelect)
        //{
        //    const int timeout = 1000; // TODO: 测试并改小这个值
        //    const int period = 100; // 刷新率为 10Hz
        //    string mac;
        //    switch (camSelect)
        //    {
        //        case CaptureTaskCameraSelect.TopCamera:
        //            mac = TopCameraMac;
        //            break;
        //        case CaptureTaskCameraSelect.BottomCamera:
        //            mac = BottomCameraMac;
        //            break;
        //        default:
        //            Debug.WriteLine("Error in GxCameraCaptureTask: please use proper CaptureTaskCameraSelect enum");
        //            return;
        //    }


        //    IGXDevice? camera = null;

        //    try
        //    {
        //        // 打开相机
        //        camera = IGXFactory.GetInstance().OpenDeviceByMAC(mac, GX_ACCESS_MODE.GX_ACCESS_EXCLUSIVE);

        //        // 采集图像
        //        if (camera != null)
        //        {
        //            UInt32 streamCount = camera.GetStreamCount();
        //            if (streamCount > 0)
        //            {
        //                IGXStream stream = camera.OpenStream(0);
        //                IGXFeatureControl featControl = camera.GetRemoteFeatureControl();
        //                GX_DEVICE_CLASS_LIST deviceClass = camera.GetDeviceInfo().GetDeviceClass();

        //                // 设置最优包长
        //                if (GX_DEVICE_CLASS_LIST.GX_DEVICE_CLASS_GEV == deviceClass)
        //                {
        //                    if (true == featControl.IsImplemented("GevSCPSPacketSize"))
        //                    {
        //                        UInt32 packetSize = stream.GetOptimalPacketSize();
        //                        featControl.GetIntFeature("GevSCPSPacketSize").SetValue(packetSize);
        //                    }
        //                }


        //                //stream.SetAcqusitionBufferNumber(10); // 设置缓存数量，在开采前设置

        //                /*** 下面是一些相机配置 ***/

        //                {
        //                    // 设置 buffer 行为（好像这个无效）
        //                    if (featControl.IsImplemented("StreamBufferHandlingMode"))
        //                    {
        //                        featControl.GetEnumFeature("StreamBufferHandlingMode").SetValue("NewestOnly");
        //                        string s = featControl.GetEnumFeature("StreamBufferHandlingMode").GetValue();
        //                        Debug.Assert(s == "NewestOnly");
        //                    }
        //                    else
        //                    {
        //                        Debug.WriteLine("StreamBufferHandlingMode not supported");
        //                    }

        //                    // 设置采集模式
        //                    if (featControl.IsImplemented("AcquisitionMode"))
        //                    {
        //                        featControl.GetEnumFeature("AcquisitionMode").SetValue("Continuous");
        //                        string s = featControl.GetEnumFeature("AcquisitionMode").GetValue();
        //                        Debug.Assert(s == "Continuous");
        //                    }
        //                    else
        //                    {
        //                        Debug.WriteLine("AcquisitionMode not supported");
        //                    }

        //                    if (featControl.IsImplemented("TriggerSelector") && featControl.IsImplemented("TriggerMode"))
        //                    {
        //                        featControl.GetEnumFeature("TriggerSelector").SetValue("FrameStart"); // 这个是调试软件提供的，不清楚是否必要

        //                        // 设置触发模式
        //                        if (featControl.IsImplemented("TriggerMode"))
        //                        {
        //                            featControl.GetEnumFeature("TriggerMode").SetValue("Off");
        //                            string s = featControl.GetEnumFeature("TriggerMode").GetValue();
        //                            Debug.Assert(s == "Off");
        //                        }
        //                        else
        //                        {
        //                            Debug.WriteLine("TriggerMode not supported");
        //                        }

        //                        // 设置触发源
        //                        if (featControl.IsImplemented("TriggerSource"))
        //                        {
        //                            featControl.GetEnumFeature("TriggerSource").SetValue("Software");
        //                            string s = featControl.GetEnumFeature("TriggerSource").GetValue();
        //                            Debug.Assert(s == "Software");
        //                        }
        //                        else
        //                        {
        //                            Debug.WriteLine("TriggerSource not supported");
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Debug.WriteLine("TriggerSelector not supported");
        //                    }

        //                    // 设置采集帧率调节模式：控制采集帧率是否激活
        //                    if (featControl.IsImplemented("AcquisitionFrameRateMode"))
        //                    {
        //                        featControl.GetEnumFeature("AcquisitionFrameRateMode").SetValue("On");
        //                        string s = featControl.GetEnumFeature("AcquisitionFrameRateMode").GetValue();
        //                        Debug.Assert(s == "On");
        //                    }
        //                    else
        //                    {
        //                        Debug.WriteLine("AcquisitionFrameRateMode not supported");
        //                    }

        //                    // 设置采集帧率值，当采集帧率调节模式为 On 时有效
        //                    if (featControl.IsImplemented("AcquisitionFrameRate"))
        //                    {
        //                        featControl.GetFloatFeature("AcquisitionFrameRate").SetValue(5.0000);
        //                        double d = featControl.GetFloatFeature("AcquisitionFrameRate").GetValue();
        //                        Debug.Assert(d == 5.0000);
        //                    }
        //                    else
        //                    {
        //                        Debug.WriteLine("AcquisitionFrameRate not supported");
        //                    }
        //                }

        //                /***** 相机配置结束 *****/

        //                stream.StartGrab();  // 开启流通道
        //                featControl.GetCommandFeature("AcquisitionStart").Execute();  // 发送开采命令，必须先开启流通道

        //                bool isRunningGood = true;
        //                while (!gxCameraTaskCancelToken.IsCancellationRequested && isRunningGood)
        //                {
        //                    try
        //                    {
        //                        //featControl.GetCommandFeature("TriggerSoftware").Execute();
        //                        var frameData = stream.DQBuf(timeout);  // 零拷贝采单帧，超时 500ms
        //                                                                //var frameData = stream.GetImage(500); // 拷贝采单帧，超时 500ms
        //                                                                // 处理图像
        //                                                                //Debug.WriteLine(frameData.GetStatus());

        //                        try
        //                        {
        //                            if (frameData.GetStatus() != GX_FRAME_STATUS_LIST.GX_FRAME_STATUS_SUCCESS)
        //                            {
        //                                Debug.WriteLine("采集到不完整帧");
        //                                continue;
        //                            }

        //                            ulong width = frameData.GetWidth();
        //                            ulong height = frameData.GetHeight();

        //                            if (width != 2592 || height != 2048)
        //                                continue;
        //                            //Debug.Assert(width == 2592);
        //                            //Debug.Assert(height == 2048);
        //                            var pixelFormat = frameData.GetPixelFormat();
        //                            if (pixelFormat == GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_MONO8)
        //                            {

        //                                var pRaw8Buffer = frameData.ConvertToRaw8(GX_VALID_BIT_LIST.GX_BIT_0_7);

        //                                var frameMat = Mat.FromPixelData((int)height, (int)width, MatType.CV_8U, pRaw8Buffer);

        //                                // 更新 UI 图像
        //                                dispatcher.Invoke(() =>
        //                                {
        //                                    switch (camSelect)
        //                                    {
        //                                        case CaptureTaskCameraSelect.TopCamera:
        //                                            //TopImgSrcMutex.WaitOne();
        //                                            TopImageSource = frameMat.ToBitmapSource();
        //                                            //TopImgSrcMutex.ReleaseMutex();
        //                                            break;
        //                                        case CaptureTaskCameraSelect.BottomCamera:
        //                                            //BottomImgSrcMutex.WaitOne();
        //                                            BottomImageSource = frameMat.ToBitmapSource();
        //                                            //BottomImgSrcMutex.ReleaseMutex();
        //                                            break;
        //                                        default:
        //                                            Debug.WriteLine("Error in GxCameraCaptureTask: please use proper CaptureTaskCameraSelect enum");
        //                                            isRunningGood = false;
        //                                            break;
        //                                    }
        //                                });
        //                            }
        //                            else
        //                            {
        //                                Debug.WriteLine("Format error!");
        //                            }
        //                        }
        //                        finally
        //                        {
        //                            stream.QBuf(frameData);
        //                        }

        //                        Thread.Sleep(period);
        //                        //await Task.Delay(period);
        //                    }
        //                    catch (CGalaxyException ex)
        //                    {
        //                        if (ex.GetErrorCode() == -14)
        //                        {
        //                            Debug.WriteLine($"{camSelect} timeout in capture loop: " + ex.Message);
        //                        }
        //                        else
        //                        {
        //                            Debug.WriteLine("Error in camera capture loop:");
        //                            Debug.WriteLine(ex.Message);
        //                            throw;
        //                        }
        //                    }
        //                }

        //                featControl.GetCommandFeature("AcquisitionStop").Execute();  // 发送停采命令
        //                stream.StopGrab();
        //                stream.Close(); // 关闭流通道
        //            }
        //        }
        //    }
        //    catch (CGalaxyException ex)
        //    {

        //        if (ex.GetErrorCode() == -8)
        //        {
        //            dispatcher.Invoke(() =>
        //            {
        //                MessageBox.Show("打开相机失败，请确认是否被占用（可尝试重新拔出）");
        //            });
        //        }
        //        else if (ex.GetErrorCode() == -14)
        //        {
        //            Debug.WriteLine("Camera timeout: " + ex.Message);
        //        }
        //        else
        //        {
        //            Debug.WriteLine("Error code: " + ex.GetErrorCode().ToString());
        //            Debug.WriteLine("Error message: " + ex.Message);
        //            throw;
        //        }
        //    }
        //    finally
        //    {
        //        // 关闭相机
        //        camera?.Close();
        //    }
        //}
    }
}
