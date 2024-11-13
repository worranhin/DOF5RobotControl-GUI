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
        const double l2 = 11.5;
        const double l3 = 17.25;
        const double l4 = 28.0;
        const double l5 = 18.1;
        const double ltx = 67.9;
        const double lty = 41.5;
        const double ltz = 27.75;

        /// <summary>
        /// 求正运动学
        /// </summary>
        /// <param name="space">关节空间</param>
        /// <returns></returns>
        public static TaskSpace Forward(JointSpace space)
        {
            var m1 = l3 + l5 + lty + space.P2;

            TaskSpace ts = new()
            {
                Px = m1 * Sind(space.R1)
                    + space.P3 * Cosd(space.R1)
                    + ltx * Cosd(space.R1) * Cosd(space.R5)
                    + ltz * Cosd(space.R1) * Sind(space.R5),

                Py = space.P3 * Sind(space.R1)
                    - m1 * Cosd(space.R1)
                    + ltx * Sind(space.R1) * Cosd(space.R5)
                    + ltz * Sind(space.R1) * Sind(space.R5),

                Pz = ltx * Sind(space.R5) - ltz * Cosd(space.R5) - space.P4 - (l1 + l2 + l4),

                Ry = -space.R5,
                Rz = space.R1
            };

            ts.Px = Math.Round(ts.Px, 2);
            ts.Py = Math.Round(ts.Py, 2);
            ts.Pz = Math.Round(ts.Pz, 2);
            ts.Ry = Math.Round(ts.Ry, 2);
            ts.Rz = Math.Round(ts.Rz, 2);

            return ts;
        }

        /// <summary>
        /// 求逆运动学
        /// </summary>
        /// <param name="space">任务空间（位姿）</param>
        /// <returns></returns>
        public static JointSpace Inverse(TaskSpace space)
        {
            var m1 = l3 + l5 + lty;

            JointSpace js = new()
            {
                R1 = space.Rz,
                R5 = -space.Ry,
                P2 = space.Px * Sind(space.Rz) - space.Py * Cosd(space.Rz) - m1,
                P3 = space.Px * Cosd(space.Rz) + space.Py * Sind(space.Rz) - ltx * Cosd(-space.Ry) - ltz * Sind(-space.Ry),
                P4 = -space.Pz + ltx * Sind(-space.Ry) - ltz * Cosd(-space.Ry) - (l1 + l2 + l4)
            };

            js.R1 = Math.Round(js.R1, 2);
            js.R5 = Math.Round(js.R5, 2);
            js.P2 = Math.Round(js.P2, 2);
            js.P3 = Math.Round(js.P3, 2);
            js.P4 = Math.Round(js.P4, 2);

            return js;
        }

        private static double Cosd(double x)
        {
            return Math.Cos(x * Math.PI / 180.0);
        }

        private static double Sind(double x)
        {
            return Math.Sin(x * Math.PI / 180.0);
        }
    }
}
