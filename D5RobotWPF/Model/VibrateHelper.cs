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

                Joints joints = targetState.ToD5RJoints();
                if (vibrateHorizontal)
                    joints.P2 += (int)(x * 1000000); // x mm
                if (vibrateVertical)
                    joints.P4 += (int)(x * 1000000);

                robot.JointsMoveAbsolute(joints);  // TODO: 目前的控制周期实际是 (6ms) 左右，如果输出debug信息会更久，猜测瓶颈在于 RMD 的通讯速率（115200 baud)
                Thread.Sleep(5);
            }

            robot.JointsMoveAbsolute(targetState.ToD5RJoints());
        }
    }
}
