using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using D5R;

namespace DOF5RobotControl_GUI.Model
{
    public enum JogMode
    {
        OneStep,
        Continuous
    };

    public enum JogResolution
    {
        Speed1mm,
        Speed100um,
        Speed10um
    };

    public enum JointSelect
    {
        R1, P2, P3, P4, R5
    };

    public class JogHandler
    {

        public bool isJogging = false;

        private readonly D5Robot robot;
        private RoboticState targetState;
        private CancellationTokenSource cancelJoggingSource;
        private CancellationToken cancelJoggingToken;

        public JogHandler(D5Robot robot, RoboticState targetState)
        {
            this.robot = robot;
            this.targetState = targetState;
            cancelJoggingSource = new CancellationTokenSource();
            cancelJoggingToken = cancelJoggingSource.Token;
        }

        public void Start(JointSelect jointSelect)
        {
            switch (jointSelect)
            {
                case JointSelect.R1:
                    break;
                case JointSelect.P2:
                    break;
                case JointSelect.P3:
                    break;
                case JointSelect.P4:
                    break;
                case JointSelect.R5:
                    break;
                default:
                    break;
            }
        }

        public void StartJogging(Joints joints)
        {
            isJogging = true;
            cancelJoggingSource.Cancel();
            cancelJoggingSource = new();
            cancelJoggingToken = cancelJoggingSource.Token;

            Task.Run(() =>
            {
                while (!cancelJoggingToken.IsCancellationRequested)
                {
                    try
                    {
                        robot.JointsMoveRelative(joints);
                    } catch (RobotException exc)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Jog error in JogHandler: {exc.Code}");
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
