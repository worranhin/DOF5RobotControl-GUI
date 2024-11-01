using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI
{
    internal class JointsPosition(int r1, int p2, int p3, int p4, int r5)
    {
        public int R1 { get; set; } = r1;
        public int P2 { get; set; } = p2;
        public int P3 { get; set; } = p3;
        public int P4 { get; set; } = p4;
        public int R5 { get; set; } = r5;
    }
}
