using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI
{
    internal class JointsPosition(int r1, int p2, int p3, int p4, int r5) : ObservableObject
    {
        private int _r1;
        private int _p2;
        private int _p3;
        private int _p4;
        private int _r5;

        public int R1 { get => _r1; set => SetProperty(ref _r1,value); }
        public int P2 { get => _p2; set => SetProperty(ref _p2, value); }
        public int P3 { get => _p3; set => SetProperty(ref _p3, value); }
        public int P4 { get => _p4; set => SetProperty(ref _p4, value); }
        public int R5 { get => _r5; set => SetProperty(ref _r5, value); }

        public D5RControl.Joints ToJoints()
        {
            return new D5RControl.Joints()
            {
                R1 = R1,
                P2 = P2,
                P3 = P3,
                P4 = P4,
                R5 = R5
            };
        }

        public void SetFromJoints(D5RControl.Joints j)
        {
            this.R1 = j.R1;
            this.P2 = j.P2;
            this.P3 = j.P3;
            this.P4 = j.P4;
            this.R5 = j.R5;
        }
    }
}
