using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using D5R;

namespace DOF5RobotControl_GUI.Model
{
    public partial class RoboticState : ObservableObject
    {
        [ObservableProperty]
        private JointSpace _jointSpace = new();
        [ObservableProperty]
        private TaskSpace _taskSpace = new();

        public RoboticState()
        {
            JointSpace.PropertyChanged += UpdateTaskSpace;
            TaskSpace.PropertyChanged += UpdateJointSpace;
        }

        public RoboticState(double r1, double p2, double p3, double p4, double r5)
        {
            JointSpace = new() { R1 = r1, P2 = p2, P3 = p3, P4 = p4, R5 = r5 };
            KineHelper.Forward(JointSpace, TaskSpace);

            JointSpace.PropertyChanged += UpdateTaskSpace;
            TaskSpace.PropertyChanged += UpdateJointSpace;
        }

        /// <summary>
        /// 转换为控制用的 struct
        /// </summary>
        /// <returns></returns>
        public Joints ToD5RJoints()
        {
            Joints j = new()
            {
                R1 = (int)(JointSpace.R1 * 100),
                P2 = (int)(JointSpace.P2 * 1000000),
                P3 = (int)(JointSpace.P3 * 1000000),
                P4 = (int)(JointSpace.P4 * 1000000),
                R5 = (int)(JointSpace.R5 * 100)
            };

            return j;
        }

        /// <summary>
        /// 从控制用的 struct 设置当前状态
        /// </summary>
        /// <param name="j"></param>
        public void SetFromD5RJoints(Joints j)
        {
            // 将旋转电机单圈角度换算成 +/- 180
            if (j.R1 > 18000)
                j.R1 = -(36000 - j.R1);
            if (j.R5 > 18000)
                j.R5 = -(36000 - j.R5);


            //JointSpace = new()
            //{
            //    R1 = j.R1 / 100.0,
            //    P2 = j.P2 / 1000000.0,
            //    P3 = j.P3 / 1000000.0,
            //    P4 = j.P4 / 1000000.0,
            //    R5 = j.R5 / 100.0
            //};

            JointSpace.R1 = j.R1 / 100.0;
            JointSpace.P2 = j.P2 / 1000000.0;
            JointSpace.P3 = j.P3 / 1000000.0;
            JointSpace.P4 = j.P4 / 1000000.0;
            JointSpace.R5 = j.R5 / 100.0;



            //TaskSpace = KineHelper.Forward(JointSpace);
            //UpdateTaskSpace();
        }

        /// <summary>
        /// 就地更新
        /// </summary>
        private void UpdateTaskSpace(object? sender, PropertyChangedEventArgs e)
        {
            TaskSpace.PropertyChanged -= UpdateJointSpace;
            KineHelper.ClipJoint(JointSpace);
            KineHelper.Forward(JointSpace, TaskSpace);
            TaskSpace.PropertyChanged += UpdateJointSpace;
        }

        private void UpdateJointSpace(object? sender, PropertyChangedEventArgs e)
        {
            JointSpace.PropertyChanged -= UpdateTaskSpace;
            KineHelper.Inverse(TaskSpace, JointSpace);
            if (!KineHelper.CheckJoint(JointSpace))
            {
                MessageBox.Show("Joint out of range.");
                KineHelper.ClipJoint(JointSpace);
                KineHelper.Forward(JointSpace, TaskSpace);
            }
            JointSpace.PropertyChanged += UpdateTaskSpace;
        }
    }
}
