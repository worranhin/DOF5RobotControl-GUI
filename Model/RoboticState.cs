using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Model
{
    internal class RoboticState : ObservableObject
    {
        private JointSpace _jointSpace = new();
        public JointSpace JointSpace
        {
            get => _jointSpace;
            set
            {
                if (SetProperty(ref _jointSpace, value))
                    _jointSpace.PropertyChanged += UpdateTaskSpace;
            }
        }

        private TaskSpace _taskSpace = new();
        public TaskSpace TaskSpace
        {
            get => _taskSpace;
            set
            {
                if (SetProperty(ref _taskSpace, value))
                    _taskSpace.PropertyChanged += UpdateJointSpace;
            }
        }

        public RoboticState()
        {
            JointSpace = new();
            TaskSpace = new();
        }

        public RoboticState(double r1, double p2, double p3, double p4, double r5)
        {
            JointSpace = new() { R1 = r1, P2 = p2, P3 = p3, P4 = p4, R5 = r5 };
            TaskSpace = KineHelper.Forward(JointSpace);
        }

        /// <summary>
        /// 转换为控制用的 struct
        /// </summary>
        /// <returns></returns>
        public D5RControl.Joints ToD5RJoints()
        {
            D5RControl.Joints j = new()
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
        public void SetFromD5RJoints(D5RControl.Joints j)
        {
            JointSpace = new()
            {
                R1 = j.R1 / 100.0,
                P2 = j.P2 / 1000000.0,
                P3 = j.P3 / 1000000.0,
                P4 = j.P4 / 1000000.0,
                R5 = j.R5 / 100.0
            };

            TaskSpace = KineHelper.Forward(JointSpace);
        }

        public void UpdateTaskSpace(object? sender, PropertyChangedEventArgs e)
        {
            TaskSpace = KineHelper.Forward(JointSpace);
        }

        public void UpdateJointSpace(object? sender, PropertyChangedEventArgs e)
        {
            JointSpace = KineHelper.Inverse(TaskSpace);
        }
    }
}
