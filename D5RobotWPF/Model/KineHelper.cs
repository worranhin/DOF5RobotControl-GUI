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
        /// <param name="joint">关节空间</param>
        /// <returns></returns>
        public static void Forward(JointSpace joint, TaskSpace task)
        {
            var m1 = l3 + l5 + lty + joint.P2;

            if (!CheckJoint(joint))
            {
                throw new ArgumentOutOfRangeException(nameof(joint), "关节超出限位。");
            }

            //TaskSpace ts = new()
            //{
            //    Px = m1 * Sind(joint.R1)
            //        + joint.P3 * Cosd(joint.R1)
            //        + ltx * Cosd(joint.R1) * Cosd(joint.R5)
            //        + ltz * Cosd(joint.R1) * Sind(joint.R5),

            //    Py = joint.P3 * Sind(joint.R1)
            //        - m1 * Cosd(joint.R1)
            //        + ltx * Sind(joint.R1) * Cosd(joint.R5)
            //        + ltz * Sind(joint.R1) * Sind(joint.R5),

            //    Pz = ltx * Sind(joint.R5) - ltz * Cosd(joint.R5) - joint.P4 - (l1 + l2 + l4),

            //    Ry = -joint.R5,
            //    Rz = joint.R1
            //};

            task.Px = m1 * Sind(joint.R1)
                    + joint.P3 * Cosd(joint.R1)
                    + ltx * Cosd(joint.R1) * Cosd(joint.R5)
                    + ltz * Cosd(joint.R1) * Sind(joint.R5);

            task.Py = joint.P3 * Sind(joint.R1)
                - m1 * Cosd(joint.R1)
                + ltx * Sind(joint.R1) * Cosd(joint.R5)
                + ltz * Sind(joint.R1) * Sind(joint.R5);

            task.Pz = ltx * Sind(joint.R5) - ltz * Cosd(joint.R5) - joint.P4 - (l1 + l2 + l4);

            task.Ry = -joint.R5;
            task.Rz = joint.R1;

            task.Px = Math.Round(task.Px, 2);
            task.Py = Math.Round(task.Py, 2);
            task.Pz = Math.Round(task.Pz, 2);
            task.Ry = Math.Round(task.Ry, 2);
            task.Rz = Math.Round(task.Rz, 2);

            //return task;
        }

        /// <summary>
        /// 求逆运动学
        /// </summary>
        /// <param name="task">任务空间（位姿）</param>
        /// <returns></returns>
        public static JointSpace Inverse(TaskSpace task, JointSpace joint)
        {
            var m1 = l3 + l5 + lty;

            //JointSpace js = new()
            //{
            //    R1 = task.Rz,
            //    R5 = -task.Ry,
            //    P2 = task.Px * Sind(task.Rz) - task.Py * Cosd(task.Rz) - m1,
            //    P3 = task.Px * Cosd(task.Rz) + task.Py * Sind(task.Rz) - ltx * Cosd(-task.Ry) - ltz * Sind(-task.Ry),
            //    P4 = -task.Pz + ltx * Sind(-task.Ry) - ltz * Cosd(-task.Ry) - (l1 + l2 + l4)
            //};

            joint.R1 = task.Rz;
            joint.R5 = -task.Ry;
            joint.P2 = task.Px * Sind(task.Rz) - task.Py * Cosd(task.Rz) - m1;
            joint.P3 = task.Px * Cosd(task.Rz) + task.Py * Sind(task.Rz) - ltx * Cosd(-task.Ry) - ltz * Sind(-task.Ry);
            joint.P4 = -task.Pz + ltx * Sind(-task.Ry) - ltz * Cosd(-task.Ry) - (l1 + l2 + l4);

            joint.R1 = Math.Round(joint.R1, 2);
            joint.R5 = Math.Round(joint.R5, 2);
            joint.P2 = Math.Round(joint.P2, 2);
            joint.P3 = Math.Round(joint.P3, 2);
            joint.P4 = Math.Round(joint.P4, 2);

            return joint;
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

        /// <summary>
        /// 将关节空间限制在合理范围
        /// </summary>
        /// <param name="js"></param>
        public static void ClipJoint(JointSpace js)
        {
            if (!CheckJoint(js))
            {
                js.R1 = js.R1 > _R1max ? _R1max : js.R1 < _R1min ? _R1min : js.R1;
                js.P2 = js.P2 > _P2max ? _P2max : js.P2 < _P2min ? _P2min : js.P2;
                js.P3 = js.P3 > _P3max ? _P3max : js.P3 < _P3min ? _P3min : js.P3;
                js.P4 = js.P4 > _P4max ? _P4max : js.P4 < _P4min ? _P4min : js.P4;
                js.R5 = js.R5 > _R5max ? _R5max : js.R5 < _R5min ? _R5min : js.R5;
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
