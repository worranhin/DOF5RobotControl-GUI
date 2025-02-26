using DOF5RobotControl_GUI.Model;

namespace DOF5RobotControl_GUI.Services
{
    public interface IDataRecordService
    {
        void Start();
        void Stop();
        void Record(JointSpace currentJoints, JointSpace deltaJoints, CamFrame topFrame, CamFrame bottomFrame);
    }
}