using D5R;
using System.IO.Ports;

namespace D5Robot
{
    public class D5Robot
    {
        public bool IsConnected = false;

        public D5State CurrentState => new()
        {
            JointSpace = new()
            {
                R1 = GetJointValue(1),
                P2 = GetJointValue(2),
                P3 = GetJointValue(3),
                P4 = GetJointValue(4),
                R5 = GetJointValue(5)
            }
        };

        private D5State _targetState;
        public D5State TargetState => _targetState;

        const string NatorId = "usb:id:2250716012";

        private readonly SerialPort serial;
        private readonly NatorsController ntController;
        private readonly RmdMotor jointRmd1;
        private readonly NatorsMotor jointNt2;
        private readonly NatorsMotor jointNt3;
        private readonly NatorsMotor jointNt4;
        private readonly RmdMotor jointRmd5;

        public D5Robot(string port)
        {

            serial = new(port)
            {
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            jointRmd1 = new(serial, 1);
            jointRmd5 = new(serial, 2);

            ntController = new(NatorId);
            jointNt2 = new(ntController, 1);
            jointNt3 = new(ntController, 2);
            jointNt4 = new(ntController, 3);

            _targetState = CurrentState;
        }

        ~D5Robot()
        {
            ntController.Dispose();
            serial.Close();
            serial.Dispose();
        }

        /// <summary>
        /// 指示当前运动是否完成
        /// </summary>
        public bool IsMoveDone
        {
            get
            {
                const double tolerance = 0.1;
                const double angleTolerance = 0.1;

                var current = CurrentState.JointSpace;
                var target = TargetState.JointSpace;
                return Math.Abs(current.R1 - target.R1) < angleTolerance
                    && Math.Abs(current.P2 - target.P2) < tolerance
                    && Math.Abs(current.P3 - target.P3) < tolerance
                    && Math.Abs(current.P4 - target.P4) < tolerance
                    && Math.Abs(current.R5 - target.R5) < angleTolerance;
            }
        }

        /// <summary>
        /// 获取指定关节的值
        /// </summary>
        /// <param name="axis">指定关节，范围 1-5</param>
        /// <returns>关节值，单位为 mm 或 deg</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public double GetJointValue(int axis)
        {
            double value;

            switch (axis)
            {
                case 1:
                    if (jointRmd1 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    value = jointRmd1.GetMultiAngle() * 0.01;
                    return value;

                case 5:
                    if (jointRmd5 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    value = -jointRmd5.GetMultiAngle() * 0.01;
                    return value;

                case 2:
                    if (jointNt2 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    value = -jointNt2.Position * 1e-6;
                    return value;

                case 3:
                    if (jointNt3 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    value = jointNt3.Position * 1e-6;
                    return value;

                case 4:
                    if (jointNt4 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    value = jointNt4.Position * 1e-6;
                    return value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), "Axis should be of 1-5");
            }
        }

        /// <summary>
        /// 移动关节到指定位置
        /// </summary>
        /// <param name="axis">指定关节，范围 1...5</param>
        /// <param name="value">关节值，单位为 mm 或 deg</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException">关节应为 1...5</exception>
        public void JointMoveAbsolute(int axis, double value)
        {
            long rmdControl;
            int ntControl;

            switch (axis)
            {
                case 1:
                    if (jointRmd1 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    _targetState.JointSpace.R1 = value;
                    rmdControl = (long)(value * 100);
                    jointRmd1.MultiAngleControl(rmdControl);
                    break;

                case 5:
                    if (jointRmd5 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    _targetState.JointSpace.R5 = value;
                    rmdControl = -(long)(value * 100);
                    jointRmd5.MultiAngleControl(rmdControl);
                    break;

                case 2:
                    if (jointNt2 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    _targetState.JointSpace.P2 = value;
                    ntControl = -(int)(value * 1_000_000);
                    jointNt2.MoveAbsolute(ntControl);
                    break;

                case 3:
                    if (jointNt3 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    _targetState.JointSpace.P3 = value;
                    ntControl = (int)(value * 1_000_000);
                    jointNt3.MoveAbsolute(ntControl);
                    break;

                case 4:
                    if (jointNt4 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    _targetState.JointSpace.P4 = value;
                    ntControl = (int)(value * 1_000_000);
                    jointNt4.MoveAbsolute(ntControl);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), "Axis should be of 1-5");
            }
        }

        /// <summary>
        /// 驱动关节相对运动
        /// </summary>
        /// <param name="axis">要运动的关节，范围 1...5</param>
        /// <param name="value">相对运动的关节值，单位为 mm 或 deg</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void JointMoveRelative(int axis, double value)
        {
            int rmdControl;
            int ntControl;

            switch (axis)
            {
                case 1:
                    if (jointRmd1 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    _targetState.JointSpace.R1 += value;
                    rmdControl = (int)(value * 100);
                    jointRmd1.IncrementalControl(rmdControl);
                    break;
                case 5:
                    if (jointRmd5 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    _targetState.JointSpace.R5 += value;
                    rmdControl = -(int)(value * 100);
                    jointRmd5.IncrementalControl(rmdControl);
                    break;
                case 2:
                    if (jointNt2 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    _targetState.JointSpace.P2 += value;
                    ntControl = -(int)(value * 1_000_000);
                    jointNt2.MoveRelative(ntControl);
                    break;
                case 3:
                    if (jointNt3 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    _targetState.JointSpace.P3 += value;
                    ntControl = (int)(value * 1_000_000);
                    jointNt3.MoveRelative(ntControl);
                    break;
                case 4:
                    if (jointNt4 == null)
                        throw new InvalidOperationException("Robot is not connected.");

                    _targetState.JointSpace.P4 += value;
                    ntControl = (int)(value * 1_000_000);
                    jointNt4.MoveRelative(ntControl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), "Axis should be of 1-5");
            }
        }

        /// <summary>
        /// 绝对移动
        /// </summary>
        /// <param name="target"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void MoveAbsolute(D5State target)
        {

            JointMoveAbsolute(1, target.JointSpace.R1);
            JointMoveAbsolute(2, target.JointSpace.P2);
            JointMoveAbsolute(3, target.JointSpace.P3);
            JointMoveAbsolute(4, target.JointSpace.P4);
            JointMoveAbsolute(5, target.JointSpace.R5);
        }

        /// <summary>
        /// 相对运动各关节
        /// </summary>
        /// <param name="diff">相对运动的值</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void MoveRelative(D5JointSpace diff)
        {
            JointMoveRelative(1, diff.R1);
            JointMoveRelative(2, diff.P2);
            JointMoveRelative(3, diff.P3);
            JointMoveRelative(4, diff.P4);
            JointMoveRelative(5, diff.R5);
        }

        /// <summary>
        /// 停止运动
        /// </summary>
        public void Stop()
        {
            jointRmd1.Stop();
            jointRmd5.Stop();
            jointNt2.Stop();
            jointNt3.Stop();
            jointNt4.Stop();

            _targetState = CurrentState;
        }

        /// <summary>
        /// 将当前位置设为零点
        /// </summary>
        public void SetZero()
        {
            jointRmd1.SetZero();
            jointRmd5.SetZero();
            jointNt2.SetPosition(0);
            jointNt3.SetPosition(0);
            jointNt4.SetPosition(0);

            _targetState = CurrentState;
        }
    }
}
