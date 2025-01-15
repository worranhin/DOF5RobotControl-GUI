using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DOF5RobotControl_GUI.Model;
using D5R;
using System.Windows;
using System.Diagnostics;
using System.Windows.Threading;
using Opc.Ua;
using System.Windows.Input;
using System.IO.Ports;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Windows.Media.Imaging;
using VisionLibrary;
using System.Drawing;

namespace DOF5RobotControl_GUI.ViewModel
{
    public struct JogParams
    {
        public JointSelect Joint { get; set; }
        public bool IsPositive { get; set; }
    };

    partial class MainViewModel : ObservableObject
    {
        /***** 预存点位 *****/
        public static readonly Joints ZeroPos = new(0, 0, 0, 0, 0);
        public static readonly Joints IdlePos = new(0, 0, -10000000, 0, 0);
        public static readonly Joints ChangeJawPos = new(0, -1500000, 8000000, 5000000, 0); // 0, -1.5, 8, 5, 0
        public static readonly Joints PreChangeJawPos = new(0, -1500000, 0, 0, 0);
        public static readonly Joints FetchRingPos = new(0, 10000000, 10000000, 0, 0); // 0, 10, 10, 0, 0
        public static readonly Joints PreFetchRingPos = new(0, 8673000, -15000000, -10000000, 0);
        public static readonly Joints AssemblePos1 = new(0, -600000, 900000, 9000000, 0); // 0, -0.6, 0.9, 9, 0
        public static readonly Joints PreAssemblePos2 = new(9000, 0, 0, 0, 0); // 90, 0, 0, 0, 0 -> 90, 14, -12, 5, 0 
        public static readonly Joints AssemblePos2 = new(9000, 14000000, -12000000, 5000000, 0); // 90, 0, 0, 0, 0 -> 90, 14, -12, 5, 0 
        public static readonly Joints AssemblePos3 = new(0, -2500000, 4000000, 7000000, 0); // 0, -2.5, 4, 7, 0

        /***** 线程相关字段 *****/
        public Dispatcher Dispatcher { get; private set; }
        CancellationTokenSource? updateStateTaskCancelSource;
        CancellationToken updateStateTaskCancelToken;
        private CancellationTokenSource? insertCancelSource;
        private CancellationToken insertCancelToken;

        /***** 机器人系统相关 *****/
        readonly string natorId = "usb:id:2250716012";
        private D5Robot? robot;
        [ObservableProperty]
        private bool _systemConnected = false;
        [ObservableProperty]
        private string[] _portsAvailable = Array.Empty<string>();
        [ObservableProperty]
        private string _selectedPort = "";
        [ObservableProperty]
        private RoboticState _targetState = new(0, 0, 0, 0, 0);
        [ObservableProperty]
        private RoboticState _currentState = new(0, 0, 0, 0, 0);
        [ObservableProperty]
        private bool _isInserting = false;

        /***** 点动相关字段/属性 *****/
        public static IEnumerable<JogMode> JogModes => Enum.GetValues(typeof(JogMode)).Cast<JogMode>();
        public static IEnumerable<JogResolution> JogResolutions => Enum.GetValues(typeof(JogResolution)).Cast<JogResolution>();
        public readonly int natorJogResolution = 30000;
        public readonly int RMDJogResolution = 20;
        readonly uint jogPeriod = 20;  // ms
        System.Timers.Timer? jogTimer;
        [ObservableProperty]
        private JogMode _jogModeSelected = JogMode.OneStep;
        [ObservableProperty]
        private JogResolution _jogResolutionSelected = JogResolution.Speed1mm;

        /***** 振动相关字段/属性 *****/
        internal VibrateHelper? VibrateHelper;
        [ObservableProperty]
        private bool _isVibrating = false;
        [ObservableProperty]
        private bool _isVibrateHorizontal = false;
        [ObservableProperty]
        private bool _isVibrateVertical = false;
        [ObservableProperty]
        private double _vibrateAmplitude = 0.1;
        [ObservableProperty]
        private double _vibrateFrequency = 1.0;

        public MainViewModel()
        {
            Dispatcher = Application.Current.Dispatcher;

            Initialize();
        }

        public MainViewModel(Dispatcher dispatcher)
        {
            Dispatcher = dispatcher;

            Initialize();
        }

        ~MainViewModel()
        {
            updateStateTaskCancelSource?.Cancel();
        }

        private void Initialize()
        {
            // 初始化 Serial
            PortsAvailable = SerialPort.GetPortNames();
            if (PortsAvailable.Length > 0)
                SelectedPort = PortsAvailable[0];
        }

        /***** 机器人控制命令 *****/

        [RelayCommand]
        private void ToggleConnect()
        {
            if (SystemConnected)  // 如果目前系统已连接，则断开连接
            {
                robot?.Dispose();
                robot = null;
                SystemConnected = false;
                updateStateTaskCancelSource?.Cancel();
                updateStateTaskCancelSource = null;
            }
            else  // 系统未连接，则建立连接
            {
                string portName;
                if (SelectedPort.Length > 4)
                {
                    portName = "\\\\.\\" + SelectedPort;
                }
                else
                {
                    portName = SelectedPort;
                }

                try
                {
                    robot = new D5Robot(portName, natorId, 1, 2);
                    SystemConnected = true;
                    VibrateHelper = new VibrateHelper(robot, TargetState);

                    updateStateTaskCancelSource = new();
                    updateStateTaskCancelToken = updateStateTaskCancelSource.Token;
                    Task.Run(UpdateCurrentStateTask, updateStateTaskCancelToken);
                }
                catch (RobotException err)
                {
                    MessageBox.Show("Error while Connecting: " + err.Code.ToString());
                    robot?.Dispose();
                    robot = null;
                    SystemConnected = false;
                    VibrateHelper = null;
                    //throw;
                }
            }
        }

        [RelayCommand]
        private void PortRefresh()
        {
            PortsAvailable = SerialPort.GetPortNames();
        }

        [RelayCommand]
        private void SetTargetJoints(Joints joints)
        {
            TargetState.SetFromD5RJoints(joints);
        }

        [RelayCommand]
        private void SetTargetJointsFromCurrent()
        {
            var joints = CurrentState.ToD5RJoints();
            TargetState.SetFromD5RJoints(joints);
        }

        [RelayCommand]
        private void RobotRun()
        {
            if (robot == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            if (TargetState.JointSpace.HasErrors)
            {
                MessageBox.Show("Joint out of range, adjust it first.");
                return;
            }

            Joints j = TargetState.ToD5RJoints();
            try
            {
                robot.JointsMoveAbsolute(j);
            }
            catch (RobotException exc)
            {
                MessageBox.Show($"Jog error while running: {exc.Code}");
            }
        }

        [RelayCommand]
        private void RobotStop()
        {
            insertCancelSource?.Cancel();
            insertCancelSource?.Dispose();
            insertCancelSource = null;

            if (robot == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            if (!robot.Stop())
            {
                MessageBox.Show($"Error while stopping.");
                return;
            }
        }

        [RelayCommand]
        private void RobotSetZero()
        {
            if (robot == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            if (!robot.SetZero())
            {
                MessageBox.Show($"Error while setting zero.");
                return;
            }
        }

        [RelayCommand]
        private static void OpenCamera()
        {
            CameraWindow window = new();
            window.Show();
        }

        [RelayCommand]
        private static void GetImage()
        {
            ImageSource? image = WeakReferenceMessenger.Default.Send<TopImgRequestMessage>();
            Debug.WriteLine(image);
        }

        [RelayCommand]
        private async Task GoToReadyPosition()
        {
            ImageSource? topImage = null, bottomImage = null;
            try
            {
                topImage = WeakReferenceMessenger.Default.Send<TopImgRequestMessage>();
                bottomImage = WeakReferenceMessenger.Default.Send<BottomImgRequestMessage>();
            } catch (InvalidOperationException ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show("Request Image failed. Please open the camera first.", "Error when go to ready position");
            }

            if (topImage is not BitmapSource topBitmap || bottomImage is not BitmapSource bottomBitmap)
            {
                MessageBox.Show("Failed to get image, please check the camera. (You have to start the camera first.)", "Fail to Get Image");
                return;
            }

            var error = await GetErrorAsync(topBitmap, bottomBitmap);
            Debug.WriteLine(error);
        }

        [RelayCommand]
        private void ToggleInsertion()
        {
            if (!IsInserting)
            {
                insertCancelSource = new();
                insertCancelToken = insertCancelSource.Token;
                Task.Run(async () =>
                {
                    while (!insertCancelToken.IsCancellationRequested)
                    {
                        try
                        {
                            TargetState.JointSpace.P3 = CurrentState.JointSpace.P3 + 0.1;
                        }
                        catch (ArgumentOutOfRangeException exc)
                        {
                            insertCancelSource.Cancel();
                            MessageBox.Show(exc.Message, "Error while Insertion");
                            break;
                        }
                        RobotRun();
                        //Thread.Sleep(500);
                        await Task.Delay(500);
                    }
                }, insertCancelToken);
                IsInserting = true;
            }
            else  // if inserting, then cancel it
            {
                insertCancelSource?.Cancel();
                insertCancelSource?.Dispose();
                insertCancelSource = null;
                IsInserting = false;
            }
        }

        private async Task<TaskSpace> GetErrorAsync(BitmapSource topBitmap, BitmapSource bottomBitmap)
        {
            var topTask = ImageProcessor.ProcessTopImgAsync(topBitmap);
            var bottomTask = ImageProcessor.ProcessBottomImgAsync(bottomBitmap);
            await Task.WhenAll(topTask, bottomTask);
            (double px, double py, double rz) = await topTask;
            double pz = await bottomTask;
            return new TaskSpace() { Px = px, Py = py, Pz = pz, Ry = double.NaN, Rz = rz };

            //double px, py, pz, rz;
            //int width, height, stride;
            //byte[] rawBuffer;
            //VisionWrapper vision = new();

            //width = topBitmap.PixelWidth;
            //height = topBitmap.PixelHeight;
            //stride = width * ((topBitmap.Format.BitsPerPixel + 7) / 8); // 每行的字节数 ( + 7) / 8 是为了向上取整
            //rawBuffer = new byte[height * stride];
            //topBitmap.CopyPixels(rawBuffer, stride, 0);

            //var processTopImg = Task.Run(() =>
            //{
            //    TaskSpaceError error = new();
            //    GCHandle handle = GCHandle.Alloc(rawBuffer, GCHandleType.Pinned);
            //    try
            //    {
            //        IntPtr pointer = handle.AddrOfPinnedObject();
            //        error = vision.GetTaskSpaceError(pointer, width, height, stride, MatchingMode.ROUGH);
            //        Debug.WriteLine(error);
            //    }
            //    catch (Exception ex)
            //    {
            //        Debug.WriteLine("Error in VisionWrapper: " + ex.Message);
            //    }
            //    finally
            //    {
            //        handle.Free();
            //    }
            //    return (error.Px, error.Py, error.Rz);
            //});

            //width = bottomBitmap.PixelWidth;
            //height = bottomBitmap.PixelHeight;
            //stride = width * ((bottomBitmap.Format.BitsPerPixel + 7) / 8); // 每行的字节数 ( + 7) / 8 是为了向上取整
            //rawBuffer = new byte[height * stride];
            //bottomBitmap.CopyPixels(rawBuffer, stride, 0);
            //var processBottomImg = Task.Run(() =>
            //{
            //    double verticalError = 0.0;
            //    GCHandle handle = GCHandle.Alloc(rawBuffer, GCHandleType.Pinned);
            //    try
            //    {
            //        IntPtr pointer = handle.AddrOfPinnedObject();
            //        verticalError = vision.GetVerticalError(pointer, width, height, stride);
            //        Debug.WriteLine(verticalError);
            //    }
            //    catch (Exception ex)
            //    {
            //        Debug.WriteLine("Error in VisionWrapper: " + ex.Message);
            //    }
            //    finally
            //    {
            //        handle.Free();
            //    }
            //    return verticalError;
            //});

            //(px, py, rz) = await processTopImg;
            //pz = await processBottomImg;

            //return new TaskSpace() { Px = px, Py = py, Pz = pz, Ry = 0, Rz = rz };
        }

        /***** 机器人控制命令结束 *****/

        /***** Jog 相关命令 *****/

        [RelayCommand]
        private void Jog(JogParams param)
        {
            if (JogModeSelected == JogMode.OneStep)
            {
                Debug.WriteLine($"{param.Joint}, {param.IsPositive}, {JogModeSelected}, {JogResolutionSelected}");

                double resolution = 0;
                switch (JogResolutionSelected)
                {
                    case JogResolution.Speed1mm:
                        resolution = 1;
                        break;
                    case JogResolution.Speed100um:
                        resolution = 0.1;
                        break;
                    case JogResolution.Speed10um:
                        resolution = 0.01;
                        break;
                    default:
                        Debug.WriteLine("Invalid JogResolutionSelected");
                        break;
                }

                resolution = param.IsPositive ? resolution : -resolution;

                switch (param.Joint)
                {
                    case JointSelect.R1:
                        TargetState.JointSpace.R1 += resolution;
                        break;
                    case JointSelect.P2:
                        TargetState.JointSpace.P2 += resolution;
                        break;
                    case JointSelect.P3:
                        TargetState.JointSpace.P3 += resolution;
                        break;
                    case JointSelect.P4:
                        TargetState.JointSpace.P4 += resolution;
                        break;
                    case JointSelect.R5:
                        TargetState.JointSpace.R5 += resolution;
                        break;
                    default:
                        Debug.WriteLine("Invalid JointSelect");
                        break;
                }

                if (!TargetState.JointSpace.HasErrors)
                    robot?.JointsMoveAbsolute(TargetState.ToD5RJoints());
            }
        }

        public void StartJogContinuous(JogParams param)
        {
            if (JogModeSelected != JogMode.Continuous)
            {
                return;
            }

            double resolution = 0;
            switch (JogResolutionSelected)
            {
                case JogResolution.Speed1mm:
                    resolution = 1;
                    break;
                case JogResolution.Speed100um:
                    resolution = 0.1;
                    break;
                case JogResolution.Speed10um:
                    resolution = 0.01;
                    break;
                default:
                    break;
            }

            if (!param.IsPositive)
                resolution = -resolution;  // 每秒步进量

            jogTimer = new(jogPeriod);
            resolution = resolution * jogPeriod / 1000;  // 每次控制的步进量

            Action updateJointAction = () => { };
            switch (param.Joint)
            {
                case JointSelect.R1:
                    updateJointAction = () => { TargetState.JointSpace.R1 += resolution; };
                    break;
                case JointSelect.P2:
                    updateJointAction = () => { TargetState.JointSpace.P2 += resolution; };
                    break;
                case JointSelect.P3:
                    updateJointAction = () => { TargetState.JointSpace.P3 += resolution; };
                    break;
                case JointSelect.P4:
                    updateJointAction = () => { TargetState.JointSpace.P4 += resolution; };
                    break;
                case JointSelect.R5:
                    updateJointAction = () => { TargetState.JointSpace.R5 += resolution; };
                    break;
                default:
                    break;
            }

            jogTimer.Elapsed += (source, e) =>
            {
                try
                {
                    updateJointAction();
                    //RobotRun();
                    robot?.JointsMoveAbsolute(TargetState.ToD5RJoints());
                }
                catch (ArgumentException exc)
                {
                    MessageBox.Show(exc.Message);
                    jogTimer?.Stop();
                }
            };

            jogTimer.Start();
        }

        public void StopJogContinuous()
        {
            jogTimer?.Stop();
            jogTimer = null;
        }

        /***** Jog 相关结束 *****/

        /***** 处理振动相关 UI 逻辑 *****/

        [RelayCommand]
        private void ToggleVibrate()
        {
            if (VibrateHelper == null)
            {
                MessageBox.Show("VibrateHelper is null! Please Connect to robot first.", "Error while toggle vibration");
                return;
            }

            if (!IsVibrating)
            {
                try
                {
                    VibrateHelper.Start(IsVibrateHorizontal, IsVibrateVertical, VibrateAmplitude, VibrateFrequency);
                    IsVibrating = true;
                }
                catch (ArgumentException exc)
                {
                    if (exc.ParamName == "vibrateVertical")
                        MessageBox.Show(exc.Message, "Error while toggle vibration");
                    else
                        throw;
                }
            }
            else
            {
                VibrateHelper.Stop();
                IsVibrating = false;
            }
        }

        /***** 处理振动结束 *****/

        /***** OPC 相关代码 *****/

        public void OpcMapMethod(int method)
        {
            // 创建一个 EventArgs 实例来传递给 ButtonClicked 方法
            switch (method)
            {
                case 1: PortRefreshCommand.Execute(null); break;
                case 2: ToggleConnectCommand.Execute(null); break;
                case 3: SetTargetJointsCommand.Execute(ZeroPos); break;
                case 4: SetTargetJointsCommand.Execute(IdlePos); break;
                case 5: SetTargetJointsCommand.Execute(PreChangeJawPos); break;
                case 6: SetTargetJointsCommand.Execute(ChangeJawPos); break;
                case 7: SetTargetJointsCommand.Execute(AssemblePos1); break;
                case 8: SetTargetJointsCommand.Execute(AssemblePos2); break;
                case 9: SetTargetJointsCommand.Execute(AssemblePos3); break;
                case 10: SetTargetJointsCommand.Execute(PreFetchRingPos); break;
                case 11: SetTargetJointsCommand.Execute(FetchRingPos); break;
                case 12: RobotRunCommand.Execute(null); break;
                case 13: RobotStopCommand.Execute(null); break;
                case 14: RobotSetZeroCommand.Execute(null); break;
            }

            // 备份，对照
            //switch (method)
            //{
            //    case 1: PortRefresh_Click(this, args); break;
            //    case 2: BtnConnect_Click(this, args); break;
            //    case 3: BtnZeroPos_Click(this. args); break;
            //    case 4: BtnIdlePos_Click(this, args); break;
            //    case 5: BtnPreChangeJawPos_Click(this, args); break;
            //    case 6: BtnChangeJawPos_Click(this, args); break;
            //    case 7: BtnAssemblePos1_Click(this, args); break;
            //    case 8: BtnAssemblePos2_Click(this, args); break;
            //    case 9: BtnAssemblePos3_Click(this, args); break;
            //    case 10: BtnPreFetchRingPos_Click(this, args); break;
            //    case 11: BtnFetchRingPos_Click(this, args); break;
            //    case 12: BtnRun_Click(this, args); break;
            //    case 13: BtnStop_Click(this, args); break;
            //    case 14: BtnSetZero_Click(this, args); break;
            //}
        }

        /***** OPC 相关代码结束 *****/

        /***** 任务相关代码 *****/

        private void UpdateCurrentStateTask()
        {
            while (robot != null && !updateStateTaskCancelToken.IsCancellationRequested)
            {
                try
                {
                    Joints joints = (Joints)robot.GetCurrentJoint();
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            CurrentState.SetFromD5RJoints(joints);
                        }
                        catch (ArgumentException exc)
                        {
                            if (exc.ParamName == "joint")
                                Debug.WriteLine(exc.Message);
                        }
                    });
                }
                catch (RobotException exc)
                {
                    Debug.WriteLine(exc.Message);
                    if (exc.Code != ErrorCode.RMDFormatError && exc.Code != ErrorCode.SerialSendError)
                        throw;
                }
                catch (ArgumentException exc)
                {
                    Debug.WriteLine(exc.Message);
                }

                Thread.Sleep(1000);
            }
        }

        /***** 任务相关代码结束 *****/
    }
}
