using DOF5RobotControl_GUI.Model;
using System.Diagnostics;

namespace DOF5RobotControl_GUI.Services
{
    public class DummyRobotControlService : IRobotControlService
    {
        public bool IsConnected { get; private set; }

        public RoboticState CurrentState { get; private set; } = new();

        public RoboticState TargetState { get; private set; } = new();

        public JointSpace CurrentJoint => throw new NotImplementedException();

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

        public RoboticState GetCurrentState()
        {
            return CurrentState;
        }

        public void MoveAbsolute(RoboticState target)
        {
            TargetState = target.Clone();
        }

        public async Task MoveAbsoluteAsync(RoboticState target, CancellationToken token = default)
        {
            MoveAbsolute(target);
            await WaitForTargetedAsync(token);
        }

        public void MoveRelative(JointSpace relative)
        {
            var target = CurrentState.Clone();
            target.JointSpace.Add(relative);
            TargetState = target;
        }

        public async Task MoveRelativeAsync(JointSpace diff, CancellationToken token = default)
        {
            MoveRelative(diff);
            await WaitForTargetedAsync(token);
        }

        public void SetZero()
        {
            CurrentState = new();
        }

        public void StartVibrate(bool vibrateHorizontal, bool vibrateVertical, double amplitude, double frequency)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            TargetState.Copy(CurrentState);
        }

        public void StopVibrate()
        {
            throw new NotImplementedException();
        }

        public async Task WaitForTargetedAsync(int CheckPeriod = 100, double CheckDistance = 0.1)
        {
            while (true)
            {
                if (TaskSpace.Distance(CurrentState.TaskSpace, TargetState.TaskSpace) < CheckDistance)
                    return;

                await Task.Delay(CheckPeriod);
            }
        }

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

        public double GetJointValue(int axis)
        {
            throw new NotImplementedException();
        }
    }
}
