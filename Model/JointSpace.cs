using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Model
{
    internal class JointSpace : ObservableObject
    {
        private double _r1 = 0.0;
        public double R1 { get => _r1; set  { Debug.WriteLine("Set R1 in JointSpace"); SetProperty(ref _r1, value); } }
        private double _p2 = 0.0;
        public double P2 { get => _p2; set => SetProperty(ref _p2, value); }
        private double _p3 = 0.0;
        public double P3 { get => _p3; set => SetProperty(ref _p3, value); }
        private double _p4 = 0.0;
        public double P4 { get => _p4; set => SetProperty(ref _p4, value); }
        private double _r5 = 0.0;
        public double R5 { get => _r5; set => SetProperty(ref _r5, value); }

        public TaskSpace ToTaskSpace() => KineHelper.Forward(this);
    }
}
