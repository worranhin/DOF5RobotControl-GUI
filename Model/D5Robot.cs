using System.Runtime.InteropServices;
using System.Windows;

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
            SerialError = 200,
            SerialInitError = 201,
            SerialCloseError = 202,
            SerialSendError = 203,
            SerialReceiveError,
            NatorError = 300,
            NatorInitError = 301,
            RMDError = 400,
            RMDInitError = 401,
            RMDGetPIError = 402
        };

        [DllImport("libD5Robot.dll")]
        internal static extern ErrorCode CreateD5RobotInstance(out IntPtr instance, [MarshalAs(UnmanagedType.LPStr)]string serialPort, [MarshalAs(UnmanagedType.LPStr)]string natorID, int topRMDId, int bottomRMDId);
        [DllImport("libD5Robot.dll")]
        internal static extern ErrorCode DestroyD5RobotInstance(IntPtr instance);
        [DllImport("libD5Robot.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool CallIsInit(IntPtr instance);
        [DllImport("libD5Robot.dll")]
        internal static extern ErrorCode CallSetZero(IntPtr instance);
        [DllImport("libD5Robot.dll")]
        internal static extern ErrorCode CallStop(IntPtr instance);
        [DllImport("libD5Robot.dll")]
        internal static extern ErrorCode CallJointsMoveAbsolute(IntPtr instance, Joints j);
        [DllImport("libD5Robot.dll")]
        internal static extern ErrorCode CallJointsMoveRelative(IntPtr instance, Joints j);

        private readonly IntPtr _robotPtr;
        private bool disposedValue;

        public D5Robot(string serialPort, string natorID, int topRMDId, int bottomRMDId)
        {
            var result = CreateD5RobotInstance(out _robotPtr, serialPort, natorID, topRMDId, bottomRMDId);
            if (result != ErrorCode.OK)
            {
                throw new Exception($"CreateD5RobotInstance error: {result}");
            }
        }

        //~D5Robot()
        //{
        //    DestroyD5RobotInstance(_robotPtr);
        //}

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~D5Robot()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public bool IsInit()
        {
            return CallIsInit(_robotPtr);
        }

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
