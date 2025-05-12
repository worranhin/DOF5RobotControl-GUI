using D5R;
using DOF5RobotControl_GUI.Model;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows;

namespace DOF5RobotControl_GUI.Services
{
    public class RobotControlService : IRobotControlService
    {
        public bool IsConnected { get; private set; } = false;

        public JointSpace CurrentJoints
        {
            get
            {
                if (rmdMotor1 == null || rmdMotor2 == null || ntMotor1 == null || ntMotor2 == null || ntMotor3 == null)
                    throw new InvalidOperationException("Robot is not initialized.");

                Joints joint = new()
                {
                    R1 = rmdMotor1.GetSingleAngle(),
                    R5 = rmdMotor2.GetSingleAngle(),
                    P2 = ntMotor1.Position,
                    P3 = ntMotor2.Position,
                    P4 = ntMotor3.Position
                };
                
                JointSpace jointSpace = new(joint);  // 将电机驱动的单位换算成 mm
                return jointSpace;
            }
        }

        public RoboticState CurrentState => new() { JointSpace = CurrentJoints };

        public RoboticState TargetState { get; private set; } = new();

        const string natorId = "usb:id:2250716012";

        private SerialPort? serial;
        private RmdMotor? rmdMotor1;
        private RmdMotor? rmdMotor2;
        private NatorsController? ntController;
        private NatorsMotor? ntMotor1;
        private NatorsMotor? ntMotor2;
        private NatorsMotor? ntMotor3;

        private CancellationTokenSource? vibrateCancelSource;

        ~RobotControlService()
        {
            Disconnect();
        }

        public void Connect(string port)
        {
            // 建立串口连接
            serial = new(port)
            {
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One
            };

            try
            {
                rmdMotor1 = new(serial, 1);
                rmdMotor2 = new(serial, 2);

                ntController = new(natorId);
                ntMotor1 = new(ntController, 1);
                ntMotor2 = new(ntController, 2);
                ntMotor3 = new(ntController, 3);
                
                IsConnected = true;
            }
            catch (RobotException ex)
            {
                IsConnected = false;
                throw new InvalidOperationException("Error while Connecting, check the port name.", ex);
            }
        }

        public void Disconnect()
        {
            rmdMotor1?.Stop();
            rmdMotor2?.Stop();
            rmdMotor1 = null;
            rmdMotor2 = null;

            ntMotor1?.Stop();
            ntMotor2?.Stop();
            ntMotor3?.Stop();
            ntMotor1 = null;
            ntMotor2 = null;
            ntMotor3 = null;

            ntController?.Dispose();
            ntController = null;

            serial?.Close();

            IsConnected = false;
        }

        /// <summary>
        /// 获取指定关节的值
        /// </summary>
        /// <param name="axis">指定关节，范围 1-5</param>
        /// <returns>关节值，单位为 mm 或 deg</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public double GetJointValue(int axis)
        {
            double value;

            switch (axis)
            {
                case 1:
                    if (rmdMotor1 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    value = rmdMotor1.GetMultiAngle() * 0.01;
                    return value;

                case 5:
                    if (rmdMotor2 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    value = -rmdMotor2.GetMultiAngle() * 0.01;
                    return value;

                case 2:
                    if (ntMotor1 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    value = -ntMotor1.Position * 1e-6;
                    return value;

                case 3:
                    if (ntMotor2 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    value = ntMotor2.Position * 1e-6;
                    return value;

                case 4:
                    if (ntMotor3 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    value = -ntMotor3.Position * 1e-6;
                    return value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), "Axis should be of 1-5");
            }
        }

        /// <summary>
        /// 移动关节到指定位置
        /// </summary>
        /// <param name="axis">指定关节，范围 1...5</param>
        /// <param name="value">关节值，单位为 mm 或 deg</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException">关节应为 1...5</exception>
        public void JointMoveAbsolute(int axis, double value)
        {
            long rmdControl;
            int ntControl;

            switch (axis)
            {
                case 1:
                    if (rmdMotor1 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    rmdControl = (long)(value * 100);
                    rmdMotor1.MultiAngleControl(rmdControl);
                    break;

                case 5:
                    if (rmdMotor2 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    rmdControl = -(long)(value * 100);
                    rmdMotor2.MultiAngleControl(rmdControl);
                    break;

                case 2:
                    if (ntMotor1 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    ntControl = -(int)(value * 1_000_000);
                    ntMotor1.MoveAbsolute(ntControl);
                    break;

                case 3:
                    if (ntMotor2 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    ntControl = (int)(value * 1_000_000);
                    ntMotor2.MoveAbsolute(ntControl);
                    break;

                case 4:
                    if (ntMotor3 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    ntControl = (int)(value * 1_000_000);
                    ntMotor3.MoveAbsolute(ntControl);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), "Axis should be of 1-5");
            }
        }

        /// <summary>
        /// 驱动关节相对运动
        /// </summary>
        /// <param name="axis">要运动的关节，范围 1...5</param>
        /// <param name="value">相对运动的关节值，单位为 mm 或 deg</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void JointMoveRelative(int axis, double value)
        {
            int rmdControl;
            int ntControl;

            switch (axis)
            {
                case 1:
                    if (rmdMotor1 == null)
                        throw new InvalidOperationException("Robot is not connected.");
                    rmdControl = (int)(value * 100);
                    rmdMotor1.IncrementalControl(rmdControl);
                    break;
                case 5:
                    if (rmdMotor2 == null)
                        throw new InvalidOperationException("Robot is not connected.");
                    rmdControl = -(int)(value * 100);
                    rmdMotor2.IncrementalControl(rmdControl);
                    break;
                case 2:
                    if (ntMotor1 == null)
                        throw new InvalidOperationException("Robot is not connected.");
                    ntControl = -(int)(value * 1_000_000);
                    ntMotor1.MoveRelative(ntControl);
                    break;
                case 3:
                    if (ntMotor2 == null)
                        throw new InvalidOperationException("Robot is not connected.");
                    ntControl = (int)(value * 1_000_000);
                    ntMotor2.MoveRelative(ntControl);
                    break;
                case 4:
                    if (ntMotor3 == null)
                        throw new InvalidOperationException("Robot is not connected.");
                    ntControl = (int)(value * 1_000_000);
                    ntMotor3.MoveRelative(ntControl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), "Axis should be of 1-5");
            }
        }

        /// <summary>
        /// 运动到指定机器人状态
        /// </summary>
        /// <param name="target"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void MoveAbsolute(RoboticState target)
        {
            if (rmdMotor1 == null || rmdMotor2 == null || ntMotor1 == null || ntMotor2 == null || ntMotor3 == null)
                throw new InvalidOperationException("Robot is not connected.");

            if (target.JointSpace.HasErrors)
                throw new ArgumentOutOfRangeException(nameof(target), "Joint value is not valid.");

            TargetState = target;
            var joint = target.JointSpace;
            JointMoveAbsolute(1, joint.R1);
            JointMoveAbsolute(2, joint.P2);
            JointMoveAbsolute(3, joint.P3);
            JointMoveAbsolute(4, joint.P4);
            JointMoveAbsolute(5, joint.R5);
        }

        public async Task MoveAbsoluteAsync(RoboticState target, CancellationToken token = default)
        {
            TargetState = target;
            var joint = target.JointSpace;
            JointMoveAbsolute(1, joint.R1);
            JointMoveAbsolute(2, joint.P2);
            JointMoveAbsolute(3, joint.P3);
            JointMoveAbsolute(4, joint.P4);
            JointMoveAbsolute(5, joint.R5);

            await WaitForTargetedAsync(token);
        }

        /// <summary>
        /// 相对运动各关节
        /// </summary>
        /// <param name="diff">相对运动的值</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void MoveRelative(JointSpace diff)
        {
            if (rmdMotor1 == null || rmdMotor2 == null || ntMotor1 == null || ntMotor2 == null || ntMotor3 == null)
                throw new InvalidOperationException("Robot is not connected.");

            if (diff.HasErrors)
                throw new ArgumentOutOfRangeException(nameof(diff), "Joint value is not valid.");

            JointMoveRelative(1, diff.R1);
            JointMoveRelative(2, diff.P2);
            JointMoveRelative(3, diff.P3);
            JointMoveRelative(4, diff.P4);
            JointMoveRelative(5, diff.R5);

            TargetState.JointSpace.Add(diff);
        }

        public async Task MoveRelativeAsync(JointSpace diff, CancellationToken token = default)
        {
            TargetState.JointSpace = CurrentJoints.Add(diff);

            JointMoveRelative(1, diff.R1);
            JointMoveRelative(2, diff.P2);
            JointMoveRelative(3, diff.P3);
            JointMoveRelative(4, diff.P4);
            JointMoveRelative(5, diff.R5);

            await WaitForTargetedAsync(token);
        }

        /// <summary>
        /// 停止运动
        /// </summary>
        public void Stop()
        {
            rmdMotor1?.Stop();
            rmdMotor2?.Stop();
            ntMotor1?.Stop();
            ntMotor2?.Stop();
            ntMotor3?.Stop();
        }

        public void SetZero()
        {
            if (rmdMotor1 == null || rmdMotor2 == null || ntMotor1 == null || ntMotor2 == null || ntMotor3 == null)
                throw new InvalidOperationException("Robot is not connected.");

            rmdMotor1.SetZero();
            rmdMotor2.SetZero();
            ntMotor1.SetPosition(0);
            ntMotor2.SetPosition(0);
            ntMotor3.SetPosition(0);
        }

        public void StartVibrate(bool vibrateHorizontal, bool vibrateVertical, double amplitude, double frequency)
        {
            if (vibrateHorizontal == false && vibrateVertical == false)
                throw new ArgumentException("At least one of vibrateHorizontal and vibrateVertical should be true.", nameof(vibrateVertical));

            Task.Run(() => VibrateTask(vibrateHorizontal, vibrateVertical, amplitude, frequency));
        }

        public void StopVibrate()
        {
            vibrateCancelSource?.Cancel();
        }

        /// <summary>
        /// 振动任务
        /// </summary>
        /// <param name="vibrateHorizontal"></param>
        /// <param name="vibrateVertical"></param>
        /// <param name="amplitude">振动的单向幅值，单位 mm</param>
        /// <param name="frequency">振动的频率</param>
        private void VibrateTask(bool vibrateHorizontal, bool vibrateVertical, double amplitude, double frequency)
        {
            vibrateCancelSource = new();
            var token = vibrateCancelSource.Token;

            var origin = CurrentJoints;
            var p2 = origin.P2;
            var p4 = origin.P4;

            Stopwatch sw = Stopwatch.StartNew();
            while (!token.IsCancellationRequested)
            {
                var t = sw.ElapsedMilliseconds / 1000.0;
                var d = amplitude * Math.Sin(2 * Math.PI * frequency * t);  // 正弦函数，频率为 frequency，幅值为正负 amplitude 单位 mm

                if (vibrateHorizontal)
                    JointMoveAbsolute(2, p2 + d);
                if (vibrateVertical)
                    JointMoveAbsolute(4, p4 + d);

                // 目前的控制周期实际是 (6ms) 左右，如果输出debug信息会更久，猜测瓶颈在于 RMD 的通讯速率（115200 baud)
            }

            // 运动到原来的位置
            MoveAbsolute(new() { JointSpace = origin });
        }

        public Task WaitForTargetedAsync(int CheckPeriod = 100, double CheckDistance = 0.1) => 
            WaitForTargetedAsync(default, CheckPeriod, CheckDistance);

        public async Task WaitForTargetedAsync(CancellationToken token, int CheckPeriod = 100, double CheckDistance = 0.1)
        {
            while (!token.IsCancellationRequested)
            {
                if (TaskSpace.Distance(CurrentState.TaskSpace, TargetState.TaskSpace) < CheckDistance)
                    break;

                try
                {
                    await Task.Delay(CheckPeriod, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}
