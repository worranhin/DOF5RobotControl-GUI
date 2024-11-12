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
        //private int _r1 = r1;
        //private int _p2 = p2;
        //private int _p3 = p3;
        //private int _p4 = p4;
        //private int _r5 = r5;
        //private int _px;
        //private int _py;
        //private int _pz;
        //private int _ry;
        //private int _rz;


        //public int R1 { get => _r1; set => SetProperty(ref _r1,value); }
        //public int P2 { get => _p2; set => SetProperty(ref _p2, value); }
        //public int P3 { get => _p3; set => SetProperty(ref _p3, value); }
        //public int P4 { get => _p4; set => SetProperty(ref _p4, value); }
        //public int R5 { get => _r5; set => SetProperty(ref _r5, value); }
        //public int Px { get => _px; set => SetProperty(ref _px, value); }
        //public int Py { get => _py; set => SetProperty(ref _py, value); }
        //public int Pz { get => _pz; set => SetProperty(ref _pz, value); }
        //public int Ry { get => _ry; set => SetProperty(ref _ry, value); }
        //public int Rz { get => _rz; set => SetProperty(ref _rz, value); }

        private JointSpace _jointSpace = new();
        public JointSpace JointSpace
        {
            get => _jointSpace;
            set
            {
                Debug.WriteLine("Set JointSpace in RoboticState");
                if (SetProperty(ref _jointSpace, value))
                    TaskSpace = KineHelper.Forward(JointSpace);
            }
        }

        private TaskSpace _taskSpace = new();
        public TaskSpace TaskSpace { get => _taskSpace; set => SetProperty(ref _taskSpace, value); }

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
    }
}
