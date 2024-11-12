using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Model
{
    internal class TaskSpace
    {
        public double Px { get; set; }
        public double Py { get; set; }
        public double Pz { get; set; }
        public double Ry { get; set; }
        public double Rz { get; set; }

        public JointSpace ToJointSpace() => KineHelper.Inverse(this);
    }
}
