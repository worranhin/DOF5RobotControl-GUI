using D5R;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Windows.ApplicationModel.Chat;

namespace D5Robot
{
    public struct D5JointSpace
    {
        public double R1 = 0.0;
        public double P2 = 0.0;
        public double P3 = 0.0;
        public double P4 = 0.0;
        public double R5 = 0.0;

        public double R1rad
        {
            readonly get => R1 * Math.PI / 180.0;
            set => R1 = value * 180.0 / Math.PI;
        }

        public double R5rad
        {
            readonly get => R5 * Math.PI / 180.0;
            set => R5 = value * 180.0 / Math.PI;
        }

        public D5JointSpace() { }

        public override readonly string ToString()
        {
            return $"R1: {R1}, P2: {P2}, P3: {P3}, P4: {P4}, R5: {R5}";
        }

        public static D5JointSpace operator +(D5JointSpace left, D5JointSpace right)
        {
            D5JointSpace result;
            result.R1 = left.R1 + right.R1;
            result.P2 = left.P2 + right.P2;
            result.P3 = left.P3 + right.P3;
            result.P4 = left.P4 + right.P4;
            result.R5 = left.R5 + right.R5;
            return result;
        }

        public static D5JointSpace operator -(D5JointSpace left, D5JointSpace right)
        {
            D5JointSpace result;
            result.R1 = left.R1 - right.R1;
            result.P2 = left.P2 - right.P2;
            result.P3 = left.P3 - right.P3;
            result.P4 = left.P4 - right.P4;
            result.R5 = left.R5 - right.R5;
            return result;
        }

        /// <summary>
        /// 将当前关节值加上指定关节值
        /// </summary>
        /// <param name="joint"></param>
        /// <returns>原对象</returns>
        public D5JointSpace Add(D5JointSpace joint)
        {
            R1 += joint.R1;
            P2 += joint.P2;
            P3 += joint.P3;
            P4 += joint.P4;
            R5 += joint.R5;

            return this;
        }

        /// <summary>
        /// 将当前关节值减去指定关节值
        /// </summary>
        /// <param name="joint"></param>
        /// <returns>原对象</returns>
        public D5JointSpace Minus(D5JointSpace joint)
        {
            R1 -= joint.R1;
            P2 -= joint.P2;
            P3 -= joint.P3;
            P4 -= joint.P4;
            R5 -= joint.R5;

            return this;
        }

        /// <summary>
        /// 复制另一个 JointSpace 的值
        /// </summary>
        /// <param name="joint"></param>
        /// <returns>原对象</returns>
        public D5JointSpace Copy(D5JointSpace joint)
        {
            R1 = joint.R1;
            P2 = joint.P2;
            P3 = joint.P3;
            P4 = joint.P4;
            R5 = joint.R5;

            return this;
        }

        public readonly D5JointSpace Clone()
        {
            D5JointSpace joint = new();
            joint.Copy(this);
            return joint;
        }
    }
}
