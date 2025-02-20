using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Model
{
    public partial class TaskSpace : ObservableObject
    {
        [ObservableProperty]
        private double _px;
        [ObservableProperty]
        private double _py;
        [ObservableProperty]
        private double _pz;
        [ObservableProperty]
        private double _ry;
        [ObservableProperty]
        private double _rz;

        public override string ToString()
        {
            string str = base.ToString() + $"\tPx:{Px} Py:{Py} Pz:{Pz} Ry:{Ry} Rz:{Rz}";
            return str;
        }

        public static double Distance(TaskSpace a, TaskSpace b)
        {
            double dx = a.Px - b.Px;
            double dy = a.Py - b.Py;
            double dz = a.Pz - b.Pz;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
