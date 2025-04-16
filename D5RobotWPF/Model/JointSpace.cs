using CommunityToolkit.Mvvm.ComponentModel;
using D5R;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace DOF5RobotControl_GUI.Model
{
    public partial class JointSpace : ObservableValidator
    {
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required]
        [Range(-90.0, 90.0, ErrorMessage = "Value of {0} must be between {1} and {2}.")]
        private double _r1 = 0.0;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(-16, 16, ErrorMessage = "Value of {0} must be between {1} and {2}.")]
        private double _p2 = 0.0;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(-16, 16, ErrorMessage = "Value of {0} must be between {1} and {2}.")]
        private double _p3 = 0.0;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(-16, 16, ErrorMessage = "Value of {0} must be between {1} and {2}.")]
        private double _p4 = 0.0;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(-90, 90, ErrorMessage = "Value of {0} must be between {1} and {2}.")]
        private double _r5 = 0.0;

        public JointSpace() { }

        public JointSpace(Joints joints)
        {
            SetFromD5RJoints(joints);
        }

        [Obsolete("This method is deprecated, use KineHelper instead. It will be removed after v0.7.0")]
        public TaskSpace? ToTaskSpace()
        {
            if (HasErrors)
            {
                Debug.WriteLine(GetErrors());
                return null;
            }
            else
                return KineHelper.Forward(this);
        }

        [Obsolete("This method is deprecated, use Kinehelper instead. It will be removed after v0.7.0")]
        public void ToTaskSpace(TaskSpace task)
        {
            if (HasErrors)
            {
                Debug.WriteLine(GetErrors());
                return;
            }
            else
                KineHelper.Forward(this, task);
        }

        public override string ToString()
        {
            return $"R1: {R1}, P2: {P2}, P3: {P3}, P4: {P4}, R5: {R5}";
        }

        /// <summary>
        /// 将当前关节值加上指定关节值
        /// </summary>
        /// <param name="joint"></param>
        /// <returns>原对象</returns>
        public JointSpace Add(JointSpace joint)
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
        public JointSpace Minus(JointSpace joint)
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
        public JointSpace Copy(JointSpace joint)
        {
            R1 = joint.R1;
            P2 = joint.P2;
            P3 = joint.P3;
            P4 = joint.P4;
            R5 = joint.R5;

            return this;
        }

        public JointSpace Clone()
        {
            JointSpace joint = new();
            joint.Copy(this);
            return joint;
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


            R1 = j.R1 / 100.0;
            P2 = -j.P2 / 1000000.0;
            P3 = j.P3 / 1000000.0;
            P4 = j.P4 / 1000000.0;
            R5 = -j.R5 / 100.0;
        }
    }
}
