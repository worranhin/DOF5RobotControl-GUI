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
        private PropertyChangedEventHandler jointChangedHandler = (sender, e) => { };
        private PropertyChangedEventHandler taskChangedHandler = (sender, e) => { };
        private bool jointIsUpdating = false;
        private bool taskIsUpdating = false;

        [ObservableProperty]
        private JointSpace _jointSpace = new();

        [ObservableProperty]
        private TaskSpace _taskSpace = new();

        public RoboticState()
        {
            InitHandler();
        }

        public RoboticState(double r1, double p2, double p3, double p4, double r5)
        {
            JointSpace.R1 = r1;
            JointSpace.P2 = p2;
            JointSpace.P3 = p3;
            JointSpace.P4 = p4;
            JointSpace.R5 = r5;
            KineHelper.Forward(JointSpace, _taskSpace);

            InitHandler();
        }

        private void InitHandler()
        {
            JointSpace.PropertyChanging += (sender, e) =>
            {
                jointIsUpdating = true; // 指示正在更新属性
            };

            JointSpace.PropertyChanged += (sender, e) =>
            {
                if (!taskIsUpdating) // 如果本来就在更新属性，则不要再根据 joint 更新，避免互相递归地调用
                {
                    if (!JointSpace.HasErrors)
                    {
                        KineHelper.Forward(JointSpace, TaskSpace);
                    }

                }

                jointIsUpdating = false; // 指示结束更新属性
            };

            TaskSpace.PropertyChanging += (sender, e) =>
            {
                taskIsUpdating = true;
            };

            TaskSpace.PropertyChanged += (sender, e) =>
            {
                if (!jointIsUpdating)
                {
                    KineHelper.Inverse(TaskSpace, JointSpace);
                    //KineHelper.ClipJoint(JointSpace);  // TODO: 处理超程问题
                    //KineHelper.Forward(JointSpace, TaskSpace);
                }
                taskIsUpdating = false;
            };
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
            // 将旋转电机单圈角度 0~360 换算成 +/-180
            if (j.R1 > 18000)
                j.R1 = -(36000 - j.R1);
            if (j.R5 > 18000)
                j.R5 = -(36000 - j.R5);

            // TODO: 优化这段代码的性能，让它在所有属性更改后再通知 Task 进行正解
            JointSpace.R1 = j.R1 / 100.0;
            JointSpace.P2 = j.P2 / 1000000.0;
            JointSpace.P3 = j.P3 / 1000000.0;
            JointSpace.P4 = j.P4 / 1000000.0;
            JointSpace.R5 = j.R5 / 100.0;
        }



        /// <summary>
        /// 就地更新 TODO: 删除这个函数
        /// </summary>
        private void UpdateTaskSpace()
        {
            KineHelper.ClipJoint(JointSpace);
            KineHelper.Forward(JointSpace, TaskSpace);
        }

        private void UpdateJointSpace()
        {
            KineHelper.Inverse(TaskSpace, JointSpace);
            if (!KineHelper.CheckJoint(JointSpace))
            {
                KineHelper.ClipJoint(JointSpace);
                KineHelper.Forward(JointSpace, TaskSpace);
                MessageBox.Show("Joint out of range.");
            }
            //JointSpace.PropertyChanged += jointChangedHandler;
            //TaskSpace.PropertyChanged += taskChangedHandler;
        }
    }
}
