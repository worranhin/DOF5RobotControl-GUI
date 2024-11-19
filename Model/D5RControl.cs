using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI
{
    internal static partial class D5RControl
    {
        internal struct Joints(int r1, int p2, int p3, int p4, int r5)
        {
            public int R1 = r1;
            public int P2 = p2;
            public int P3 = p3;
            public int P4 = p4;
            public int R5 = r5;
        };

        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_Init", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int Init(string RMDSerialPort);
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_DeInit")]
        internal static partial int DeInit();
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_Stop")]
        internal static partial int Stop();
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_SetZero")]
        internal static partial int SetZero(int r1 = 0, int p2 = 0, int p3 = 0, int p4 = 0, int r5 = 0);
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_JointsMoveAbsolute")]
        internal static partial int JointsMoveAbsolute(Joints j);
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_JointsMoveRelative")]
        internal static partial int JointsMoveRelative(Joints j);
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_SetAccumulateRelative")]
        internal static partial int SetAccumulateRelative([MarshalAs(UnmanagedType.Bool)] bool accumulate);
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_Test")]
        internal static partial int Test(int x);

        public static bool IsZeroJoints(Joints j)
        {
            return j.R1 == 0 && j.P2 == 0 && j.P3 == 0 && j.P4 == 0 && j.R5 == 0;
        }
    }
}
