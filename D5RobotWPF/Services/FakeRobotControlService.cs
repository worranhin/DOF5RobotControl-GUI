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
        
        public void StartVibrate(bool vibrateHorizontal, bool vibrateVertical, double amplitude, double frequency)
        {
            throw new NotImplementedException();
        }

        public void StopVibrate()
        {
            throw new NotImplementedException();
        }
    }
}
