using DOF5RobotControl_GUI.Model;

namespace DOF5RobotControl_GUI.Services
{
    public interface ICameraControlService
    {
        /// <summary>
        /// Indicate the camera is opened or not
        /// </summary>
        bool CameraIsOpened { get; }

        /// <summary>
        /// Indicate the camera motor is connected or not.
        /// </summary>
        bool CamMotorIsConnected { get; }

        void OpenCamera();
        void CloseCamera();

        /// <summary>
        /// 注册相机接收到帧的回调函数
        /// </summary>
        /// <param name="TopFrameReceivedHandler">顶部相机接收帧时的回调函数</param>
        /// <param name="BottomFrameReceivedHandler">底部相机接收帧时的回调函数</param>
        void RegisterCallback(EventHandler<CamFrame> TopFrameReceivedHandler, EventHandler<CamFrame> BottomFrameReceivedHandler);

        /// <summary>
        /// 注销相机接收到帧的回调函数
        /// </summary>
        /// <param name="TopFrameReceivedHandler">顶部相机接收帧时的回调函数</param>
        /// <param name="BottomFrameReceivedHandler">底部相机接收帧时的回调函数</param>
        void UnRegisterCallback(EventHandler<CamFrame> TopFrameReceivedHandler, EventHandler<CamFrame> BottomFrameReceivedHandler);

        CamFrame GetTopFrame();
        CamFrame GetBottomFrame();
        void ConnectCamMotor(string port);
        void DisconnectCamMotor();

        /// <summary>
        /// Move the bottom camera motor.
        /// 移动底部相机
        /// </summary>
        /// <param name="angle">移动的角度，正为向右，负为向左，单位 ？</param>
        void MoveBottomCamera(int angle);

        /// <summary>
        /// 移动顶部相机
        /// </summary>
        /// <param name="distance">移动的距离，正为向右，负为向左，单位 mm</param>
        void MoveTopCamera(int distance);
    }
}