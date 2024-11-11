using DOF5RobotControl_GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

// D5Robot.h
//D5R_API D5Robot *CreateD5RobotInstance(const char *serialPort,
//                                       std::string natorID, uint8_t topRMDID,
//                                       uint8_t botRMDID);
//D5R_API void DestroyD5RobotInstance(D5Robot *instance);
//D5R_API bool CallIsInit(D5Robot *instance);
//D5R_API bool CallSetZero(D5Robot *instance);
//D5R_API bool CallStop(D5Robot *instance);
//D5R_API bool CallJointsMoveAbsolute(D5Robot *instance, const Joints j);
//D5R_API bool CallJointsMoveRelative(D5Robot *instance, const Joints j);

namespace DOF5RobotControl_GUI.Model
{
    public partial class D5Robot(string serialPort, string natorID, int topRMDId, int bottomRMDId)
    {
        public struct Joints(int r1, int p2, int p3, int p4, int r5)
        {
            public int R1 = r1;
            public int P2 = p2;
            public int P3 = p3;
            public int P4 = p4;
            public int R5 = r5;
        };

        [LibraryImport("libD5Robot.dll", StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr CreateD5RobotInstance(string serialPort, string natorID, int topRMDId, int bottomRMDId);
        [LibraryImport("libD5Robot.dll")]
        internal static partial void DestroyD5RobotInstance(IntPtr instance);
        [LibraryImport("libD5Robot.dll")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static partial bool CallIsInit(IntPtr instance);
        [LibraryImport("libD5Robot.dll")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static partial bool CallSetZero(IntPtr instance);
        [LibraryImport("libD5Robot.dll")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static partial bool CallStop(IntPtr instance);
        [LibraryImport("libD5Robot.dll")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static partial bool CallJointsMoveAbsolute(IntPtr instance, Joints j);
        [LibraryImport("libD5Robot.dll")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static partial bool CallJointsMoveRelative(IntPtr instance, Joints j);

        private readonly IntPtr _robotPtr = CreateD5RobotInstance(serialPort, natorID, topRMDId, bottomRMDId);

        ~D5Robot()
        {
            DestroyD5RobotInstance(_robotPtr);
        }

        public bool IsInit()
        {
            return CallIsInit(_robotPtr);
        }

        public bool SetZero()
        {
            return CallSetZero(_robotPtr);
        }

        public bool Stop()
        {
            return CallStop(_robotPtr);
        }

        public bool JointsMoveAbsolute(Joints j)
        {
            return CallJointsMoveAbsolute(_robotPtr, j);
        }

        public bool JointsMoveRelative(Joints j)
        {
            return CallJointsMoveRelative(_robotPtr, j);
        }
    }
}
