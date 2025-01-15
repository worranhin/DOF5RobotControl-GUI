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
        public static readonly Joints PreChangeJawPos = new(0, -1200000, 8000000, 5000000, 0);
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
        const uint jogPeriod = 20;  // ms
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
                updateStateTaskCancelSource?.Dispose();
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
                    UpdateCurrentStateTaskAsync();
                    //Task.Run(UpdateCurrentStateTaskAsync, updateStateTaskCancelToken);
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
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show("Request Image failed. Please open the camera first.", "Error when go to ready position");
            }

            if (topImage is not BitmapSource topBitmap || bottomImage is not BitmapSource bottomBitmap)
            {
                MessageBox.Show("Failed to get image, please check the camera. (You have to start the camera first.)", "Fail to Get Image");
                return;
            }

            try
            {

                //var testError = await GetErrorAsync(topBitmap, bottomBitmap); // Px:-0.1309112082714844 Py:-0.03595818257717385 Pz:-3.792922143182983 Ry:NaN Rz:2.742368907737685
                //Debug.WriteLine(testError);

                //return;


                // 为了安全，先前往便于视觉检测的位置
                //TargetState.SetFromD5RJoints(PreChangeJawPos);
                //robot?.JointsMoveAbsolute(TargetState.ToD5RJoints());
                //await Task.Delay(1000);

                var error = await GetErrorAsync(topBitmap, bottomBitmap); // Px:-0.1309112082714844 Py:-0.03595818257717385 Pz:-3.792922143182983 Ry:NaN Rz:2.742368907737685
                Debug.WriteLine(error);

                //TaskSpace delta = new() { Px = error.Px, Py = error.Py, Pz = error.Pz, Ry = 0, Rz = 0 };
                JointSpace deltaJoint = KineHelper.InverseDifferential(error, CurrentState.TaskSpace);
                if (deltaJoint.P4 + CurrentState.JointSpace.P4 > 9)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("超出安全范围!请检查。");
                    });
                    return;
                }
                Debug.WriteLine(deltaJoint);

                TargetState.JointSpace.Copy(CurrentState.JointSpace);
                TargetState.JointSpace.Add(deltaJoint);
                Debug.WriteLine(TargetState.JointSpace);

                //robot?.JointsMoveAbsolute(TargetState.ToD5RJoints());
            }
            catch (OverflowException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        [RelayCommand]
        private void ToggleInsertion()
        {
            if (!IsInserting)
            {
                IsInserting = true;
                InsertTaskAsync();                
            }
            else  // if inserting, then cancel it
            {
                insertCancelSource?.Cancel();
                insertCancelSource?.Dispose();
                insertCancelSource = null;
                IsInserting = false;
            }
        }

        private async void InsertTaskAsync()
        {
            try
            {                
                insertCancelSource = new();
                insertCancelToken = insertCancelSource.Token;
                var insertTask = Task.Run(async () =>
                {
                    try
                    {
                        while (!insertCancelToken.IsCancellationRequested)
                        {
                            //TargetState.JointSpace.P3 = CurrentState.JointSpace.P3 + 0.1;
                            TargetState.TaskSpace.Px = CurrentState.TaskSpace.Px + 0.1;
                            double dRz = 0.01 * CurrentState.TaskSpace.Rz > 0.01 ? 0.01 * CurrentState.TaskSpace.Rz : 0.01;
                            TargetState.TaskSpace.Rz = CurrentState.TaskSpace.Rz + dRz;
                            RobotRun();
                            //robot?.JointsMoveAbsolute(TargetState.ToD5RJoints());
                            await Task.Delay(200);
                        }
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        insertCancelSource.Cancel();
                        IsInserting = false;
                        MessageBox.Show(ex.Message, "Error while Insertion");
                    }
                }, insertCancelToken);

                var errorTask = Task.Run(async () =>
                {
                    while (!insertCancelToken.IsCancellationRequested)
                    {
                        var bitmaps = RequestImage();
                        if (bitmaps.HasValue)
                        {
                            var (top, bottom) = bitmaps.Value;
                            var error = await GetErrorAsync(top, bottom, MatchingMode.FINE);
                            Debug.WriteLine("Task Error:" + error);
                        }
                    }
                }, insertCancelToken);

                await Task.WhenAll(insertTask, errorTask);
            }
            finally
            {
                insertCancelSource?.Dispose();
                insertCancelSource = null;
            }
        }

        // TODO: 由图像坐标系换算为机器人坐标系
        /// <summary>
        /// 异步获得误差（目标值减去当前值）
        /// </summary>
        /// <param name="topBitmap"></param>
        /// <param name="bottomBitmap"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        private async Task<TaskSpace> GetErrorAsync(BitmapSource topBitmap, BitmapSource bottomBitmap, MatchingMode mode = MatchingMode.ROUGH)
        {
            var topTask = ImageProcessor.ProcessTopImgAsync(topBitmap, mode);
            var bottomTask = ImageProcessor.ProcessBottomImgAsync(bottomBitmap);
            await Task.WhenAll(topTask, bottomTask);
            (double px, double py, double rz) = await topTask;
            double pz = await bottomTask;
            return new TaskSpace() { Px = px, Py = py, Pz = pz, Ry = 0, Rz = -rz };
        }

        private (BitmapSource topBitmap, BitmapSource bottomBitmap)? RequestImage()
        {
            ValueTuple<BitmapSource, BitmapSource>? retval = null;
            try
            {
                ImageSource? topImage = WeakReferenceMessenger.Default.Send<TopImgRequestMessage>();
                ImageSource? bottomImage = WeakReferenceMessenger.Default.Send<BottomImgRequestMessage>();

                if (topImage is BitmapSource topBitmap && bottomImage is BitmapSource bottomBitmap)
                {
                    //MessageBox.Show("Failed to get image, please check the camera. (You have to start the camera first.)", "Fail to Get Image");
                    retval = (topBitmap, bottomBitmap);
                }
                else
                {
                    MessageBox.Show("Failed to get image, please check the camera. (You have to start the camera first.)", "Fail to Get Image");
                }
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show("Request Image failed. Please open the camera first.", "Error when go to ready position");
            }

            return retval;
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

        private async void UpdateCurrentStateTaskAsync()
        {
            try
            {
                while (robot != null && !updateStateTaskCancelToken.IsCancellationRequested)
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

                    await Task.Delay(1000);
                }
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
            finally
            {
                updateStateTaskCancelSource?.Dispose();
                updateStateTaskCancelSource = null;
            }
        }

        /***** 任务相关代码结束 *****/
    }
}
