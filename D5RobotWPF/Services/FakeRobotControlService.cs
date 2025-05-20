using DOF5RobotControl_GUI.Model;
using System.Diagnostics;

namespace DOF5RobotControl_GUI.Services
{
    public class FakeRobotControlService : IRobotControlService
    {
        public bool IsConnected { get; private set; }

        public RoboticState CurrentState { get; private set; } = new();

        public RoboticState TargetState { get; private set; } = new();

        public JointSpace CurrentJoint => CurrentState.JointSpace.Clone();

        private CancellationTokenSource? mockRunCancelSource;
        private Task? mockRunTask;

        public void Connect(string port)
        {
            Debug.WriteLine("Dummy Robot connected.");
            IsConnected = true;

            // 模拟电机运动的线程
            mockRunCancelSource = new();
            var token = mockRunCancelSource.Token;
            mockRunTask = Task.Run(() =>
            {
                const double kp = 0.5;
                while (!token.IsCancellationRequested)
                {
                    CurrentState.JointSpace.R1 += (TargetState.JointSpace.R1 - CurrentState.JointSpace.R1) * kp;
                    CurrentState.JointSpace.P2 += (TargetState.JointSpace.P2 - CurrentState.JointSpace.P2) * kp;
                    CurrentState.JointSpace.P3 += (TargetState.JointSpace.P3 - CurrentState.JointSpace.P3) * kp;
                    CurrentState.JointSpace.P4 += (TargetState.JointSpace.P4 - CurrentState.JointSpace.P4) * kp;
                    CurrentState.JointSpace.R5 += (TargetState.JointSpace.R5 - CurrentState.JointSpace.R5) * kp;
                    Thread.Sleep(100);
                }
            });
        }

        public void Disconnect()
        {
            Debug.WriteLine("Dummy robot disconnected.");

            mockRunCancelSource?.Cancel();
            mockRunCancelSource?.Dispose();
            mockRunCancelSource = null;

            if (mockRunTask != null)
            {
                mockRunTask.Wait();
                mockRunTask = null;
            }

            IsConnected = false;
        }

        public void SetZero()
        {
            CurrentState = new();
        }

        public void Stop()
        {
            TargetState.Copy(CurrentState);
        }

        public double GetJointValue(int axis)
        {
            return axis switch
            {
                1 => CurrentState.JointSpace.R1,
                2 => CurrentState.JointSpace.P2,
                3 => CurrentState.JointSpace.P3,
                4 => CurrentState.JointSpace.P4,
                5 => CurrentState.JointSpace.R5,
                _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, "Axis should be of range 1 - 5"),
            };
        }

        public void JointMoveAbsolute(int axis, double value)
        {
            switch (axis)
            {
                case 1:
                    TargetState.JointSpace.R1 = value;
                    return;
                case 2:
                    TargetState.JointSpace.P2 = value;
                    return;
                case 3:
                    TargetState.JointSpace.P3 = value;
                    return;
                case 4:
                    TargetState.JointSpace.P4 = value;
                    return;
                case 5:
                    TargetState.JointSpace.R5 = value;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Axis should be of range 1 - 5");
            }
        }

        public void JointMoveRelative(int axis, double value)
        {
            switch (axis)
            {
                case 1:
                    TargetState.JointSpace.R1 += value;
                    return;
                case 2:
                    TargetState.JointSpace.P2 += value;
                    return;
                case 3:
                    TargetState.JointSpace.P3 += value;
                    return;
                case 4:
                    TargetState.JointSpace.P4 += value;
                    return;
                case 5:
                    TargetState.JointSpace.R5 += value;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, "Axis should be of range 1 - 5");
            }
        }

        public void MoveAbsolute(JointSpace target)
        {
            TargetState.JointSpace = target.Clone();
        }

        public void MoveAbsolute(RoboticState target)
        {
            TargetState = target.Clone();
        }

        public async Task MoveAbsoluteAsync(JointSpace target, CancellationToken token, int checkPeriod = 100, double tolerance = 0.1, double angleTolerance = 0.1)
        {
            MoveAbsolute(target);
            await WaitForTargetedAsync(token, checkPeriod, tolerance, angleTolerance);
        }

        public Task MoveAbsoluteAsync(RoboticState target, CancellationToken token, int checkPeriod = 100, double tolerance = 0.1, double angleTolerance = 0.1)
        {
            return MoveAbsoluteAsync(target.JointSpace, token, checkPeriod, tolerance, angleTolerance);
        }

        public void MoveRelative(JointSpace diff)
        {
            var target = CurrentState.Clone();
            target.JointSpace.Add(diff);
            TargetState = target;
        }

        public async Task MoveRelativeAsync(JointSpace diff, CancellationToken token, int checkPeriod = 100, double tolerance = 0.1, double angleTolerance = 0.1)
        {
            MoveRelative(diff);
            await WaitForTargetedAsync(token, checkPeriod, tolerance, angleTolerance);
        }

        public Task WaitForTargetedAsync(int CheckPeriod = 100, double CheckDistance = 0.1, double angleTolerance = 0.1) => WaitForTargetedAsync(CancellationToken.None);

        public async Task WaitForTargetedAsync(CancellationToken token, int CheckPeriod = 100, double CheckDistance = 0.1, double angleTolerance = 0.1)
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

        CancellationTokenSource? vibrateCancelSource;

        public void StartVibrate(bool vibrateHorizontal, bool vibrateVertical, double amplitude, double frequency)
        {
            vibrateCancelSource = new();
            var token = vibrateCancelSource.Token;

            VibrateAsync(vibrateHorizontal, vibrateVertical, amplitude, frequency, token).ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    Debug.WriteLine($"Vibrate task faulted: {t.Exception?.GetBaseException().Message}");
                }
            });
        }

        public void StopVibrate()
        {
            vibrateCancelSource?.Cancel();
            vibrateCancelSource?.Dispose();
            vibrateCancelSource = null;
        }

        public async Task VibrateAsync(bool vibrateHorizontal, bool vibrateVertical, double amplitude, double frequency, CancellationToken token)
        {
            var origin = CurrentJoint;
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

                //await WaitForTargetedAsync(token, 10);
                await Task.Delay(5, token);  // 必须延时一小段时间，否则 Nators 电机会产生零点漂移

                // 目前的控制周期实际是 (6ms) 左右，如果输出debug信息会更久，猜测瓶颈在于 RMD 的通讯速率（115200 baud)
            }

            // 运动到原来的位置
            if (vibrateHorizontal)
                JointMoveAbsolute(2, p2);
            if (vibrateVertical)
                JointMoveAbsolute(4, p4);
        }
    }
}
