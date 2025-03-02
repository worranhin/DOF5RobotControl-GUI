using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Model
{
    public partial class JointSpace : ObservableValidator
    {        
        public bool IsUpdating { get; private set; }

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
    }
}
