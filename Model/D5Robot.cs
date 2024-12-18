using DOF5RobotControl_GUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
    public partial class D5Robot : IDisposable
    {
        public struct Joints
        {
            public int R1;
            public int P2;
            public int P3;
            public int P4;
            public int R5;

            public Joints(int r1, int p2, int p3, int p4, int r5) => (R1, P2, P3, P4, R5) = (r1, p2, p3, p4, r5);
        };

        public enum ErrorCode
        {
            OK = 0,
            SystemError = 100,
            CreateInstanceError = 101,
            DestroyInstanceError_nullptr,
            SerialError = 200,
            SerialInitError = 201,
            SerialCloseError = 202,
            SerialSendError = 203,
            SerialReceiveError,
            SerialReceiveError_LessThanExpected,
            SerialClearBufferError,
            NatorError = 300,
            NatorInitError = 301,
            NatorSetError,
            NatorGetError,
            NatorMoveError,
            RMDError = 400,
            RMDInitError = 401,
            RMDGetPIError = 402,
            RMDFormatError,
            RMDChecksumError,
            RMDMoveError,
            D5RError = 500,
            D5RMoveError,
            D5RNatorNotInitialized,
            D5RRMDMotorNotInitialized,
            D5RCameraNotInitialized,
            CameraError = 600,
            CameraInitError,
            CameraReadError,
            VisialError = 700
        };

        //[LibraryImport("D5RobotDll.dll")]
        //public static partial ErrorCode CreateD5RobotInstance(out IntPtr instance, [MarshalAs(UnmanagedType.LPStr)]string serialPort, [MarshalAs(UnmanagedType.LPStr)]string natorID, int topRMDId, int bottomRMDId);
        [LibraryImport("Dll/D5RobotDll.dll")]
        public static partial IntPtr CreateD5RobotInstance([MarshalAs(UnmanagedType.LPStr)] string serialPort,
                                [MarshalAs(UnmanagedType.LPStr)] string natorID, byte topRMDID,
                                byte bottomRMDID);
        [LibraryImport("Dll/D5RobotDll.dll")]
        internal static partial ErrorCode DestroyD5RobotInstance(IntPtr instance);
        [LibraryImport("Dll/D5RobotDll.dll")]
        internal static partial ErrorCode CallInitTopCamera(IntPtr instance);
        [LibraryImport("Dll/D5RobotDll.dll")]
        internal static partial ErrorCode CallInitBotCamera(IntPtr instance);
        //[LibraryImport("Dll/D5RobotDll.dll")]
        //[return: MarshalAs(UnmanagedType.I1)]
        //internal static partial bool CallIsInit(IntPtr instance);
        [LibraryImport("Dll/D5RobotDll.dll")]
        internal static partial ErrorCode CallSetZero(IntPtr instance);
        [LibraryImport("Dll/D5RobotDll.dll")]
        internal static partial ErrorCode CallStop(IntPtr instance);
        [LibraryImport("Dll/D5RobotDll.dll")]
        internal static partial ErrorCode CallJointsMoveAbsolute(IntPtr instance, Joints j);
        [LibraryImport("Dll/D5RobotDll.dll")]
        internal static partial ErrorCode CallJointsMoveRelative(IntPtr instance, Joints j);
        [LibraryImport("Dll/D5RobotDll.dll")]
        internal static partial ErrorCode CallGetTopCameraImg(IntPtr instance, ref byte data, out int size);
        [LibraryImport("Dll/D5RobotDll.dll")]
        internal static partial ErrorCode CallGetBottomCameraImg(IntPtr instance, ref byte data, out int size);

        private readonly IntPtr _robotPtr;
        private bool disposedValue;

        public D5Robot(string serialPort, string natorID, byte topRMDId, byte bottomRMDId)
        {
            _robotPtr = CreateD5RobotInstance(serialPort, natorID, topRMDId, bottomRMDId);
            if (_robotPtr == 0)
            {
                throw new Exception($"CreateD5RobotInstance error.");
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~D5Robot()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public ErrorCode InitTopCamera()
        {
            return CallInitBotCamera(_robotPtr);
        }

        public ErrorCode InitBottomCamera()
        {
            return CallInitBotCamera(_robotPtr);
        }

        //public bool IsInit()
        //{
        //    return CallIsInit(_robotPtr);
        //}

        public ErrorCode SetZero()
        {
            return CallSetZero(_robotPtr);
        }

        public ErrorCode Stop()
        {
            return CallStop(_robotPtr);
        }

        public ErrorCode JointsMoveAbsolute(Joints j)
        {
            return CallJointsMoveAbsolute(_robotPtr, j);
        }

        public ErrorCode JointsMoveRelative(Joints j)
        {
            return CallJointsMoveRelative(_robotPtr, j);
        }

        public Image GetTopCameraImg()
        {
            byte[] pData = new byte[2592 * 2048 * 3];
            int size = new int();
            ErrorCode code;
            code = CallGetTopCameraImg(_robotPtr, ref pData[0], out size);
            if (code == ErrorCode.OK)
                return Image.FromStream(new MemoryStream(pData, 0, size));
            else
                throw new Exception("Error get top camera img: " + code.ToString());
        }

        public Image GetBottomCameraImg()
        {
            byte[] pData = new byte[2592 * 2048 * 3];
            int size = new int();
            ErrorCode code;
            code = CallGetBottomCameraImg(_robotPtr, ref pData[0], out size);
            if (code == ErrorCode.OK)
                return Image.FromStream(new MemoryStream(pData, 0, size));
            else
                throw new Exception("Error get top camera img: " + code.ToString());
        }



        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                try
                {
                    var res = DestroyD5RobotInstance(_robotPtr);
                    if (res != ErrorCode.OK)
                    {
                        MessageBox.Show($"Error Destroying Instance: {res}");
                        //throw 
                    }
                }
                catch (SEHException e)
                {
                    MessageBox.Show($"Error while destroying robot instance:\n{e.Message}");
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error while destroying robot instance:\n{e.Message}");
                    throw;
                }

                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
