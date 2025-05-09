using System.IO.Ports;

namespace DOF5RobotControl_GUI.Model
{
    class RmdMotor
    {
        public short Temperature { get; private set; } = 0;  // 温度，单位 1℃
        public short Power { get; private set; } = 0;  // 功率 -1000 ~ 1000
        public Int16 Velocity { get; private set; } = 0;  // 转速，单位 1dps
        public UInt16 Encoder { get; private set; } = 0;  // 编码器位置，范围 0~16383

        readonly SerialPort port;
        readonly byte id;

        public RmdMotor(SerialPort port, byte id)
        {
            this.port = port;
            this.id = id;

            if (!port.IsOpen)
                port.Open();
        }

        /// <summary>
        /// 获取多圈角度
        /// </summary>
        /// <returns>多圈角度值，单位为 0.01°</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Int64 GetMultiAngle()
        {
            const byte commandByte = 0x92;

            const int bytesToWrite = 5;
            const byte sendDataLength = 0;

            const int bytesToRead = 14;
            const byte receiveDataLength = 0x08;

            // 发送命令，接收回复数据
            SendCommand(bytesToWrite, commandByte, sendDataLength);
            var data = ReceiveResponse(bytesToRead, commandByte, receiveDataLength);

            // 解析接收到的数据
            Int64 motorAngle = 0;
            for (int i = 0; i < receiveDataLength; i++)
            {
                motorAngle |= ((Int64)data[i]) << (8 * i);
            }

            return motorAngle;
        }

        /// <summary>
        /// 读取单圈角度
        /// </summary>
        /// <returns>单圈角度，单位 0.01deg</returns>
        public UInt16 GetSingleAngle()
        {
            const byte commandByte = 0x94;

            const int bytesToWrite = 5;
            const byte sendDataLength = 0;

            const int bytesToRead = 8;
            const byte receiveDataLength = 0x02;

            SendCommand(bytesToWrite, commandByte, sendDataLength);
            var data = ReceiveResponse(bytesToRead, commandByte, receiveDataLength);

            UInt16 angle = 0;
            for (int i = 0; i < receiveDataLength; i++)
            {
                angle |= (UInt16)(((UInt16)data[i]) << (8 * i));
            }

            return angle;
        }

        /// <summary>
        /// 将当前位置设为零点
        /// </summary>
        public void SetZero()
        {
            const byte commandByte = 0x19;

            const int bytesToWrite = 5;
            const byte sendDataLength = 0;

            const int bytesToRead = 5;
            const byte receiveDataLength = 0;

            SendCommand(bytesToWrite, commandByte, sendDataLength);
            ReceiveResponse(bytesToRead, commandByte, receiveDataLength);
        }

        /// <summary>
        /// 电机关闭
        /// </summary>
        public void Shutdown()
        {
            const byte commandByte = 0x80;

            const int bytesToWrite = 5;
            const byte sendDataLength = 0;

            const int bytesToRead = 5;
            const byte receiveDataLength = 0;

            SendCommand(bytesToWrite, commandByte, sendDataLength);
            ReceiveResponse(bytesToRead, commandByte, receiveDataLength);
        }

        /// <summary>
        /// 电机停止
        /// </summary>
        public void Stop()
        {
            const byte commandByte = 0x81;

            const int bytesToWrite = 5;
            const byte sendDataLength = 0;

            const int bytesToRead = 5;
            const byte receiveDataLength = 0;

            SendCommand(bytesToWrite, commandByte, sendDataLength);
            ReceiveResponse(bytesToRead, commandByte, receiveDataLength);
        }

        /// <summary>
        /// 开环控制
        /// </summary>
        /// <param name="power">输出功率，范围 -1000~1000</param>
        public void OpenLoopControl(Int16 power)
        {
            const byte commandByte = 0xA0;

            const int bytesToWrite = 8;
            const byte sendDataLength = 0x02;

            const int bytesToRead = 13;
            const byte receiveDataLength = 0x07;

            byte[] sendData = BitConverter.GetBytes(power);
            SendCommand(bytesToWrite, commandByte, sendDataLength, sendData);
            var data = ReceiveResponse(bytesToRead, commandByte, receiveDataLength);

            Temperature = data[0];
            Power = BitConverter.ToInt16(data, 1);
            Velocity = BitConverter.ToInt16(data, 3);
            Encoder = BitConverter.ToUInt16(data, 5);
        }

        /// <summary>
        /// 速度闭环控制
        /// </summary>
        /// <param name="velocity">速度，单位 0.01dps</param>
        public void VelocityControl(Int32 velocity)
        {
            const byte commandByte = 0xA2;

            const int bytesToWrite = 10;
            const byte sendDataLength = 0x04;

            const int bytesToRead = 13;
            const byte receiveDataLength = 0x07;

            byte[] sendData = BitConverter.GetBytes(velocity);
            SendCommand(bytesToWrite, commandByte, sendDataLength, sendData);
            var data = ReceiveResponse(bytesToRead, commandByte, receiveDataLength);

            Temperature = data[0];
            Power = BitConverter.ToInt16(data, 1);
            Velocity = BitConverter.ToInt16(data, 3);
            Encoder = BitConverter.ToUInt16(data, 5);
        }

        /// <summary>
        /// 多圈位置闭环控制
        /// </summary>
        /// <param name="angle">角度，单位 0.01deg</param>
        public void MultiAngleControl(Int64 angle)
        {
            const byte commandByte = 0xA3;

            const int bytesToWrite = 14;
            const byte sendDataLength = 0x08;

            const int bytesToRead = 13;
            const byte receiveDataLength = 0x07;

            byte[] sendData = BitConverter.GetBytes(angle);
            SendCommand(bytesToWrite, commandByte, sendDataLength, sendData);
            var data = ReceiveResponse(bytesToRead, commandByte, receiveDataLength);

            Temperature = data[0];
            Power = BitConverter.ToInt16(data, 1);
            Velocity = BitConverter.ToInt16(data, 3);
            Encoder = BitConverter.ToUInt16(data, 5);
        }

        /// <summary>
        /// 多圈位置闭环控制
        /// </summary>
        /// <param name="angle">角度，单位 0.01deg</param>
        /// <param name="maxSpeed">最大速度，单位 0.01dps</param>
        public void MultiAngleControl(Int64 angle, UInt32 maxSpeed)
        {
            const byte commandByte = 0xA4;

            const int bytesToWrite = 18;
            const byte sendDataLength = 0x0C;

            const int bytesToRead = 13;
            const byte receiveDataLength = 0x07;

            byte[] sendData = new byte[sendDataLength];
            BitConverter.GetBytes(angle).CopyTo(sendData, 0);
            BitConverter.GetBytes(maxSpeed).CopyTo(sendData, 8);
            SendCommand(bytesToWrite, commandByte, sendDataLength, sendData);
            var data = ReceiveResponse(bytesToRead, commandByte, receiveDataLength);

            Temperature = data[0];
            Power = BitConverter.ToInt16(data, 1);
            Velocity = BitConverter.ToInt16(data, 3);
            Encoder = BitConverter.ToUInt16(data, 5);
        }

        /// <summary>
        /// 单圈位置闭环控制
        /// </summary>
        /// <param name="direction">方向，0x00 为顺时针, 0x01 为逆时针</param>
        /// <param name="angle">角度，范围 0~35999, 单位 0.01 degree</param>
        public void PositionControl(byte direction, UInt16 angle)
        {
            const byte commandByte = 0xA5;

            const int bytesToWrite = 10;
            const byte sendDataLength = 0x04;

            const int bytesToRead = 13;
            const byte receiveDataLength = 0x07;

            // 准备数据
            byte[] sendData = new byte[sendDataLength];
            sendData[0] = direction;
            Array.Copy(BitConverter.GetBytes(angle), 0, sendData, 1, 2);
            sendData[3] = 0x00;

            SendCommand(bytesToWrite, commandByte, sendDataLength, sendData);
            var data = ReceiveResponse(bytesToRead, commandByte, receiveDataLength);

            // 解析接收到的数据
            Temperature = data[0];
            Power = BitConverter.ToInt16(data, 1);
            Velocity = BitConverter.ToInt16(data, 3);
            Encoder = BitConverter.ToUInt16(data, 5);
        }

        /// <summary>
        /// 单圈位置闭环控制
        /// </summary>
        /// <param name="direction">方向，0x00 为顺时针, 0x01 为逆时针</param>
        /// <param name="angle">角度，范围 0~35999, 单位 0.01 degree</param>
        /// <param name="maxSpeed">限制转动的最大速度，单位 0.01 dps</param>
        public void PositionControl(byte direction, UInt16 angle, UInt32 maxSpeed)
        {
            const byte commandByte = 0xA6;

            const int bytesToWrite = 14;
            const byte sendDataLength = 0x08;

            const int bytesToRead = 13;
            const byte receiveDataLength = 0x07;

            // 准备数据
            byte[] sendData = new byte[sendDataLength];
            sendData[0] = direction;
            BitConverter.GetBytes(angle).CopyTo(sendData, 1);
            sendData[3] = 0x00;
            BitConverter.GetBytes(maxSpeed).CopyTo(sendData, 4);

            SendCommand(bytesToWrite, commandByte, sendDataLength, sendData);
            var data = ReceiveResponse(bytesToRead, commandByte, receiveDataLength);

            // 解析接收到的数据
            Temperature = data[0];
            Power = BitConverter.ToInt16(data, 1);
            Velocity = BitConverter.ToInt16(data, 3);
            Encoder = BitConverter.ToUInt16(data, 5);
        }

        /// <summary>
        /// 增量位置闭环控制
        /// </summary>
        /// <param name="increment">单位 0.01 degree</param>
        public void IncrementalControl(Int32 increment)
        {
            const byte commandByte = 0xA7;

            const int bytesToWrite = 10;
            const byte sendDataLength = 0x04;

            const int bytesToRead = 13;
            const byte receiveDataLength = 0x07;

            // 准备数据
            byte[] sendData = BitConverter.GetBytes(increment);

            SendCommand(bytesToWrite, commandByte, sendDataLength, sendData);
            var data = ReceiveResponse(bytesToRead, commandByte, receiveDataLength);

            // 解析接收到的数据
            Temperature = data[0];
            Power = BitConverter.ToInt16(data, 1);
            Velocity = BitConverter.ToInt16(data, 3);
            Encoder = BitConverter.ToUInt16(data, 5);
        }

        /// <summary>
        /// 增量位置闭环控制
        /// </summary>
        /// <param name="increment">单位 0.01degree</param>
        /// <param name="maxSpeed">最大速度，单位 0.01dps</param>
        public void IncrementalControl(Int32 increment, UInt32 maxSpeed)
        {
            const byte commandByte = 0xA8;

            const int bytesToWrite = 14;
            const byte sendDataLength = 0x08;

            const int bytesToRead = 13;
            const byte receiveDataLength = 0x07;

            // 准备数据
            byte[] sendData = new byte[sendDataLength];
            BitConverter.GetBytes(increment).CopyTo(sendData, 0);
            BitConverter.GetBytes(maxSpeed).CopyTo(sendData, 4);

            SendCommand(bytesToWrite, commandByte, sendDataLength, sendData);
            var data = ReceiveResponse(bytesToRead, commandByte, receiveDataLength);

            // 解析接收到的数据
            Temperature = data[0];
            Power = BitConverter.ToInt16(data, 1);
            Velocity = BitConverter.ToInt16(data, 3);
            Encoder = BitConverter.ToUInt16(data, 5);
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="byteCount">命令帧的字节数</param>
        /// <param name="commandByte">命令字节</param>
        /// <param name="dataLength">数据长度</param>
        /// <exception cref="InvalidOperationException"></exception>
        private void SendCommand(int byteCount, byte commandByte, byte dataLength, byte[]? data = null)
        {
            const byte headByte = 0x3E;

            byte[] writeBuf = new byte[byteCount];
            writeBuf[0] = headByte;
            writeBuf[1] = commandByte;
            writeBuf[2] = id;
            writeBuf[3] = dataLength;
            writeBuf[4] = GetCheckSum(writeBuf, 0, 4);

            if (dataLength > 0)
            {
                if (data == null || data.Length < dataLength)
                {
                    throw new ArgumentException("data is null or not enough length");
                }

                data.CopyTo(writeBuf, 5);
                writeBuf[byteCount - 1] = GetCheckSum(writeBuf, 5, byteCount - 1);
            }

            try
            {
                port.Write(writeBuf, 0, byteCount);
            }
            catch (TimeoutException ex)
            {
                throw new InvalidOperationException("timeout when writing serial ports", ex);
            }
        }

        /// <summary>
        /// 接收回复帧，检查格式，返回数据
        /// </summary>
        /// <param name="byteCount">回复帧的字节数</param>
        /// <param name="commandByte">命令字节</param>
        /// <param name="dataLength">数据长度</param>
        /// <returns>长度为 dataLength 的数据数组</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private byte[] ReceiveResponse(int byteCount, byte commandByte, byte dataLength)
        {
            const byte headByte = 0x3E;

            byte[] readBuf = new byte[byteCount];
            try
            {
                port.Read(readBuf, 0, byteCount);
            }
            catch (TimeoutException ex)
            {
                throw new InvalidOperationException("timeout when reading serial ports", ex);
            }

            // Check received format
            if (readBuf[0] != headByte ||
                readBuf[1] != commandByte ||
                readBuf[2] != id ||
                readBuf[3] != dataLength ||
                readBuf[4] != GetCheckSum(readBuf, 0, 4))
            {
                throw new InvalidOperationException("Abnormal received data - frame header");
            }

            if (dataLength > 0)
            {
                // Check data sum
                if (readBuf[byteCount - 1] != GetCheckSum(readBuf, 5, byteCount - 1))
                {
                    throw new InvalidOperationException("Abnormal received data - data checksum invalid.");
                }

                byte[] data = new byte[dataLength];
                Array.Copy(readBuf, 5, data, 0, dataLength);

                return data;
            }
            else
                return [];

        }

        /// <summary>
        /// 计算校验和
        /// </summary>
        /// <param name="bytes">数据数组</param>
        /// <param name="start">开始位（包含）</param>
        /// <param name="end">结束位（不包含）</param>
        /// <returns>校验和</returns>
        private static byte GetCheckSum(byte[] bytes, int start, int end)
        {
            byte sum = 0;
            for (int i = start; i < end; ++i)
            {
                sum += bytes[i];
            }
            return sum;
        }
    }
}
