using DOF5RobotControl_GUI.Model;

namespace DOF5RobotControl_GUI.Services
{
    public interface IRobotControlService
    {
        /// <summary>
        /// 指示机器人的连接状态
        /// </summary>
        bool IsConnected { get; }

        JointSpace CurrentJoint { get; }

        /// <summary>
        /// 机器人的当前状态
        /// </summary>
        RoboticState CurrentState { get; }

        /// <summary>
        /// 机器人的目标状态
        /// </summary>
        RoboticState TargetState { get; }

        /// <summary>
        /// 建立连接
        /// </summary>
        /// <param name="port">RMD 电机的串口名</param>
        void Connect(string port);

        /// <summary>
        /// 断开连接
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 单关节绝对移动
        /// </summary>
        /// <param name="axis">关节轴，范围1 - 5</param>
        /// <param name="value">移动的绝对位置</param>
        void JointMoveAbsolute(int axis, double value);

        /// <summary>
        /// 单关节相对移动
        /// </summary>
        /// <param name="axis">关节轴，范围1 - 5</param>
        /// <param name="value">移动的相对距离</param>
        void JointMoveRelative(int axis, double value);

        /// <summary>
        /// 绝对移动
        /// </summary>
        /// <param name="target">目标关节值</param>
        /// <param name="checkPeriod"></param>
        /// <param name="checkDistance"></param>
        void MoveAbsolute(JointSpace target);

        /// <summary>
        /// 绝对运动
        /// </summary>
        /// <param name="target"></param>
        void MoveAbsolute(RoboticState target);

        /// <summary>
        /// 异步地绝对移动，任务将在关节到位后返回
        /// </summary>
        /// <param name="target">目标关节值</param>
        /// <param name="checkPeriod">检查周期</param>
        /// <param name="tolerance">检查距离，每个关节均小于该值则返回</param>
        /// <returns></returns>
        Task MoveAbsoluteAsync(JointSpace target, CancellationToken token, int checkPeriod = 100, double tolerance = 0.1, double angleTolerance = 0.1);

        /// <summary>
        /// 异步绝对运动
        /// </summary>
        /// <param name="target"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task MoveAbsoluteAsync(RoboticState target, CancellationToken token, int checkPeriod = 100, double tolerance = 0.1, double angleTolerance = 0.1);

        /// <summary>
        /// 相对运动
        /// </summary>
        /// <param name="diff"></param>
        void MoveRelative(JointSpace diff);

        /// <summary>
        /// 异步相对运动
        /// </summary>
        /// <param name="diff"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task MoveRelativeAsync(JointSpace diff, CancellationToken token, int checkPeriod = 100, double tolerance = 0.1, double angleTolerance = 0.1);


        /// <summary>
        /// 停止运动
        /// </summary>
        void Stop();

        /// <summary>
        /// 设置当前状态为零点
        /// </summary>
        void SetZero();

        /// <summary>
        /// 开始振动
        /// </summary>
        /// <param name="vibrateHorizontal">是否水平振动</param>
        /// <param name="vibrateVertical">是否竖直振动</param>
        /// <param name="amplitude">振幅（mm）</param>
        /// <param name="frequency">频率（Hz）</param>
        void StartVibrate(bool vibrateHorizontal, bool vibrateVertical, double amplitude, double frequency);

        /// <summary>
        /// 停止振动
        /// </summary>
        void StopVibrate();

        /// <summary>
        /// 等待直到到达目标位置
        /// </summary>
        /// <param name="CheckPeriod">检查周期</param>
        /// <param name="CheckDistance">检查阈值</param>
        /// <returns></returns>
        //public Task WaitForTargetedAsync(int CheckPeriod = 100, double CheckDistance = 0.1, double angleTolerance = 0.1);

        /// <summary>
        /// 等待直到到达目标位置
        /// </summary>
        /// <param name="token"></param>
        /// <param name="checkPeriod">检查周期</param>
        /// <param name="tolerance">检查阈值</param>
        /// <returns></returns>
        public Task WaitForTargetedAsync(CancellationToken token, int checkPeriod = 100, double tolerance = 0.1, double angleTolerance = 0.1);

        /// <summary>
        /// 获取指定关节的值
        /// </summary>
        /// <param name="axis">指定关节，范围 1-5</param>
        /// <returns>关节值，单位为 mm 或 deg</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        double GetJointValue(int axis);
    }
}