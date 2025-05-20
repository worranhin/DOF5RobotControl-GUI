namespace DOF5RobotControl_GUI.Model
{
    /// <summary>
    /// 用于计算运动学正逆解
    /// A helper for calculating kinematics
    /// </summary>
    public class KineHelper
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
        const double _P2min = -15.0, _P2max = 15.0;
        const double _P3min = -15.0, _P3max = 15.0;
        const double _P4min = -15.0, _P4max = 15.0;

        /// <summary>
        /// 求正运动学
        /// </summary>
        /// <param name="joint">关节空间</param>
        /// <returns></returns>
        public static void Forward(JointSpace joint, TaskSpace task)
        {
            var m1 = l3 + l5 + lty;
            double px, py, pz, ry, rz;

            //if (!CheckJoint(joint)) // TODO: 在别的地方处理这个限位，因为 CurrentState 也会调用这个，所以在这里不合适
            //{
            //    throw new ArgumentOutOfRangeException(nameof(joint), "关节超出限位。");
            //}

            px = (m1 + joint.P2) * Sind(joint.R1)
                    + joint.P3 * Cosd(joint.R1)
                    + ltx * Cosd(joint.R1) * Cosd(joint.R5)
                    + ltz * Cosd(joint.R1) * Sind(joint.R5);

            py = joint.P3 * Sind(joint.R1)
                - (m1 + joint.P2) * Cosd(joint.R1)
                + ltx * Sind(joint.R1) * Cosd(joint.R5)
                + ltz * Sind(joint.R1) * Sind(joint.R5);

            pz = ltx * Sind(joint.R5) - ltz * Cosd(joint.R5) - joint.P4 - (l1 + l2 + l4);

            ry = -joint.R5;
            rz = joint.R1;

            task.Px = Math.Round(px, 4);
            task.Py = Math.Round(py, 4);
            task.Pz = Math.Round(pz, 4);
            task.Ry = Math.Round(ry, 2);
            task.Rz = Math.Round(rz, 2);
        }

        /// <summary>
        /// Forward kinematic
        /// </summary>
        /// <param name="joint"></param>
        /// <returns>A new TaskSpace instance</returns>
        public static TaskSpace Forward(JointSpace joint)
        {
            TaskSpace task = new();
            Forward(joint, task);
            return task;
        }

        public static TaskSpace ForwardDifferential(JointSpace current, JointSpace diff)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 求逆运动学
        /// </summary>
        /// <param name="pose">任务空间（位姿）</param>
        /// <returns></returns>
        public static JointSpace Inverse(TaskSpace pose, JointSpace joint)
        {
            double m1 = l3 + l5 + lty;
            double m2 = l1 + l2 + l4;
            double r1, p2, p3, p4, r5;

            r1 = pose.Rz;
            r5 = -pose.Ry;
            p2 = pose.Px * Sind(pose.Rz) - pose.Py * Cosd(pose.Rz) - m1;
            p3 = pose.Px * Cosd(pose.Rz) + pose.Py * Sind(pose.Rz) - ltx * Cosd(-pose.Ry) - ltz * Sind(-pose.Ry);
            p4 = -pose.Pz + ltx * Sind(-pose.Ry) - ltz * Cosd(-pose.Ry) - m2;

            joint.R1 = Math.Round(r1, 2);
            joint.R5 = Math.Round(r5, 2);
            joint.P2 = Math.Round(p2, 4);
            joint.P3 = Math.Round(p3, 4);
            joint.P4 = Math.Round(p4, 4);

            return joint;
        }

        /// <summary>
        /// Solve Inverse Kinematics 求解逆运动学
        /// </summary>
        /// <param name="pose">TaskSpace</param>
        /// <returns>A new JointSpace instance</returns>
        public static JointSpace Inverse(TaskSpace pose)
        {
            JointSpace joints = new();
            Inverse(pose, joints);
            return joints;
        }

        public static JointSpace InverseDifferential(TaskSpace deltaSpace, TaskSpace currentSpace)
        {
            JointSpace djs = new();

            double dRzRad = deltaSpace.Rz * Math.PI / 180.0; // !!!计算时需要用弧度值!!!
            double dRyRad = deltaSpace.Ry * Math.PI / 180.0;
            double dPx = deltaSpace.Px;
            double dPy = deltaSpace.Py;
            double dPz = deltaSpace.Pz;

            double rz = currentSpace.Rz;
            double ry = currentSpace.Ry;
            double px = currentSpace.Px;
            double py = currentSpace.Py;
            double pz = currentSpace.Pz;

            djs.R1 = dRzRad * 180.0 / Math.PI;
            djs.R5 = -dRyRad * 180.0 / Math.PI;
            djs.P2 = Sind(rz) * dPx - Cosd(rz) * dPy + (px * Cosd(rz) + py * Sind(rz)) * dRzRad;
            djs.P3 = Cosd(rz) * dPx + Sind(rz) * dPy + (-px * Sind(rz) + py * Cosd(rz)) * dRzRad + (ltx * Sind(ry) + ltz * Cosd(ry)) * dRyRad;
            djs.P4 = -dPz + (ltx * Cosd(ry) - ltz * Sind(ry)) * (-dRyRad);

            djs.R1 = Math.Round(djs.R1, 2);
            djs.R5 = Math.Round(djs.R5, 2);
            djs.P2 = Math.Round(djs.P2, 4);
            djs.P3 = Math.Round(djs.P3, 4);
            djs.P4 = Math.Round(djs.P4, 4);

            return djs;
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

        /// <summary>
        /// 计算角度制的 cos(x) 值
        /// </summary>
        /// <param name="x">角度</param>
        /// <returns></returns>
        private static double Cosd(double x)
        {
            return Math.Cos(x * Math.PI / 180.0);
        }

        /// <summary>
        /// 计算角度制的 sin(x) 值
        /// </summary>
        /// <param name="x">角度</param>
        /// <returns></returns>
        private static double Sind(double x)
        {
            return Math.Sin(x * Math.PI / 180.0);
        }
    }
}
