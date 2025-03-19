using DOF5RobotControl_GUI.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Services
{
    public class DummyRobotControlService : IRobotControlService
    {
        public bool RobotIsConnected { get; private set; }

        public RoboticState CurrentState { get; private set; } = new();

        public RoboticState TargetState { get; private set; } = new();

        public void Connect(string port)
        {
            Debug.WriteLine("Dummy Robot connected.");
            RobotIsConnected = true;
        }

        public void Disconnect()
        {
            Debug.WriteLine("Dummy robot disconnected.");
            RobotIsConnected = false;
        }

        public RoboticState GetCurrentState()
        {
            return CurrentState;
        }

        public void MoveRelative(RoboticState relative)
        {
            var target = CurrentState.Clone();
            target.JointSpace.Add(relative.JointSpace);
            TargetState = target;
            DummyMove();
        }

        public void MoveTo(RoboticState target)
        {
            TargetState = target;
            DummyMove();
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

        private void DummyMove()
        {
            Task.Run(() =>
            {
                while (TaskSpace.Distance(TargetState.TaskSpace, CurrentState.TaskSpace) > 0.1)
                {
                    CurrentState.JointSpace.R1 += (TargetState.JointSpace.R1 - CurrentState.JointSpace.R1) * 0.5;
                    CurrentState.JointSpace.P2 += (TargetState.JointSpace.P2 - CurrentState.JointSpace.P2) * 0.5;
                    CurrentState.JointSpace.P3 += (TargetState.JointSpace.P3 - CurrentState.JointSpace.P3) * 0.5;
                    CurrentState.JointSpace.P4 += (TargetState.JointSpace.P4 - CurrentState.JointSpace.P4) * 0.5;
                    CurrentState.JointSpace.R5 += (TargetState.JointSpace.R5 - CurrentState.JointSpace.R5) * 0.5;
                }
                CurrentState.Copy(TargetState);
            });
        }
    }
}
