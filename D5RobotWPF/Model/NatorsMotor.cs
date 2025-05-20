using D5R;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Model
{
    public class NatorsMotor(NatorsController controller, int axis)
    {
        /// <summary>
        /// 电机目前的位置，单位纳米
        /// </summary>
        public int Position => controller.GetPosition(axis);

        /// <summary>
        /// 电机当前的状态
        /// </summary>
        public uint Status => controller.GetStatus(axis);

        /// <summary>
        /// 设置电机当前位置
        /// </summary>
        /// <param name="position">设定当前的位置值，单位纳米</param>
        public void SetPosition(int position)
        {
            controller.SetPosition(axis, position);
        }

        /// <summary>
        /// 绝对移动
        /// </summary>
        /// <param name="position">移动到的绝对位置，单位纳米</param>
        public void MoveAbsolute(int position)
        {
            controller.MoveAbsolute(axis, position);
        }

        /// <summary>
        /// 相对移动
        /// </summary>
        /// <param name="diff">相对移动距离，单位纳米</param>
        public void MoveRelative(int diff)
        {
            controller.MoveRelative(axis, diff);
        }

        /// <summary>
        /// 扫描绝对移动
        /// </summary>
        /// <param name="target">绝对目标位置，范围 0...4095，0 对应 0V，4095 对应 100V</param>
        /// <param name="scanSpeed">扫描速度，范围 0...4,095,000，表示每秒执行 target 的个数（以 0...4095 为单位）</param>
        public void ScanMoveAbsolute(uint target, uint scanSpeed)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>(target, 4095, nameof(target));
            ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>(scanSpeed, 4_095_000, nameof(scanSpeed));

            controller.ScanMoveAbsolute(axis, target, scanSpeed);
        }

        /// <summary>
        /// 扫描相对移动
        /// </summary>
        /// <param name="diff">相对目标位置，范围 0...4095，0 对应 0V，4095 对应 100V</param>
        /// <param name="scanSpeed">扫描速度，范围 0...4,095,000，表示每秒执行 target 的个数（以 0...4095 为单位）</param>
        public void ScanMoveRelative(int diff, uint scanSpeed)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan<int>(diff, 4095, nameof(diff));
            ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>(scanSpeed, 4_095_000, nameof(scanSpeed));

            controller.ScanMoveRelative(axis, diff, scanSpeed);
        }

        /// <summary>
        /// 步进运动
        /// </summary>
        /// <param name="steps">运动步数，范围 -30,000...30,000，值为 0 时停止，值为 ±30,000 时持续移动</param>
        /// <param name="amplitude">幅值，范围 100...4,095，0 对应 0V，4095 对应 100V</param>
        /// <param name="frequency">频率，单位为 Hz，有效范围 1...18,500</param>
        public void StepMove(int steps, uint amplitude=2048, uint frequency=10000)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(steps, -30_000, nameof(steps));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(steps, 30_000, nameof(steps));
            ArgumentOutOfRangeException.ThrowIfLessThan(amplitude, 100u, nameof(amplitude));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(amplitude, 4095u, nameof(amplitude));
            ArgumentOutOfRangeException.ThrowIfLessThan(frequency, 1u, nameof(frequency));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(frequency, 18_500u, nameof(frequency));

            controller.StepMove(axis, steps, amplitude, frequency);
        }

        /// <summary>
        /// 停止电机运动
        /// </summary>
        public void Stop()
        {
            controller.Stop(axis);
        }

    }
}
