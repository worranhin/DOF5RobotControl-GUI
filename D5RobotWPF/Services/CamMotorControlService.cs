using System.IO.Ports;

namespace DOF5RobotControl_GUI.Services
{
    public class CamMotorControlService : ICamMotorControlService
    {
        public enum MotorSelect : byte { Top = 0, Bottom = 1 };

        private SerialPort? motorSerial;

        public bool IsConnected { get; private set; } = false;

        public void Connect(string port)
        {
            motorSerial = new(port, 115200, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 500,
                WriteTimeout = 500
            };
            motorSerial.Open();
            IsConnected = true;
        }

        public void Disconnect()
        {
            motorSerial?.Close();
            motorSerial = null;
            IsConnected = false;
        }

        public void MoveStepLeft(MotorSelect id)
        {
            if (motorSerial == null)
                throw new InvalidOperationException("相机电机串口未连接，请先调用 Connect() 方法进行连接");

            byte[] command = [0x4E, 0x40, 0x00, 0x00, 0x00];
            command[2] = (byte)id;
            command[4] = GetHeaderCheckSum(command);

            motorSerial.Write(command, 0, command.Length);
        }

        public void MoveStepRight(MotorSelect id)
        {
            if (motorSerial == null)
                throw new InvalidOperationException("相机电机串口未连接，请先调用 Connect() 方法进行连接");

            byte[] command = [0x4E, 0x41, 0x00, 0x00, 0x00];
            command[2] = (byte)id;
            command[4] = GetHeaderCheckSum(command);

            motorSerial.Write(command, 0, command.Length);
        }

        public void MoveRelativeRight(MotorSelect id, Int32 data)
        {
            if (motorSerial == null)
                throw new InvalidOperationException("相机电机串口未连接，请先调用 Connect() 方法进行连接");

            byte[] command = { 0x4E, 0x42, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            command[2] = (byte)id;
            command[4] = GetHeaderCheckSum(command);

            byte checksum_data = 0x00;
            BitConverter.GetBytes(data).CopyTo(command, 5);
            for (int i = 0; i < 4; i++)
            {
                checksum_data += command[5 + i];
            }
            command[9] = checksum_data;

            motorSerial.Write(command, 0, command.Length);
        }

        public void MoveRelativeLeft(MotorSelect id, Int32 data)
        {
            if (motorSerial == null)
                throw new InvalidOperationException("相机电机串口未连接，请先调用 Connect() 方法进行连接");

            byte[] command = { 0x4E, 0x43, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            command[2] = (byte)id;
            command[4] = GetHeaderCheckSum(command);

            byte checksum_data = 0x00;
            BitConverter.GetBytes(data).CopyTo(command, 5);
            for (int i = 0; i < 4; i++)
            {
                checksum_data += command[5 + i];
            }
            command[9] = checksum_data;

            motorSerial.Write(command, 0, command.Length);
        }

        private static byte GetHeaderCheckSum(byte[] command)
        {
            byte sum = 0x00;
            for (int i = 0; i < 4; ++i)
            {
                sum += command[i];
            }
            return sum;
        }
    }
}
