using DOF5RobotControl_GUI.Model;

namespace DOF5RobotControl_GUI.Services
{
    public interface IRobotControlService
    {
        /// <summary>
        /// 指示机器人的连接状态
        /// </summary>
        bool RobotIsConnected { get; }

        /// <summary>
        /// 实时获取机器人当前状态
        /// </summary>
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

        /// <summary>
        /// 等待直到到达目标位置
        /// </summary>
        /// <param name="CheckPeriod">检查周期</param>
        /// <param name="CheckDistance">检查阈值</param>
        /// <returns></returns>
        /// 
        public Task WaitForTargetedAsync(int CheckPeriod = 100, double CheckDistance = 0.1);

        /// <summary>
        /// 等待直到到达目标位置
        /// </summary>
        /// <param name="token"></param>
        /// <param name="CheckPeriod">检查周期</param>
        /// <param name="CheckDistance">检查阈值</param>
        /// <returns></returns>
        public Task WaitForTargetedAsync(CancellationToken token, int CheckPeriod = 100, double CheckDistance = 0.1);
    }
}