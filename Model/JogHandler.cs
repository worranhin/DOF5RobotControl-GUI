using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DOF5RobotControl_GUI.Model
{
    public class JogHandler
    {
        public enum JogMode
        {
            OneStep,
            Continuous
        };

        enum JogResolution
        {
            Slow,
            Normal,
            Fast
        };

        public bool isJogging = false;

        private readonly D5Robot robot;
        private CancellationTokenSource cancelJoggingSource;
        private CancellationToken cancelJoggingToken;

        public JogHandler(D5Robot robot)
        {
            this.robot = robot;
            cancelJoggingSource = new CancellationTokenSource();
            cancelJoggingToken = cancelJoggingSource.Token;
        }

        public void StartJogging(D5Robot.Joints joints)
        {
            isJogging = true;
            cancelJoggingSource.Cancel();
            cancelJoggingSource = new();
            cancelJoggingToken = cancelJoggingSource.Token;

            Task.Run(() =>
            {
                while (!cancelJoggingToken.IsCancellationRequested)
                {
                    var result = robot.JointsMoveRelative(joints);

                    if (result != D5Robot.ErrorCode.OK)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Jog error in JogHandler: {result}");
                        });
                        break;
                    }

                    Thread.Sleep(20);
                }

                cancelJoggingSource = new();  // initialize CancelSource
                cancelJoggingToken = cancelJoggingSource.Token;
            }, cancelJoggingToken);
        }

        public void TestStartJogging()
        {
            isJogging = true;

            Task.Run(() =>
            {
                while (!cancelJoggingToken.IsCancellationRequested)
                {
                    Debug.WriteLine("Is jogging...");
                    Thread.Sleep(500);
                }

                cancelJoggingSource = new();
                cancelJoggingToken = cancelJoggingSource.Token;
            }, cancelJoggingToken);
        }

        public void StopJogging()
        {
            cancelJoggingSource?.Cancel();
            isJogging = false;
        }
    }
}
