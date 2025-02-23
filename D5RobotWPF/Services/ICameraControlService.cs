using DOF5RobotControl_GUI.Model;

namespace DOF5RobotControl_GUI.Services
{
    public interface ICameraControlService
    {
        public CamFrame GetTopFrame();
        public CamFrame GetBottomFrame();
        void MoveBottomCamera(int angle);
        void MoveTopCamera(int distance);
    }
}