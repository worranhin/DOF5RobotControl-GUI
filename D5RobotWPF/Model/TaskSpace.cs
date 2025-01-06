using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Model
{
    public class TaskSpace : ObservableObject
    {
        private double _px;
        public double Px { get => _px; set => SetProperty(ref _px, value); }
        private double _py;
        public double Py { get => _py; set => SetProperty(ref _py, value); }
        private double _pz;
        public double Pz { get => _pz; set => SetProperty(ref _pz, value); }
        private double _ry;
        public double Ry { get => _ry; set => SetProperty(ref _ry, value); }
        private double _rz;
        public double Rz { get => _rz; set => SetProperty(ref _rz, value); }

        //public JointSpace ToJointSpace() => KineHelper.Inverse(this);
    }
}
