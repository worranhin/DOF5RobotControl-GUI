using D5R;
using DOF5RobotControl_GUI.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DOF5RobotControl_GUI.Services
{
    public class RobotControlService : IRobotControlService
    {
        public bool RobotIsConnected { get; private set; } = false;
        public RoboticState CurrentState { get; private set; } = new();
        public RoboticState TargetState { get; private set; } = new();

        const string natorId = "usb:id:2250716012";
        private D5Robot? robot;
        private CancellationTokenSource? vibrateCancelSource;


        public RobotControlService() { }

        ~RobotControlService()
        {
            Disconnect();
        }

        public void Connect(string port)
        {
            string portName;
            if (port.Length > 4)
                portName = "\\\\.\\" + port;
            else
                portName = port;

            try
            {
                robot = new D5Robot(portName, natorId, 1, 2);
                RobotIsConnected = true;
            }
            catch (RobotException exc)
            {
                robot?.Dispose();
                robot = null;
                RobotIsConnected = false;
                throw new InvalidOperationException("Error while Connecting, check the port name.", exc);
            }
        }

        public void Disconnect()
        {
            robot?.Dispose();
            robot = null;
            RobotIsConnected = false;
        }

        public void MoveTo(RoboticState target)
        {
            if (robot == null)
                throw new InvalidOperationException("Robot is not connected.");

            if (target.JointSpace.HasErrors)
                throw new ArgumentOutOfRangeException(nameof(target), "Joint value is not valid.");

            TargetState = target;
            robot.JointsMoveAbsolute(TargetState.ToD5RJoints());
        }

        public void MoveRelative(RoboticState relative)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            robot?.Stop();
        }

        public void SetZero()
        {
            if (robot == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            if (!robot.SetZero())
            {
                MessageBox.Show($"Error while setting zero.");
                return;
            }
        }

        public void StartVibrate(bool vibrateHorizontal, bool vibrateVertical, double amplitude, double frequency)
        {
            if (vibrateHorizontal == false && vibrateVertical == false)
                throw new ArgumentException("At least one of vibrateHorizontal and vibrateVertical should be true.", nameof(vibrateVertical));
            Task.Run(() => VibrateTask(vibrateHorizontal, vibrateVertical, amplitude, frequency));
        }

        public void StopVibrate()
        {
            vibrateCancelSource?.Cancel();
        }

        /// <summary>
        /// 获取当前的状态
        /// </summary>
        /// <returns></returns>
        public RoboticState GetCurrentState()
        {
            UpdateCurrentState();
            return CurrentState;
        }

        /// <summary>
        /// 获取当前状态
        /// </summary>
        /// <exception cref="InvalidOperationException">Throw when robot is not connected.</exception>
        private void UpdateCurrentState()
        {
            if (robot == null) throw new InvalidOperationException("Robot is not connected, please connect first.");

            Joints joints = robot.GetCurrentJoint();
            CurrentState.SetFromD5RJoints(joints);
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
            if (robot == null)
                throw new InvalidOperationException("机器人未初始化");

            using CancellationTokenSource cancelSource = new();
            vibrateCancelSource = cancelSource;

            Stopwatch sw = Stopwatch.StartNew();
            while (!cancelSource.Token.IsCancellationRequested)
            {
                var t = sw.ElapsedMilliseconds / 1000.0;
                var x = amplitude * Math.Sin(2 * Math.PI * frequency * t);  // 正弦函数，频率为 frequency，幅值为正负 amplitude 单位 mm

                Joints joints = TargetState.ToD5RJoints();
                if (vibrateHorizontal)
                    joints.P2 += (int)(x * 1000000); // x mm
                if (vibrateVertical)
                    joints.P4 += (int)(x * 1000000);

                robot.JointsMoveAbsolute(joints);  // TODO: 目前的控制周期实际是 (6ms) 左右，如果输出debug信息会更久，猜测瓶颈在于 RMD 的通讯速率（115200 baud)
            }

            robot.JointsMoveAbsolute(TargetState.ToD5RJoints());
            vibrateCancelSource = null; // 这里 using 语句会自动释放，将字段设为 null 避免重复释放
        }

        
    }
}
