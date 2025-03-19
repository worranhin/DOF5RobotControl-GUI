using Accessibility;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using D5R;
using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using GxIAPINET;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VisionLibrary;
using D5Robot = D5R.D5Robot;

namespace DOF5RobotControl_GUI.ViewModel
{
    public struct JogParams
    {
        public JogParams() { }

        public JointSelect Joint { get; set; }
        public bool IsPositive { get; set; }
        public bool IsJogTaskSpace { get; set; } = false;
    };

    internal class BottomImgMessage : RequestMessage<CamFrame> { }

    public partial class MainViewModel : ObservableObject
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

        private readonly IRobotControlService _robotControlService;
        private readonly IPopUpService _popUpService;
        private readonly ICameraControlService _cameraCtrlService;

        /***** 线程相关字段 *****/
        public Dispatcher Dispatcher { get; private set; }
        private CancellationTokenSource? insertCancelSource;
        private CancellationToken insertCancelToken;
        CancellationTokenSource? attachCancelSource;

        readonly List<CancellationTokenSource> cancelSourceList = []; // 存储所有的取消源，在 stop 时统一取消

        /***** 机器人系统相关 *****/
        [ObservableProperty]
        private bool _systemConnected = false;
        [ObservableProperty]
        private string[] _portsAvailable = [];
        [ObservableProperty]
        private string _selectedPort = "";
        [ObservableProperty]
        private RoboticState _targetState = new(0, 0, 0, 0, 0);
        [ObservableProperty]
        private RoboticState _currentState = new(0, 0, 0, 0, 0);
        [ObservableProperty]
        private bool _isInserting = false;
        [ObservableProperty]
        private bool _isAttachingJaw = false;
        [ObservableProperty]
        private bool _camMotorIsConnected = false;        

        /***** 振动相关字段/属性 *****/
        [ObservableProperty]
        private bool _isVibrating = false;
        [ObservableProperty]
        private bool _isVibrateHorizontal = true;
        [ObservableProperty]
        private bool _isVibrateVertical = false;
        [ObservableProperty]
        private double _vibrateAmplitude = 0.05;
        [ObservableProperty]
        private double _vibrateFrequency = 10.0;

        public MainViewModel(IRobotControlService robotControlService, IPopUpService popUpService, ICameraControlService cameraCtrlService)
        {
            Dispatcher = Application.Current.Dispatcher;

            // 获取服务引用
            _robotControlService = robotControlService;
            _popUpService = popUpService;
            _cameraCtrlService = cameraCtrlService;

            // 初始化 Serial
            PortsAvailable = SerialPort.GetPortNames();
            if (PortsAvailable.Length > 0)
            {
                var defaultPort = Properties.Settings.Default.Port; // 获取上次选择的 COM 口
                if (PortsAvailable.Contains(defaultPort))
                    SelectedPort = defaultPort;
                else
                    SelectedPort = PortsAvailable[0];
            }
        }

        ~MainViewModel()
        {
            insertCancelSource?.Cancel();
            attachCancelSource?.Cancel();

            foreach (var cancelSource in cancelSourceList)
            {
                cancelSource?.Cancel();
            }
        }

        /// <summary>
        /// 移动到插入初始位置
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task MoveToInitialPositionAsync()
        {
            // 为了安全，先前往便于视觉检测的位置
            TargetState.SetFromD5RJoints(PreChangeJawPos);
            _robotControlService.MoveTo(TargetState);
            using (CancellationTokenSource cancelSource = new())
            {
                cancelSourceList.Add(cancelSource);
                var token = cancelSource.Token;
                while (!token.IsCancellationRequested && TaskSpace.Distance(TargetState.TaskSpace, CurrentState.TaskSpace) > 1) // 确保已到位
                {
                    Debug.WriteLine(TaskSpace.Distance(TargetState.TaskSpace, CurrentState.TaskSpace));

                    await Task.Delay(1000);
                    UpdateCurrentState();
                }
                cancelSourceList.Remove(cancelSource);
            }

            // 下面获取图像信息
            try
            {
                UpdateCurrentState();
                var topFrame = TopCamera.Instance.LastFrame;
                var bottomFrame = BottomCamera.Instance.LastFrame;

                var (x, y, rz) = await ImageProcessor.ProcessTopImgAsync(topFrame.Buffer, topFrame.Width, topFrame.Height, topFrame.Stride, MatchingMode.ROUGH);
                double pz = await ImageProcessor.ProcessBottomImgAsync(bottomFrame.Buffer, bottomFrame.Width, bottomFrame.Height, bottomFrame.Stride);

                TaskSpace error = new() { Px = x, Py = y, Pz = pz, Ry = 0, Rz = 0 };
                Debug.WriteLine(error);

                JointSpace deltaJoint = KineHelper.InverseDifferential(error, CurrentState.TaskSpace);

                if (deltaJoint.P4 + CurrentState.JointSpace.P4 > 11)  // 对关节移动量进行安全检查
                {
                    Debug.WriteLine(deltaJoint);
                    throw new InvalidOperationException("前往初始位置时，关节移动量超出安全范围，请检查！");
                }

                TargetState.JointSpace.Copy(CurrentState.JointSpace).Add(deltaJoint); // 设置目标位置
                Debug.WriteLine(TargetState.JointSpace);
                _robotControlService.MoveTo(TargetState); // 前往目标位置
                await WaitForTargetedAsync();

                //// 前往振动开始点 ////
                using CancellationTokenSource cancelSource = new();
                cancelSourceList.Add(cancelSource);
                TargetState.Copy(CurrentState);

                while (!cancelSource.Token.IsCancellationRequested)
                {
                    const double VibratePointX = 4;
                    UpdateCurrentState();
                    topFrame = TopCamera.Instance.LastFrame;
                    (x, y, rz) = await ImageProcessor.ProcessTopImgAsync(topFrame.Buffer, topFrame.Width, topFrame.Height, topFrame.Stride, MatchingMode.FINE);
                    Debug.WriteLine($"Fine  x:{x}, y:{y}, z:{rz}");
                    if (Math.Abs(y) < 0.05)
                        break;

                    TargetState.TaskSpace.Px += x - VibratePointX;
                    TargetState.TaskSpace.Py += y;
                    TargetState.TaskSpace.Rz = 0; // 将夹钳带动钳口转正
                    _robotControlService.MoveTo(TargetState);
                    await WaitForTargetedAsync(0.01);
                }
                cancelSourceList.Remove(cancelSource);
            }
            catch (OverflowException ex)
            {
                Debug.WriteLine("Error in MoveToInitialPosition: " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 等待直到 CurrentState 与 TargetState 的距离小于一定值
        /// </summary>
        /// <param name="CheckDistance">检查距离，小于该值则返回，单位mm</param>
        /// <param name="CheckPeriod">检查周期，单位ms</param>
        /// <returns></returns>
        private async Task WaitForTargetedAsync(double CheckDistance = 0.1, int CheckPeriod = 100)
        {
            using CancellationTokenSource waitCancelSource = new();
            cancelSourceList.Add(waitCancelSource);
            var token = waitCancelSource.Token;
            while (!token.IsCancellationRequested && TaskSpace.Distance(CurrentState.TaskSpace, TargetState.TaskSpace) > CheckDistance)
            {
                await Task.Delay(CheckPeriod);
                UpdateCurrentState();
            }
            cancelSourceList.Remove(waitCancelSource);
        }

        private async Task InsertTaskAsync()
        {
            object robotMoveLock = new();

            try
            {
                IsInserting = true;
                insertCancelSource = new();
                insertCancelToken = insertCancelSource.Token;

                //// 开始振动并插入 ////
                while (!insertCancelToken.IsCancellationRequested)
                {
                    UpdateCurrentState();
                    var topFrame = TopCamera.Instance.LastFrame;
                    var (dx, dy, drz) = await ImageProcessor.ProcessTopImgAsync(topFrame.Buffer, topFrame.Width, topFrame.Height, topFrame.Stride, MatchingMode.FINE);

                    if (dx < 0.05) // 若误差小于一定值则退出循环
                        break;

                    RoboticState target = CurrentState.Clone();
                    target.TaskSpace.Px += dx;
                    double x0 = CurrentState.TaskSpace.Px;
                    double xf = target.TaskSpace.Px;
                    double tf = dx / 0.5; // seconds, 速度为 0.5 mm/s
                    double trackX(double t) => x0 + t * (xf - x0) / tf;

                    double y0 = CurrentState.TaskSpace.Py;
                    double trackY(double t) => y0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);

                    double z0 = CurrentState.TaskSpace.Pz;
                    double trackZ(double t) => z0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);

                    double t = 0;
                    TargetState.Copy(CurrentState);
                    Stopwatch sw = Stopwatch.StartNew();

                    await Task.Run(() =>
                    {
                        do
                        {
                            t = sw.ElapsedMilliseconds / 1000.0;
                            TargetState.TaskSpace.Px = trackX(t);
                            TargetState.TaskSpace.Py = trackY(t);
                            TargetState.TaskSpace.Pz = trackZ(t);
                            insertCancelToken.ThrowIfCancellationRequested();
                            _robotControlService.MoveTo(TargetState);
                        } while (t < tf);
                    });
                    sw.Stop();
                }
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine("Insertion is canceled: " + ex.Message);
            }
            finally
            {
                insertCancelSource?.Dispose();
                insertCancelSource = null;
                IsInserting = false;
            }
        }

        /***** 机器人控制命令结束 *****/

        

        /***** 处理振动相关 UI 逻辑 *****/

        [RelayCommand]
        private void ToggleVibrate()
        {
            if (!IsVibrating)
            {
                try
                {
                    _robotControlService.StartVibrate(IsVibrateHorizontal, IsVibrateVertical, VibrateAmplitude, VibrateFrequency);
                    IsVibrating = true;
                }
                catch (ArgumentException exc)
                {
                    if (exc.ParamName == "vibrateVertical")
                        _popUpService.Show(exc.Message, "Error while toggle vibration");
                    else
                        throw;
                }
            }
            else
            {
                _robotControlService.StopVibrate();
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
                case 1: break;
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
    }
}
