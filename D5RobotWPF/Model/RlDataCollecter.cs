using System.Diagnostics;
using System.IO;

namespace DOF5RobotControl_GUI.Model
{
    public class RlDataCollecter
    {
        public bool IsStarted { get; private set; } = false;

        record Data(long Timestamp, float X, float Y, float Z,
            float W, float Qx, float Qy, float Qz,
            float R1, float P2, float P3, float P4, float R5,
            double E1, double E2, double E3, double E4, double E5,
            float X1, float Y1, float Z1,
            float W1, float Qx1, float Qy1, float Qz1);

        static readonly string RootDir = "Records";

        readonly Stopwatch stopWatch = new();
        readonly object startLock = new();

        List<Data>? records;
        string rootTimestamp = string.Empty;

        /// <summary>
        /// 开始记录数据，进行初始化操作
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Start()
        {
            lock (startLock)
            {
                if (IsStarted == true)
                    throw new InvalidOperationException("DataRecordService has already started.");

                // 初始化记录列表
                records = [];

                // 初始化记录路径并创建文件夹
                rootTimestamp = DateTimeOffset.UtcNow.ToString("yyMMddHHmmss");
                Directory.CreateDirectory(RootDir);
                Directory.CreateDirectory(Path.Combine(RootDir, rootTimestamp));

                // 开始计时
                stopWatch.Restart();

                IsStarted = true;
            }
        }

        /// <summary>
        /// 停止记录数据，写入文件
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Stop()
        {
            lock (startLock)
            {
                if (!IsStarted)
                    throw new InvalidOperationException("Stop was called when recorder not started.");

                // 停止计时
                stopWatch.Stop();

                // 写入文件
                var csvPath = Path.Combine(RootDir, rootTimestamp, "data.csv");
                WriteRecordsToCsv(csvPath);

                records = null;
                rootTimestamp = string.Empty;

                IsStarted = false;
            }
        }

        /// <summary>
        /// 记录一次数据
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        /// <param name="qx"></param>
        /// <param name="qy"></param>
        /// <param name="qz"></param>
        /// <param name="r1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <param name="r5"></param>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <param name="e3"></param>
        /// <param name="e4"></param>
        /// <param name="e5"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="z1"></param>
        /// <param name="w1"></param>
        /// <param name="qx1"></param>
        /// <param name="qy1"></param>
        /// <param name="qz1"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Record(float x, float y, float z, float w, float qx, float qy, float qz,
            float r1, float p2, float p3, float p4, float r5,
            double e1, double e2, double e3, double e4, double e5,
            float x1, float y1, float z1, float w1, float qx1, float qy1, float qz1)
        {
            if (records == null)
                throw new InvalidOperationException("Data recorder has not started yet.");

            var t = stopWatch.ElapsedMilliseconds;
            Data data = new(t, x, y, z, w, qx, qy, qz,
                r1, p2, p3, p4, r5,
                e1, e2, e3, e4, e5,
                x1, y1, z1, w1, qx1, qy1, qz1);
            records.Add(data);
        }

        /// <summary>
        /// 记录一次数据
        /// </summary>
        /// <param name="state">视觉输出状态</param>
        /// <param name="action">神经网络输出动作</param>
        /// <param name="error">电机实际执行动作</param>
        /// <param name="nextState">视觉输出下一状态</param>
        /// <exception cref="ArgumentException"></exception>
        public void Record(float[] state, float[] action, JointSpace error, float[] nextState)
        {
            if (state.Length != 7 || action.Length != 5)
                throw new ArgumentException("State must be of length 7 and action must be of length 5.");

            Record(state[0], state[1], state[2], state[3], state[4], state[5], state[6],
                action[0], action[1], action[2], action[3], action[4],
                error.R1, error.P2, error.P3, error.P4, error.R5,
                nextState[0], nextState[1], nextState[2], nextState[3], nextState[4], nextState[5], nextState[6]);
        }

        /// <summary>
        /// 将记录写入 CSV 文件
        /// </summary>
        /// <param name="filePath">写入的文件路径</param>
        /// <exception cref="InvalidOperationException"></exception>
        private void WriteRecordsToCsv(string? filePath = null)
        {
            if (records == null || records.Count == 0)
                throw new InvalidOperationException("No records to write.");

            filePath ??= Path.Combine(RootDir, rootTimestamp, "data.csv");

            using var writer = new StreamWriter(filePath);

            // 写入 CSV 标题
            writer.WriteLine("Timestamp,X,Y,Z,W,Qx,Qy,Qz,R1,P2,P3,P4,R5,J1,J2,J3,J4,J5,X1,Y1,Z1,W1,Qx1,Qy1,Qz1");

            // 写入每条记录
            foreach (var record in records)
            {
                // 将记录转换为 CSV 格式
                var line = $"{record.Timestamp}," +
                           $"{record.X},{record.Y},{record.Z},{record.W},{record.Qx},{record.Qy},{record.Qz}," +
                           $"{record.R1},{record.P2},{record.P3},{record.P4},{record.R5}," +
                           $"{record.E1},{record.E2},{record.E3},{record.E4},{record.E5}," +
                           $"{record.X1},{record.Y1},{record.Z1},{record.W1},{record.Qx1},{record.Qy1},{record.Qz1}";

                writer.WriteLine(line);
            }
        }
    }
}
