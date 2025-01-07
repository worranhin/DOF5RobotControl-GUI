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

        public VibrateHelper(D5Robot robot, RoboticState target) {
            this.robot = robot;
            targetState = target;
        }

        ~VibrateHelper()
        {
            vibrateCancelSource?.Cancel();
        }

        // TODO: 删除这个旧函数
        public void Start()
        {
            vibrateCancelSource?.Dispose();
            vibrateCancelSource = new();
            vibrateCancelToken = vibrateCancelSource.Token;
            Task.Run(VibrateTask, vibrateCancelToken);
        }

        public void Start(bool vibrateHorizontal, bool vibrateVertical, double amplitude, double frequency)
        {
            if (vibrateHorizontal == false && vibrateVertical == false)
                throw new ArgumentException("At least one of vibrateHorizontal and vibrateVertical should be true.", nameof(vibrateVertical));
            vibrateCancelSource?.Dispose();
            vibrateCancelSource = new();
            vibrateCancelToken = vibrateCancelSource.Token;
            Task.Run(() => VibrateTask(vibrateHorizontal, vibrateVertical, amplitude, frequency), vibrateCancelToken);
        }

        public void Stop()
        {
            vibrateCancelSource?.Cancel();
        }

        // TODO: 删除这个旧函数
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
                joints.P2 += (int)(x * 100000); // 0.1 mm
                robot.JointsMoveAbsolute(joints);

                Thread.Sleep(10);
            }

            robot.JointsMoveAbsolute(targetState.ToD5RJoints());
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
            var startTime = DateTime.Now;
            while (!vibrateCancelToken.IsCancellationRequested)
            {
                var currentTime = DateTime.Now;
                var deltaTime = currentTime - startTime;
                var t = deltaTime.TotalSeconds;
                var x = amplitude * Math.Sin(2 * Math.PI * frequency * t);  // 正弦函数，频率为 frequency，幅值为正负 amplitude 单位 mm
                Debug.WriteLine($"{t}: {x}");

                Joints joints = new();
                if (vibrateHorizontal)
                    joints.P2 += (int)(x * 1000000); // 1 mm
                if (vibrateVertical)
                    joints.P4 += (int)(x * 1000000);

                robot.JointsMoveAbsolute(joints);

                vibrateCancelToken.ThrowIfCancellationRequested();

                Thread.Sleep(10);
            }

            robot.JointsMoveAbsolute(targetState.ToD5RJoints());
        }
    }
}
