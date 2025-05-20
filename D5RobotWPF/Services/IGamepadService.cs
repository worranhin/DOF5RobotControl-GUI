
namespace DOF5RobotControl_GUI.Services
{
    public interface IGamepadService
    {
        bool GamepadConnected { get; }

        event EventHandler<MoveRequestedEventArgs>? MoveRequested;
        event EventHandler<ErrorOcurredEventArgs>? ErrorOcurred;
        event EventHandler? SpeedDownRequested;
        event EventHandler? SpeedUpRequested;

        void Start();
        void Stop();
    }
}