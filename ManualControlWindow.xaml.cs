using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.ViewModel;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using SharpDX.XInput;
using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Input;
using Joints = DOF5RobotControl_GUI.Model.D5Robot.Joints;
using GxIAPINET;
using System.Threading;
using System.Linq.Expressions;
using System.Windows.Media;

namespace DOF5RobotControl_GUI
{
    /// <summary>
    /// ManualControlWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ManualControlWindow : System.Windows.Window
    {
        private enum CaptureTaskCameraSelect {
            TopCamera,
            BottomCamera
        };

        static readonly string TopCameraMac = "00-21-49-03-4D-95";
        static readonly string BottomCameraMac = "00-21-49-03-4D-94";

        private readonly ManualControlViewModel viewModel = new();
        private readonly static SoundPlayer lowPlayer = new("res/Low.wav");
        private readonly static SoundPlayer mediumPlayer = new("res/Medium.wav");
        private readonly static SoundPlayer highPlayer = new("res/High.wav");
        private readonly static int controlPeriod = 20;  // ms
        //private readonly VideoCapture capture;
        private readonly Mat frame;
        //private readonly CancellationTokenSource captureCancelSource;
        //private readonly CancellationToken captureCancelToken;
        private readonly CancellationTokenSource xInputCancelSource;
        private readonly CancellationToken xInputCancelToken;
        private readonly CancellationTokenSource gxCameraTaskCancelSource;
        private readonly CancellationToken gxCameraTaskCancelToken;
        private readonly JogHandler jogHandler;
        private readonly D5Robot robot;
        private readonly int natorJogResolution = 100000;
        private readonly int RMDJogResolution = 20;
        private int speedLevel = 0;

        public ManualControlWindow(D5Robot robot)
        {
            InitializeComponent();
            this.Closed += WindowClosed;

            //capture = new VideoCapture(1);
            frame = new Mat();
            //captureCancelSource = new();
            //captureCancelToken = captureCancelSource.Token;
            xInputCancelSource = new();
            xInputCancelToken = xInputCancelSource.Token;
            gxCameraTaskCancelSource = new();
            gxCameraTaskCancelToken = gxCameraTaskCancelSource.Token;
            this.robot = robot;
            jogHandler = new(robot);
            this.DataContext = this.viewModel;

            // 运行两个 Task
            //Task.Run(CaptureCameraTask, captureCancelToken);
            Task.Run(XInputControlTask, xInputCancelToken);
            Task.Run(GxLibTask, gxCameraTaskCancelToken);

        }

        private void WindowClosed(object? sender, EventArgs e)
        {
            //captureCancelSource.Cancel();
            xInputCancelSource.Cancel();
            gxCameraTaskCancelSource.Cancel();
        }

        //private void CaptureCameraTask()
        //{
        //    int retrys = 0;

        //    while (!captureCancelToken.IsCancellationRequested)
        //    {
        //        bool hasFrame = capture.Read(frame);
        //        if (!hasFrame)
        //        {
        //            if (retrys++ >= 10)  // 重试超时，关闭线程
        //            {
        //                Dispatcher.Invoke(() =>
        //                {
        //                    MessageBox.Show("Fail to access camera frame.");
        //                });
        //                break;

        //            }

        //            Thread.Sleep(300);
        //            continue;
        //        }

        //        Dispatcher.Invoke(() =>
        //        {
        //            FrameImage.Source = BitmapSourceConverter.ToBitmapSource(frame);
        //        });

        //        Thread.Sleep(40);  // 25帧
        //    }

        //    // release resources
        //    capture.Dispose();
        //    frame?.Dispose();
        //}

        private void TestXInputTask()
        {
            var controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };
            // Get 1st controller available
            Controller? controller = null;
            foreach (var selectControler in controllers)
            {
                if (selectControler.IsConnected)
                {
                    controller = selectControler;
                    break;
                }
            }

            if (controller == null)
            {
                Debug.WriteLine("No XInput controller installed");
            }
            else
            {
                Debug.WriteLine("Found a XInput controller available");
                Debug.WriteLine("Press buttons on the controller to display events or escape key to exit... ");

                // Poll events from joystick
                var previousState = controller.GetState();
                while (controller.IsConnected)
                {
                    var state = controller.GetState();
                    if (previousState.PacketNumber != state.PacketNumber)
                        Debug.WriteLine(state.Gamepad);
                    Thread.Sleep(10);
                    previousState = state;
                }
            }
            Debug.WriteLine("End XGamepadApp");
        }

        private void XInputControlTask()
        {
            const int ThumbsThreshold = 15000;
            const int TrigerThreshold = 120;

            var controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };
            // Get 1st controller available
            Controller? controller = null;
            foreach (var selectControler in controllers)
            {
                if (selectControler.IsConnected)
                {
                    controller = selectControler;
                    break;
                }
            }

            if (controller == null)
            {
                //Debug.WriteLine("No XInput controller installed");
                Dispatcher.Invoke(() => MessageBox.Show("No XInput controller installed."));
                this.viewModel.GamepadConnected = false;
                return;
            }

            Debug.WriteLine("Found a XInput controller available");
            this.viewModel.GamepadConnected = true;

            // Poll events from joystick
            while (controller.IsConnected && !xInputCancelToken.IsCancellationRequested)
            {
                var state = controller.GetState();

                // 判断是否切换速度
                var res = controller.GetKeystroke(DeviceQueryType.Gamepad, out Keystroke ks);
                if (res == 0 && ks.Flags == KeyStrokeFlags.KeyDown)
                {
                    if (ks.VirtualKey == GamepadKeyCode.DPadUp)
                    {
                        speedLevel++;
                        speedLevel = speedLevel > 2 ? 2 : speedLevel;
                        viewModel.SpeedMode = speedLevel;
                        PlaySound(speedLevel);
                        continue;
                    }
                    else if (ks.VirtualKey == GamepadKeyCode.DPadDown)
                    {
                        speedLevel--;
                        speedLevel = speedLevel < 0 ? 0 : speedLevel;
                        viewModel.SpeedMode = speedLevel;
                        PlaySound(speedLevel);
                        continue;
                    }
                }


                // 根据手柄输入确定输出的位移量
                Joints joints = new();

                if (state.Gamepad.RightThumbX <= -ThumbsThreshold)
                    joints.R1 = SelectRMDSpeed(speedLevel);
                else if (state.Gamepad.RightThumbX >= ThumbsThreshold)
                    joints.R1 = -SelectRMDSpeed(speedLevel);

                if (state.Gamepad.LeftThumbX <= -ThumbsThreshold)
                    joints.P2 = SelectNatorSpeed(speedLevel);
                else if (state.Gamepad.LeftThumbX >= ThumbsThreshold)
                    joints.P2 = -SelectNatorSpeed(speedLevel);

                if (state.Gamepad.LeftThumbY <= -ThumbsThreshold)
                    joints.P3 = -SelectNatorSpeed(speedLevel);
                else if (state.Gamepad.LeftThumbY >= ThumbsThreshold)
                    joints.P3 = SelectNatorSpeed(speedLevel);

                if (state.Gamepad.LeftTrigger >= TrigerThreshold)
                    joints.P4 = -SelectNatorSpeed(speedLevel);
                else if (state.Gamepad.RightTrigger >= TrigerThreshold)
                    joints.P4 = SelectNatorSpeed(speedLevel);

                if (state.Gamepad.RightThumbY <= -ThumbsThreshold)
                    joints.R5 = -SelectRMDSpeed(speedLevel);
                else if (state.Gamepad.RightThumbY >= ThumbsThreshold)
                    joints.R5 = SelectRMDSpeed(speedLevel);

                if (!jogHandler.isJogging)
                {
                    if (joints.R1 != 0 || joints.P2 != 0 || joints.P3 != 0 || joints.P4 != 0 || joints.R5 != 0)
                    {
                        Debug.WriteLine($"R1:{joints.R1}, P2:{joints.P2}, P3:{joints.P3}, P4:{joints.P4}, R5:{joints.R5}");
                        var result = robot.JointsMoveRelative(joints);
                        if (result != D5Robot.ErrorCode.OK)
                        {
                            Dispatcher.Invoke(() => MessageBox.Show($"JointsMoveRelative error in xInputControlTask: {result}"));
                            viewModel.GamepadConnected = false;
                            //break;
                            return;
                        }
                    }
                }

                Thread.Sleep(controlPeriod);  // 控制周期
            }

            // 线程清理

            if (!xInputCancelToken.IsCancellationRequested)
            {
                xInputCancelSource.Cancel();
                viewModel.GamepadConnected = false;
                Dispatcher.Invoke(() => MessageBox.Show("Gamepad disconnected!"));
            }
        }

        /// <summary>
        /// 大恒相机库的初始化和逆初始化处理
        /// </summary>
        private void GxLibTask()
        {
            try
            {
                // 初始化大恒相机库
                IGXFactory.GetInstance().Init();

                // 枚举设备
                List<IGXDeviceInfo> deviceInfos = new();
                IGXFactory.GetInstance().UpdateAllDeviceList(1000, deviceInfos);  // 枚举相机，文档建议在打开相机前先枚举
                if (deviceInfos.Count == 0)
                {
                    throw new CGalaxyException(-3, "No device found.");
                }

                foreach (IGXDeviceInfo info in deviceInfos)
                {
                    Debug.WriteLine(info.GetModelName());
                    Debug.WriteLine(info.GetVendorName());
                }

                // 获取 Interface 信息
                List<IGXInterfaceInfo> gxInterfaceList = new();
                IGXFactory.GetInstance().GetAllInterfaceInfo(gxInterfaceList);
                foreach (IGXInterfaceInfo info in gxInterfaceList)
                {
                    Debug.WriteLine(info.GetModelName());
                    Debug.WriteLine(info.GetVendorName());
                }

                // 开启两个相机的采集任务
                var topCameraTask = Task.Run(() => GxCameraCaptureTask(CaptureTaskCameraSelect.TopCamera), gxCameraTaskCancelToken);
                var bottomCameraTask = Task.Run(() => GxCameraCaptureTask(CaptureTaskCameraSelect.BottomCamera), gxCameraTaskCancelToken);

                topCameraTask.Wait();
                bottomCameraTask.Wait();

            } catch (CGalaxyException ex)
            {
                Debug.WriteLine("Error code: " + ex.GetErrorCode().ToString());
                Debug.WriteLine("Error message: " + ex.Message);
                throw;
            } finally {
                IGXFactory.GetInstance().Uninit();
            }
        }

        /// <summary>
        /// 相机采集任务
        /// </summary>
        /// <param name="mac">相机的 MAC 地址</param>
        private void GxCameraCaptureTask(CaptureTaskCameraSelect camSelect)
        {
            const int timeout = 500; // TODO: 测试并改小这个值
            const int period = 100; // 刷新率为 10Hz
            string mac;
            switch(camSelect)
            {
                case CaptureTaskCameraSelect.TopCamera:
                    mac = TopCameraMac;
                    break;
                case CaptureTaskCameraSelect.BottomCamera:
                    mac = BottomCameraMac;
                    break;
                default:
                    Debug.WriteLine("Error in GxCameraCaptureTask: please use proper CaptureTaskCameraSelect enum");
                    return;
            }


            IGXDevice? camera = null;

            try { 
                // 打开相机
                camera = IGXFactory.GetInstance().OpenDeviceByMAC(mac, GX_ACCESS_MODE.GX_ACCESS_EXCLUSIVE);

                // 采集图像
                if (camera != null)
                {
                    UInt32 streamCount = camera.GetStreamCount();
                    if (streamCount > 0)
                    {
                        IGXStream stream = camera.OpenStream(0);
                        IGXFeatureControl featControl = camera.GetRemoteFeatureControl();
                        GX_DEVICE_CLASS_LIST deviceClass = camera.GetDeviceInfo().GetDeviceClass();

                        // 设置最优包长
                        if (GX_DEVICE_CLASS_LIST.GX_DEVICE_CLASS_GEV == deviceClass)
                        {
                            if (true == featControl.IsImplemented("GevSCPSPacketSize"))
                            {
                                UInt32 packetSize = stream.GetOptimalPacketSize();
                                featControl.GetIntFeature("GevSCPSPacketSize").SetValue(packetSize);
                            }
                        }


                        //stream.SetAcqusitionBufferNumber(10); // 设置缓存数量，在开采前设置

                        /*** 下面是一些相机配置 ***/

                        // 设置 buffer 行为（好像这个无效）
                        if (featControl.IsImplemented("StreamBufferHandlingMode"))
                        {
                            featControl.GetEnumFeature("StreamBufferHandlingMode").SetValue("NewestOnly");
                            string s = featControl.GetEnumFeature("StreamBufferHandlingMode").GetValue();
                            Debug.Assert(s == "NewestOnly");
                        } else
                        {
                            Debug.WriteLine("StreamBufferHandlingMode not supported");
                        }

                        // 设置采集模式
                        if (featControl.IsImplemented("AcquisitionMode"))
                        {
                            featControl.GetEnumFeature("AcquisitionMode").SetValue("Continuous");
                            string s = featControl.GetEnumFeature("AcquisitionMode").GetValue();
                            Debug.Assert(s == "Continuous");
                        }
                        else
                        {
                            Debug.WriteLine("AcquisitionMode not supported");
                        }

                        if (featControl.IsImplemented("TriggerSelector") && featControl.IsImplemented("TriggerMode")) {
                            featControl.GetEnumFeature("TriggerSelector").SetValue("FrameStart"); // 这个是调试软件提供的，不清楚是否必要

                            // 设置触发模式
                            if (featControl.IsImplemented("TriggerMode"))
                            {
                                featControl.GetEnumFeature("TriggerMode").SetValue("On");
                                string s = featControl.GetEnumFeature("TriggerMode").GetValue();
                                Debug.Assert(s == "On");
                            } else
                            {
                                Debug.WriteLine("TriggerMode not supported");
                            }

                            // 设置触发源
                            if (featControl.IsImplemented("TriggerSource"))
                            {
                                featControl.GetEnumFeature("TriggerSource").SetValue("Software");
                                string s = featControl.GetEnumFeature("TriggerSource").GetValue();
                                Debug.Assert(s == "Software");
                            } else
                            {
                                Debug.WriteLine("TriggerSource not supported");
                            }
                        } else
                        {
                            Debug.WriteLine("TriggerSelector not supported");
                        }

                        // 设置采集帧率调节模式：控制采集帧率是否激活
                        if (featControl.IsImplemented("AcquisitionFrameRateMode")) {
                            featControl.GetEnumFeature("AcquisitionFrameRateMode").SetValue("On");
                            string s = featControl.GetEnumFeature("AcquisitionFrameRateMode").GetValue();
                            Debug.Assert(s == "On");
                        } else
                        {
                            Debug.WriteLine("AcquisitionFrameRateMode not supported");
                        }

                        // 设置采集帧率值，当采集帧率调节模式为 On 时有效
                        if (featControl.IsImplemented("AcquisitionFrameRate"))
                        {
                            featControl.GetFloatFeature("AcquisitionFrameRate").SetValue(10.0000);
                            double d = featControl.GetFloatFeature("AcquisitionFrameRate").GetValue();
                            Debug.Assert(d == 10.0000);
                        } else
                        {
                            Debug.WriteLine("AcquisitionFrameRate not supported");
                        }

                        /*** 相机配置结束 ***/

                        stream.StartGrab();  // 开启流通道
                        featControl.GetCommandFeature("AcquisitionStart").Execute();  // 发送开采命令，必须先开启流通道

                        bool isRunningGood = true;
                        while (!gxCameraTaskCancelToken.IsCancellationRequested && isRunningGood)
                        {
                            featControl.GetCommandFeature("TriggerSoftware").Execute();
                            var frameData = stream.DQBuf(timeout);  // 零拷贝采单帧，超时 500ms
                            //var frameData = stream.GetImage(500); // 拷贝采单帧，超时 500ms
                            // 处理图像
                            //Debug.WriteLine(frameData.GetStatus());
                            UInt64 width = frameData.GetWidth();
                            UInt64 height = frameData.GetHeight();
                            Debug.Assert(width == 2592);
                            Debug.Assert(height == 2048);
                            var pixelFormat = frameData.GetPixelFormat();
                            if (pixelFormat == GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_MONO8)
                            {
                                var pRaw8Buffer = frameData.ConvertToRaw8(GX_VALID_BIT_LIST.GX_BIT_0_7);
                                var frameMat = Mat.FromPixelData((int)height, (int)width, MatType.CV_8U, pRaw8Buffer);

                                // 更新 UI 图像
                                //imgMutex.WaitOne();
                                //imageSource = frameMat.ToBitmapSource();
                                //imgMutex.ReleaseMutex();
                                Dispatcher.Invoke(() =>
                                {
                                    switch (camSelect)
                                    {
                                        case CaptureTaskCameraSelect.TopCamera:
                                            viewModel.TopImgSrcMutex.WaitOne();
                                            viewModel.TopImageSource = frameMat.ToBitmapSource();
                                            viewModel.TopImgSrcMutex.ReleaseMutex();
                                            break;
                                        case CaptureTaskCameraSelect.BottomCamera:
                                            viewModel.BottomImgSrcMutex.WaitOne();
                                            viewModel.BottomImageSource = frameMat.ToBitmapSource();
                                            viewModel.BottomImgSrcMutex.ReleaseMutex();
                                            break;
                                        default:
                                            Debug.WriteLine("Error in GxCameraCaptureTask: please use proper CaptureTaskCameraSelect enum");
                                            isRunningGood = false;
                                            break;
                                    }
                                });
                            } else
                            {
                                Debug.WriteLine("Format error!");
                            }
                            stream.QBuf(frameData);
                            //frameData.Destroy();

                            Thread.Sleep(period);
                        }

                        featControl.GetCommandFeature("AcquisitionStop").Execute();  // 发送停采命令
                        stream.StopGrab();
                        stream.Close(); // 关闭流通道
                    }
                }
            } catch (CGalaxyException ex)
            {
                Debug.WriteLine("Error code: " + ex.GetErrorCode().ToString());
                Debug.WriteLine("Error message: " + ex.Message);

                if (ex.GetErrorCode() == -8)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("打开相机失败，请确认是否被占用（可尝试重新拔出）");
                    });
                } else
                {
                    throw;
                }
            } finally
            {
                if (camera != null)
                {
                    // 关闭相机
                    camera.Close();
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A && !jogHandler.isJogging)
            {
                Debug.WriteLine("Key A pressed down.");
                Joints j = new()
                {
                    P2 = -natorJogResolution
                };
                jogHandler.StartJogging(j);
                //jogHandler.TestStartJogging();
            }
            else if (e.Key == Key.D && !jogHandler.isJogging)
            {
                Joints j = new()
                {
                    P2 = natorJogResolution
                };
                jogHandler.StartJogging(j);
                //jogHandler.TestStartJogging();
            }
            else if (e.Key == Key.W && !jogHandler.isJogging)
            {
                Joints j = new()
                {
                    P3 = natorJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.S && !jogHandler.isJogging)
            {
                Joints j = new()
                {
                    P3 = -natorJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.LeftShift && !jogHandler.isJogging)
            {
                Joints j = new()
                {
                    P4 = natorJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.LeftCtrl && !jogHandler.isJogging)
            {
                Joints j = new()
                {
                    P4 = -natorJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.Q && !jogHandler.isJogging)
            {
                Joints j = new()
                {
                    R1 = -RMDJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.R && !jogHandler.isJogging)
            {
                Joints j = new()
                {
                    R1 = RMDJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.J && !jogHandler.isJogging)
            {
                Joints j = new()
                {
                    R5 = -RMDJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.U && !jogHandler.isJogging)
            {
                Joints j = new()
                {
                    R5 = RMDJogResolution
                };
                jogHandler.StartJogging(j);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                case Key.D:
                case Key.S:
                case Key.W:
                case Key.Q:
                case Key.E:
                case Key.J:
                case Key.U:
                case Key.LeftShift:
                case Key.LeftCtrl:
                    Debug.WriteLine("Key up");
                    jogHandler.StopJogging();
                    break;
                default:
                    break;
            }
        }

        private static int SelectNatorSpeed(int level)
        {
            int value = 0;
            switch (level)
            {
                case 0:
                    value = 10000 / 1000 * controlPeriod;  // 0.01 mm/s
                    //value = 1;
                    break;
                case 1:
                    value = 100000 / 1000 * controlPeriod;  // 0.1mm/s
                    //value = 10;
                    break;
                case 2:
                    value = 1000000 / 1000 * controlPeriod;  // 1 mm/s
                    //value = 100;
                    break;
                default:
                    break;
            }

            return value;
        }

        private static int SelectRMDSpeed(int level)
        {
            int value = 0;
            switch (level)
            {
                case 0:
                    //value = (int)(50.0 / 1000.0 * (double)(controlPeriod));  // 0.01 degree/s
                    value = 1;
                    break;
                case 1:
                    //value = (int)(100.0 / 1000 * (double)controlPeriod); // 0.1 degree/s
                    value = 10;
                    break;
                case 2:
                    //value = (int)(200.0 / 1000 * controlPeriod);  // 1 degree/s
                    value = 100;
                    break;
                default:
                    break;
            }

            return value;
        }

        private static void PlaySound(int level)
        {
            switch (level)
            {
                case 0:
                    lowPlayer.Play();
                    break;
                case 1:
                    mediumPlayer.Play();
                    break;
                case 2:
                    highPlayer.Play();
                    break;
                default:
                    break;
            }
        }
    }
}
