using Accessibility;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using D5R;
using DOF5RobotControl_GUI.Model;
using GxIAPINET;
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

    internal class BottomImgMessage : RequestMessage<GxCamera.Frame> { }

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
        CancellationTokenSource? attachCancelSource;

        readonly List<CancellationTokenSource> cancelSourceList = []; // 存储所有的取消源，在 stop 时统一取消

        /***** 机器人系统相关 *****/
        const string natorId = "usb:id:2250716012";
        private D5Robot? robot;
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

        /***** 点动相关字段/属性 *****/
        public static IEnumerable<JogMode> JogModes => Enum.GetValues(typeof(JogMode)).Cast<JogMode>();
        public static IEnumerable<JogResolution> JogResolutions => Enum.GetValues(typeof(JogResolution)).Cast<JogResolution>();
        public const int natorJogResolution = 30000;
        public const int RMDJogResolution = 20;
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
        private bool _isVibrateHorizontal = true;
        [ObservableProperty]
        private bool _isVibrateVertical = false;
        [ObservableProperty]
        private double _vibrateAmplitude = 0.05;
        [ObservableProperty]
        private double _vibrateFrequency = 10.0;

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
            insertCancelSource?.Cancel();
            attachCancelSource?.Cancel();

            foreach (var cancelSource in cancelSourceList)
            {
                cancelSource?.Cancel();
            }

            robot?.Dispose();
        }

        private void Initialize()
        {
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
                    _ = UpdateCurrentStateTaskAsync();
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
            try
            {
                RobotMoveTo(TargetState);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error when running");
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Error when running");
            }
            catch (RobotException ex)
            {
                MessageBox.Show($"Error code: {ex.Code}\nError Message: {ex.Message}", "Robot error occurs while running");
            }
        }

        [RelayCommand]
        private void RobotStop()
        {
            // 取消异步任务
            insertCancelSource?.Cancel();
            attachCancelSource?.Cancel();

            foreach (var cancelSource in cancelSourceList)
            {
                cancelSource?.Cancel();
            }

            robot?.Stop();
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
        private async Task MoveToInitialPosition()
        {
            try
            {
                await MoveToInitialPositionAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error when go to insertion initial position");
            }
        }

        [RelayCommand]
        private async Task ToggleInsertion()
        {
            if (!IsInserting)
            {
                try
                {
                    await InsertTaskAsync();
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message, "Error When Inserting");
                }
            }
            else  // if inserting, then cancel it
            {
                insertCancelSource?.Cancel();
            }
        }

        /// <summary>
        /// 自动从零点开始装上钳口，再返回零点
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task AttachJawAsync()
        {
            IsAttachingJaw = true;
            attachCancelSource = new();
            var cancelToken = attachCancelSource.Token;

            try
            {
                try
                {
                    cancelToken.ThrowIfCancellationRequested();
                    await MoveToInitialPositionAsync(); // 前往装钳口初始位置
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    MessageBox.Show(ex.Message, "前往初始位置时出现错误");
                    attachCancelSource.Cancel();
                }

                cancelToken.ThrowIfCancellationRequested();
                await InsertTaskAsync(); // 插装钳口
                await Task.Delay(500); // 插入完成，先停一会

                // 此时应处于插入的状态，接下来将夹钳抬起来
                cancelToken.ThrowIfCancellationRequested();
                TargetState.Copy(CurrentState);
                TargetState.TaskSpace.Rz = 0; // 将 Rz 转正用于检测是否接触
                TargetState.TaskSpace.Pz += 1; // 抬起一点距离，使其与底座脱离接触
                RobotMoveTo(TargetState);
                await Task.Delay(500);

                do
                {
                    cancelToken.ThrowIfCancellationRequested();
                    TargetState.TaskSpace.Px = CurrentState.TaskSpace.Px - 1; // 向后退 1mm，避免与台子前方有挤压
                    RobotMoveTo(TargetState);
                    await WaitForTargetedAsync();
                } while (Math.Abs(CurrentState.TaskSpace.Rz) > 0.02); // 若与目标值0相差太多，则说明仍有接触，需继续后退

                cancelToken.ThrowIfCancellationRequested();
                TargetState.TaskSpace.Pz += 10;  // 向上抬一段距离，避免发生碰撞
                RobotMoveTo(TargetState);
                await WaitForTargetedAsync();

                cancelToken.ThrowIfCancellationRequested();
                TargetState.SetFromD5RJoints(ZeroPos); // 返回零点
                RobotMoveTo(TargetState);
                await WaitForTargetedAsync();
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Attach jaw task is canceled.");
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error When Attach Jaw");
                if (ex.InnerException != null)
                    throw;
            }
            finally
            {
                IsAttachingJaw = false;
                attachCancelSource.Dispose();
                attachCancelSource = null;
            }
        }

        [RelayCommand]
        private async Task DetachJawAsync()
        {
            await Task.Delay(100);
            throw new NotImplementedException();
        }

        /// <summary>
        /// 打开相机
        /// </summary>
        [RelayCommand]
        private static void OpenCamera()
        {
            CameraWindow window = new();
            window.Show();
        }

        /// <summary>
        /// 移动机器人至目标状态
        /// </summary>
        /// <param name="target"></param>
        /// <exception cref="InvalidOperationException">当机器人未连接时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">机器人关节</exception>
        private void RobotMoveTo(RoboticState target)
        {
            if (robot == null)
                throw new InvalidOperationException("Robot is not connected.");

            if (target.JointSpace.HasErrors)
                throw new ArgumentOutOfRangeException(nameof(target), "Joint value is not valid.");

            TargetState = target;
            robot.JointsMoveAbsolute(TargetState.ToD5RJoints());
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
            RobotMoveTo(TargetState);
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
                RobotMoveTo(TargetState); // 前往目标位置
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
                    RobotMoveTo(TargetState);
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
            if (VibrateHelper == null)
                throw new InvalidOperationException("VibrateHelper is null, 请检查机器人状态.");

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
                    //BitmapSource img = TopCamera.Instance.GetBitmapFrame();
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
                            RobotMoveTo(TargetState);
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


        /// <summary>
        /// 与 robot 通讯并更新 ViewModel 的 CurrentState
        /// </summary>
        private void UpdateCurrentState()
        {
            if (robot == null) throw new InvalidOperationException("Robot is not connected, please connect first.");

            Joints joints = robot.GetCurrentJoint();
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

        /***** 机器人控制命令结束 *****/

        /***** Jog 相关命令 *****/

        [RelayCommand]
        private void Jog(JogParams param)
        {
            if (JogModeSelected == JogMode.OneStep)
            {
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

                TargetState.JointSpace.Copy(CurrentState.JointSpace);

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
                    case JointSelect.Px:
                        TargetState.TaskSpace.Px += resolution;
                        break;
                    case JointSelect.Py:
                        TargetState.TaskSpace.Py += resolution;
                        break;
                    case JointSelect.Pz:
                        TargetState.TaskSpace.Pz += resolution;
                        break;
                    case JointSelect.Ry:
                        TargetState.TaskSpace.Ry += resolution;
                        break;
                    case JointSelect.Rz:
                        TargetState.TaskSpace.Rz += resolution;
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
                case JointSelect.Px:
                    updateJointAction = () => { TargetState.TaskSpace.Px += resolution; };
                    break;
                case JointSelect.Py:
                    updateJointAction = () => { TargetState.TaskSpace.Py += resolution; };
                    break;
                case JointSelect.Pz:
                    updateJointAction = () => { TargetState.TaskSpace.Pz += resolution; };
                    break;
                case JointSelect.Ry:
                    updateJointAction = () => { TargetState.TaskSpace.Ry += resolution; };
                    break;
                case JointSelect.Rz:
                    updateJointAction = () => { TargetState.TaskSpace.Rz += resolution; };
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

        /// <summary>
        /// 每隔 1s 不断更新当前关节值
        /// </summary>
        /// <returns></returns>
        private async Task UpdateCurrentStateTaskAsync()
        {
            try
            {
                while (robot != null && !updateStateTaskCancelToken.IsCancellationRequested)
                {
                    UpdateCurrentState();
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
