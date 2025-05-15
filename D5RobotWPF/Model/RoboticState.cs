using CommunityToolkit.Mvvm.ComponentModel;
using D5R;

namespace DOF5RobotControl_GUI.Model
{
    public partial class RoboticState : ObservableObject
    {
        private bool isJointUpdating = false;
        private bool isTaskUpdating = false;

        bool IsManuallyUpdating
        {
            set
            {
                this.isJointUpdating = value;
                this.isTaskUpdating = value;
            }
        }

        [ObservableProperty]
        private JointSpace _jointSpace = new();

        partial void OnJointSpaceChanged(JointSpace value)
        {
            value.PropertyChanging += (sender, e) =>
            {
                isJointUpdating = true; // 指示正在更新属性
            };

            value.PropertyChanged += (sender, e) =>
            {
                if (!isTaskUpdating) // 如果本来就在更新属性，则不要再根据 joint 更新，避免互相递归地调用
                {
                    if (!JointSpace.HasErrors)
                    {
                        KineHelper.Forward(JointSpace, TaskSpace);
                    }
                }

                isJointUpdating = false; // 指示结束更新属性
            };
        }

        [ObservableProperty]
        private TaskSpace _taskSpace = new();

        partial void OnTaskSpaceChanged(TaskSpace value)
        {
            value.PropertyChanging += (sender, e) =>
            {
                isTaskUpdating = true;
            };

            value.PropertyChanged += (sender, e) =>
            {
                if (!isJointUpdating)
                {
                    KineHelper.Inverse(TaskSpace, JointSpace);
                    //KineHelper.ClipJoint(JointSpace);  // TODO: 处理超程问题
                    //KineHelper.Forward(JointSpace, TaskSpace);
                }
                isTaskUpdating = false;
            };
        }

        public RoboticState()
        {
            JointSpace = new();
            TaskSpace = KineHelper.Forward(JointSpace);
            //InitHandler();
        }

        public RoboticState(JointSpace joint)
        {
            JointSpace = joint.Clone();
            TaskSpace = KineHelper.Forward(JointSpace);
        }

        public RoboticState(double r1, double p2, double p3, double p4, double r5)
        {
            JointSpace = new() { R1 = r1, P2 = p2, P3 = p3, P4 = p4, R5 = r5 };
            TaskSpace = KineHelper.Forward(JointSpace);
        }

        public static RoboticState operator -(RoboticState left, RoboticState right)
        {
            RoboticState result = new();
            result.Copy(left);
            result.JointSpace.Minus(right.JointSpace);
            return result;
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
                P2 = -(int)(JointSpace.P2 * 1000000),
                P3 = (int)(JointSpace.P3 * 1000000),
                P4 = (int)(JointSpace.P4 * 1000000),
                R5 = -(int)(JointSpace.R5 * 100)
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

            IsManuallyUpdating = true;

            JointSpace.R1 = j.R1 / 100.0;
            JointSpace.P2 = -j.P2 / 1000000.0;
            JointSpace.P3 = j.P3 / 1000000.0;
            JointSpace.P4 = j.P4 / 1000000.0;
            JointSpace.R5 = -j.R5 / 100.0;

            KineHelper.Forward(JointSpace, TaskSpace);

            IsManuallyUpdating = false;
        }

        /// <summary>
        /// 完全复制传入参数的 RoboticState
        /// </summary>
        /// <param name="state"></param>
        /// <returns>返回已修改的本对象</returns>
        public RoboticState Copy(RoboticState state)
        {
            IsManuallyUpdating = true;
            JointSpace.Copy(state.JointSpace);
            KineHelper.Forward(JointSpace, TaskSpace);
            IsManuallyUpdating = false;

            return this;
        }

        /// <summary>
        /// 克隆调用对象，返回本对象的副本
        /// </summary>
        /// <returns></returns>
        public RoboticState Clone()
        {
            RoboticState retval = new();
            retval.Copy(this);
            return retval;
        }
    }
}
