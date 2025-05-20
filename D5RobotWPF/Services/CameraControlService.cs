using DOF5RobotControl_GUI.Model;

namespace DOF5RobotControl_GUI.Services
{
    public class CameraControlService : ICameraControlService
    {
        const string TopCameraMac = "00-21-49-03-4D-95";
        const string BottomCameraMac = "00-21-49-03-4D-94";

        private readonly ICamMotorControlService _camMotorCtrlService;
        private readonly object _camOpLock = new();
        private Task libInitTask;

        public bool CameraIsOpened { get; private set; } = false;
        public bool CamMotorIsConnected { get; private set; } = false;

        public GxCamera TopCamera { get; }
        public GxCamera BottomCamera { get; }

        public CameraControlService(ICamMotorControlService camMotorCtrlService)
        {
            _camMotorCtrlService = camMotorCtrlService;

            libInitTask = Task.Run(GxCamera.GxLibInit);  // 在后台线程中调用 GxLibInit，避免死锁

            TopCamera = GxCamera.Create(TopCameraMac);
            BottomCamera = GxCamera.Create(BottomCameraMac);
        }

        ~CameraControlService()
        {
            libInitTask.Wait();
            GxCamera.GxLibUninit();
        }

        public void OpenCamera()
        {
            lock (_camOpLock)
            {
                if (!CameraIsOpened)
                {
                    TopCamera.Open(true);
                    BottomCamera.Open(true);
                    CameraIsOpened = true;
                }
            }
        }

        public void CloseCamera()
        {
            lock (_camOpLock)
            {
                if (CameraIsOpened)
                {
                    TopCamera.Close();
                    BottomCamera.Close();
                    CameraIsOpened = false;
                }
            }
        }

        /// <summary>
        /// 注册相机接收到帧的回调函数
        /// </summary>
        /// <param name="TopFrameReceivedHandler">顶部相机接收帧时的回调函数</param>
        /// <param name="BottomFrameReceivedHandler">底部相机接收帧时的回调函数</param>
        public void RegisterCallback(EventHandler<CamFrame> TopFrameReceivedHandler, EventHandler<CamFrame> BottomFrameReceivedHandler)
        {
            TopCamera.FrameReceived += TopFrameReceivedHandler;
            BottomCamera.FrameReceived += BottomFrameReceivedHandler;
        }

        public void UnRegisterCallback(EventHandler<CamFrame> TopFrameReceivedHandler, EventHandler<CamFrame> BottomFrameReceivedHandler)
        {
            TopCamera.FrameReceived -= TopFrameReceivedHandler;
            BottomCamera.FrameReceived -= BottomFrameReceivedHandler;
        }

        public CamFrame GetTopFrame()
        {
            return TopCamera.LastFrame;
        }

        public CamFrame GetBottomFrame()
        {
            return BottomCamera.LastFrame;
        }

        public void ConnectCamMotor(string port)
        {
            if (!_camMotorCtrlService.IsConnected)
            {
                _camMotorCtrlService.Connect(port);
                CamMotorIsConnected = true;
            }
        }

        public void DisconnectCamMotor()
        {
            _camMotorCtrlService.Disconnect();
            CamMotorIsConnected = false;
        }

        /// <summary>
        /// 移动顶部相机
        /// </summary>
        /// <param name="distance">移动的距离，正为向右，负为向左，单位 mm</param>
        public void MoveTopCamera(int distance)
        {
            if (distance > 0)
                _camMotorCtrlService.MoveRelativeRight(CamMotorControlService.MotorSelect.Top, distance);
            else if (distance < 0)
                _camMotorCtrlService.MoveRelativeLeft(CamMotorControlService.MotorSelect.Top, -distance);
        }

        /// <summary>
        /// 移动底部相机
        /// </summary>
        /// <param name="angle">移动的角度，正为向右，负为向左，单位 ？</param>
        public void MoveBottomCamera(int angle)
        {
            if (angle > 0)
                _camMotorCtrlService.MoveRelativeRight(CamMotorControlService.MotorSelect.Bottom, angle);
            else if (angle < 0)
                _camMotorCtrlService.MoveRelativeLeft(CamMotorControlService.MotorSelect.Bottom, -angle);
        }
    }
}
