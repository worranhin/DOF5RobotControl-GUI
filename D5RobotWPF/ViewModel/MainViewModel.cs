using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using D5R;
using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using System.Windows;
using System.Windows.Threading;

namespace DOF5RobotControl_GUI.ViewModel
{
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

        /***** 依赖服务应用 *****/
        private readonly IRobotControlService _robotControlService;
        private readonly IPopUpService _popUpService;
        private readonly ICameraControlService _cameraCtrlService;
        private readonly IOpcService _opcService;
        private readonly IDataRecordService _dataRecordService;
        private readonly IGamepadService _gamepadService;
        private readonly IProcessImageService _imageService;

        /***** 线程相关字段 *****/
        public Dispatcher Dispatcher { get; private set; }

        readonly List<CancellationTokenSource> cancelSourceList = []; // 存储所有的取消源，在 stop 时统一取消

        [ObservableProperty]
        private bool _camMotorIsConnected = false;

        public MainViewModel(
            IRobotControlService robotControlService,
            IPopUpService popUpService,
            ICameraControlService cameraCtrlService,
            IOpcService opcService,
            IDataRecordService dataRecordService,
            IGamepadService gamepadService,
            IProcessImageService processImageService)
        {
            Dispatcher = Application.Current.Dispatcher;

            // 获取服务引用
            _robotControlService = robotControlService;
            _popUpService = popUpService;
            _cameraCtrlService = cameraCtrlService;
            _opcService = opcService;
            _dataRecordService = dataRecordService;
            _gamepadService = gamepadService;
            _imageService = processImageService;
        }

        ~MainViewModel()
        {
            DisconnectSystem();
            OpcDisconnect();

            insertCancelSource?.Cancel();
            attachCancelSource?.Cancel();

            foreach (var cancelSource in cancelSourceList)
            {
                cancelSource?.Cancel();
            }
        }

        /// <summary>
        /// 打开配置窗口
        /// </summary>
        [RelayCommand]
        private static void OpenConfigWindow()
        {
            ConfigWindow window = new()
            {
                Owner = Application.Current.MainWindow
            };
            window.Show();
        }

        /// <summary>
        /// 连接/断开系统
        /// </summary>
        [RelayCommand]
        private void ToggleConnect()
        {
            // 若系统未连接，则建立连接
            if (!SystemConnected)  
                ConnectSystem();
            else  // 否则断开连接
                DisconnectSystem();
        }

        private void ConnectSystem()
        {
            try
            {
                _robotControlService.Connect(Properties.Settings.Default.RmdPort);
                _cameraCtrlService.ConnectCamMotor(Properties.Settings.Default.CamMotorPort);
                StartUpdateState();
                SystemConnected = true;
            }
            catch (InvalidOperationException ex)
            {
                _robotControlService.Disconnect();
                _cameraCtrlService.DisconnectCamMotor();
                _popUpService.Show(ex.Message);
                throw;
            }
        }

        private void DisconnectSystem()
        {
            StopUpdateState();
            _robotControlService.Disconnect();
            _cameraCtrlService.DisconnectCamMotor();
            SystemConnected = false;
        }
    }
}
