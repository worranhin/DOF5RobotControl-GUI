using DOF5RobotControl_GUI.Model;

namespace DOF5RobotControl_GUI.Services
{
    public interface IDataRecordService
    {
        void Record(JointSpace joints, CamFrame topFrame, CamFrame bottomFrame);
    }
}