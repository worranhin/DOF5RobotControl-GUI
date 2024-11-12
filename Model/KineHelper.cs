using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Model
{
    /// <summary>
    /// 用于计算运动学正逆解
    /// A helper for calculating kinematics
    /// </summary>
    internal class KineHelper
    {
        const double l1 = 38.0;
        static readonly double l2 = 11.5;
        static readonly double l3 = 17.25;
        static readonly double l4 = 28.0;
        static readonly double l5 = 18.1;
        static readonly double ltx = 67.9;
        static readonly double lty = 41.5;
        static readonly double ltz = 27.75;

        public static TaskSpace Forward(JointSpace space)
        {
            var m1 = l3 + l5 + lty + space.P2;

            TaskSpace ts = new()
            {
                Px = m1 * Math.Sin(space.R1)
                    + space.P3 * Math.Cos(space.R1)
                    + ltx * Math.Cos(space.R1) * Math.Cos(space.R5)
                    + ltz * Math.Cos(space.R1) * Math.Sin(space.R5),

                Py = space.P3 * Math.Sin(space.R1)
                    - m1 * Math.Cos(space.R1)
                    + ltx * Math.Sin(space.R1) * Math.Cos(space.R5)
                    + ltz * Math.Sin(space.R1) * Math.Sin(space.R5),

                Pz = ltx * Math.Sin(space.R5) - ltz * Math.Cos(space.R5) - space.P4 - (l1 + l2 + l4),

                Ry = -space.R5,
                Rz = space.R1
            };

            return ts;
        }

        public static JointSpace Inverse(TaskSpace space)
        {
            var m1 = l3 + l5 + lty;

            JointSpace js = new()
            {
                R1 = space.Rz,
                R5 = -space.Ry,
                P2 = space.Px * Math.Sin(space.Rz) - space.Py * Math.Cos(space.Rz) - m1,
                P3 = space.Px * Math.Cos(space.Rz) + space.Py * Math.Sin(space.Rz) - ltx * Math.Cos(-space.Ry) - ltz * Math.Sin(-space.Ry),
                P4 = -space.Pz + ltx * Math.Sin(-space.Ry) - ltz * Math.Cos(-space.Ry) - (l1 + l2 + l4)
            };

            return js;
        }
    }
}
