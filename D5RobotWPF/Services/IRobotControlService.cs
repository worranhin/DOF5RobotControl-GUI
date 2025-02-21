using DOF5RobotControl_GUI.Model;

namespace DOF5RobotControl_GUI.Services
{
    public interface IRobotControlService
    {
        bool RobotIsConnected { get; }
        RoboticState CurrentState { get; }
        RoboticState TargetState { get; }

        void Connect(string port);
        void Disconnect();
        void MoveRelative(RoboticState relative);
        void MoveTo(RoboticState target);
        void Stop();
        void SetZero();
        void StartVibrate(bool vibrateHorizontal, bool vibrateVertical, double amplitude, double frequency);
        void StopVibrate();
        RoboticState GetCurrentState();
    }
}