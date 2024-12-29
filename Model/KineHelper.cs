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
        // 机械臂参数 updated on 2024-11-13
        const double l1 = 38.0;
        const double l2 = 11.5 + 1.5;
        const double l3 = 17.25;
        const double l4 = 28.0;
        const double l5 = 29.0;
        const double ltx = 72.9;
        const double lty = 42.5;
        const double ltz = 9.46;

        // 关节限位
        const double _R1min = -90.0, _R1max = 90.0;
        const double _R5min = -45.0, _R5max = 90.0;
        const double _P2min = -14.5, _P2max = 14.5;
        const double _P3min = -14.5, _P3max = 14.5;
        const double _P4min = -14.5, _P4max = 14.5;

        /// <summary>
        /// 求正运动学
        /// </summary>
        /// <param name="space">关节空间</param>
        /// <returns></returns>
        public static TaskSpace Forward(JointSpace space)
        {
            var m1 = l3 + l5 + lty + space.P2;

            if(!CheckJoint(space)) {
                throw new ArgumentOutOfRangeException(nameof(space), "关节超出限位。");
            }

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

        /// <summary>
        /// 检查关节限位
        /// </summary>
        /// <param name="js">关节空间</param>
        /// <returns>true 如果符合要求，false 如果不符合要求</returns>
        public static bool CheckJoint(JointSpace js)
        {
            bool good1 = js.R1 >= _R1min && js.R1 <= _R1max;
            bool good2 = js.P2 >= _P2min && js.P2 <= _P2max;
            bool good3 = js.P3 >= _P3min && js.P3 <= _P3max;
            bool good4 = js.P4 >= _P4min && js.P4 <= _P4max;
            bool good5 = js.R5 >= _R5min && js.R5 <= _R5max;

            return good1 && good2 && good3 && good4 && good5;
        }

        public static JointSpace ClipJoint(JointSpace js)
        {
            if (CheckJoint(js))
            {
                return new JointSpace()
                {
                    R1 = js.R1,
                    P2 = js.P2,
                    P3 = js.P3,
                    P4 = js.P4,
                    R5 = js.R5
                };
            }
            else
            {
                return new JointSpace()
                {
                    R1 = js.R1 > _R1max ? _R1max : js.R1 < _R1min ? _R1min : js.R1,
                    P2 = js.P2 > _P2max ? _P2max : js.P2 < _P2min ? _P2min : js.P2,
                    P3 = js.P3 > _P3max ? _P3max : js.P3 < _P3min ? _P3min : js.P3,
                    P4 = js.P4 > _P4max ? _P4max : js.P4 < _P4min ? _P4min : js.P4,
                    R5 = js.R5 > _R5max ? _R5max : js.R5 < _R5min ? _R5min : js.R5
                };
            }
        }

        /// <summary>
        /// 检查关节限位
        /// </summary>
        /// <param name="js">关节空间</param>
        /// <param name="which">哪个关节不符合要求</param>
        /// <returns>true 如果符合要求，false 如果不符合要求</returns>
        public static bool CheckJoint(JointSpace js, out bool[] which)
        {
            bool good1 = js.R1 >= _R1min && js.R1 <= _R1max;
            bool good2 = js.P2 >= _P2min && js.P2 <= _P2max;
            bool good3 = js.P3 >= _P3min && js.P3 <= _P3max;
            bool good4 = js.P4 >= _P4min && js.P4 <= _P4max;
            bool good5 = js.R5 >= _R5min && js.R5 <= _R5max;

            which = new bool[5];
            which[0] = good1;
            which[1] = good2;
            which[2] = good3;
            which[3] = good4;
            which[4] = good5;

            return good1 && good2 && good3 && good4 && good5;
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
