using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.DesignModel
{
    class DRobotControlService : IRobotControlService
    {
        public bool IsConnected => false;

        public RoboticState CurrentState { get; } = new();

        public RoboticState TargetState { get; } = new();

        public void Connect(string port)
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public RoboticState GetCurrentState()
        {
            throw new NotImplementedException();
        }

        public double GetJointValue(int axis)
        {
            throw new NotImplementedException();
        }

        public void MoveAbsolute(RoboticState target)
        {
            throw new NotImplementedException();
        }

        public Task MoveAbsoluteAsync(RoboticState target, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public void MoveRelative(JointSpace relative)
        {
            throw new NotImplementedException();
        }

        public Task MoveRelativeAsync(JointSpace diff, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public void SetZero()
        {
            throw new NotImplementedException();
        }

        public void StartVibrate(bool vibrateHorizontal, bool vibrateVertical, double amplitude, double frequency)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void StopVibrate()
        {
            throw new NotImplementedException();
        }

        public Task WaitForTargetedAsync(int CheckPeriod = 100, double CheckDistance = 0.1)
        {
            throw new NotImplementedException();
        }

        public Task WaitForTargetedAsync(CancellationToken token, int CheckPeriod = 100, double CheckDistance = 0.1)
        {
            throw new NotImplementedException();
        }
    }
}
