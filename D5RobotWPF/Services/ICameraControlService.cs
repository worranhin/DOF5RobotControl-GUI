using DOF5RobotControl_GUI.Model;

namespace DOF5RobotControl_GUI.Services
{
    public interface ICameraControlService
    {
        bool CameraIsOpened { get; }
        bool CamMotorIsConnected { get; }

        void OpenCamera();
        void CloseCamera();
        void RegisterCallback(EventHandler<CamFrame> TopFrameReceivedHandler, EventHandler<CamFrame> BottomFrameReceivedHandler);
        void UnRegisterCallback(EventHandler<CamFrame> TopFrameReceivedHandler, EventHandler<CamFrame> BottomFrameReceivedHandler);
        CamFrame GetTopFrame();
        CamFrame GetBottomFrame();
        void ConnectCamMotor(string port);
        void DisconnectCamMotor();
        void MoveBottomCamera(int angle);
        void MoveTopCamera(int distance);
    }
}