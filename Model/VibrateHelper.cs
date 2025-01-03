using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D5R;
using DOF5RobotControl_GUI.ViewModel;

namespace DOF5RobotControl_GUI.Model
{
    internal class VibrateHelper
    {
        private D5Robot robot;
        private RoboticState targetState;
        private CancellationTokenSource? vibrateCancelSource;
        private CancellationToken vibrateCancelToken;
        public VibrateHelper(D5Robot robot, RoboticState target) {  // TODO: 把 viewModel 传进来感觉还是太耦合了，日后改进一下
            this.robot = robot;
            targetState = target;
        }

        ~VibrateHelper()
        {
            vibrateCancelSource?.Cancel();
        }

        public void Start()
        {
            vibrateCancelSource = new();
            vibrateCancelToken = vibrateCancelSource.Token;
            Task.Run(VibrateTask, vibrateCancelToken);
        }

        public void Stop()
        {
            vibrateCancelSource?.Cancel();
        }

        private void VibrateTask()
        {
            var startTime = DateTime.Now;
            while (!vibrateCancelToken.IsCancellationRequested)
            {
                var currentTime = DateTime.Now;
                var deltaTime = currentTime - startTime;
                var t = deltaTime.TotalSeconds;
                var x = Math.Sin(2 * Math.PI * t);  // 周期为 1s，幅值为正负1
                Debug.WriteLine($"{t}: {x}");

                var joints = targetState.ToD5RJoints();
                joints.P2 += (int)(x * 10000); // 0.01 mm
                robot.JointsMoveAbsolute(joints);

                Thread.Sleep(10);
            }

            robot.JointsMoveAbsolute(targetState.ToD5RJoints());
        }
    }
}
