using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using D5R;
using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows;
using System.Windows.Threading;
using VisionLibrary;

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
        private readonly IOpcService _opcService;
        private readonly IDataRecordService _dataRecordService;

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
        [NotifyPropertyChangedFor(nameof(TargetPx))]
        [NotifyPropertyChangedFor(nameof(TargetPy))]
        [NotifyPropertyChangedFor(nameof(TargetPz))]
        private bool _isPoseRelative = true;

        public double TargetPx
        {
            get
            {
                if (IsPoseRelative)
                    return TargetState.TaskSpace.Px - 72.90;
                else
                    return TargetState.TaskSpace.Px;
            }
            set
            {
                if (IsPoseRelative)
                    TargetState.TaskSpace.Px = value + 72.90;
                else
                    TargetState.TaskSpace.Px = value;

                OnPropertyChanged();
            }
        }

        public double TargetPy
        {
            get
            {
                if (IsPoseRelative)
                    return TargetState.TaskSpace.Py + 88.75;
                else
                    return TargetState.TaskSpace.Py;
            }
            set
            {
                if (IsPoseRelative)
                    TargetState.TaskSpace.Py = value - 88.75;
                else
                    TargetState.TaskSpace.Py = value;

                OnPropertyChanged();
            }
        }

        public double TargetPz
        {
            get
            {
                if (IsPoseRelative)
                    return TargetState.TaskSpace.Pz + 88.46;
                else
                    return TargetState.TaskSpace.Pz;
            }
            set
            {
                if (IsPoseRelative)
                    TargetState.TaskSpace.Pz = value - 88.46;
                else
                    TargetState.TaskSpace.Pz = value;

                OnPropertyChanged();
            }
        }

        [ObservableProperty]
        private bool _camMotorIsConnected = false;

        public MainViewModel(
            IRobotControlService robotControlService,
            IPopUpService popUpService,
            ICameraControlService cameraCtrlService,
            IOpcService opcService,
            IDataRecordService dataRecordService)
        {
            Dispatcher = Application.Current.Dispatcher;

            // 获取服务引用
            _robotControlService = robotControlService;
            _popUpService = popUpService;
            _cameraCtrlService = cameraCtrlService;
            _opcService = opcService;
            _dataRecordService = dataRecordService;

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
        }

        /***** OPC 相关代码结束 *****/
    }
}
