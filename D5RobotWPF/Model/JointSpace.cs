using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
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

        //public TaskSpace ToTaskSpace() => KineHelper.Forward(this);
    }
}
