namespace DOF5RobotControl_GUI.Services
{
    public interface ICamMotorControlService
    {
        void Connect(string port);
        void Disconnect();
        void MoveRelativeLeft(CamMotorControlService.MotorSelect id, int data);
        void MoveRelativeRight(CamMotorControlService.MotorSelect id, int data);
        void MoveStepLeft(CamMotorControlService.MotorSelect id);
        void MoveStepRight(CamMotorControlService.MotorSelect id);
    }
}