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
    }
}
