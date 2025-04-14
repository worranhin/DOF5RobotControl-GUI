using DOF5RobotControl_GUI.Model;
using OpenCvSharp;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace DOF5RobotControl_GUI.Services
{
    // 用于采集数据的类型
    public record StateRecord(double R1, double P2, double P3, double P4, double R5, string TopImg, string BottomImg);
    public record ActionRecord(double R1, double P2, double P3, double P4, double R5);
    public record DataRecord(long Timestamp, StateRecord State, ActionRecord Action);

    public class DataRecordService : IDataRecordService
    {
        private static readonly string RootDir = "Records";
        private static readonly string ImageDir = "Images";
        private string rootTimestamp = String.Empty;
        private List<DataRecord> records = [];
        private Stopwatch stopWatch = new();

        public void Start()
        {
            records = [];
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            rootTimestamp = currentTime.ToString("yyMMddHHmmss");

            Directory.CreateDirectory(RootDir);
            Directory.CreateDirectory(Path.Combine(RootDir, rootTimestamp));
            Directory.CreateDirectory(Path.Combine(RootDir, rootTimestamp, ImageDir));

            stopWatch.Restart();
        }

        public void Stop()
        {
            string jsonStr = JsonSerializer.Serialize(records);
            var jsonPath = Path.Combine(RootDir, rootTimestamp, "data.json");
            
            // 写入文件
            using StreamWriter sw = File.CreateText(jsonPath);
            sw.WriteLine(jsonStr);

            stopWatch.Stop();
        }

        public void Record(JointSpace currentJoints, JointSpace targetJoints, CamFrame topFrame, CamFrame bottomFrame)
        {
            //DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            //long timestamp_ms = currentTime.ToUnixTimeMilliseconds();
            long timestamp_ms = stopWatch.ElapsedMilliseconds;
            string topImgStr = "TopImg_" + timestamp_ms.ToString() + ".bmp";
            string bottomImgStr = "BottomImg_" + timestamp_ms.ToString() + ".bmp";

            // 添加记录
            StateRecord state = new(currentJoints.R1, currentJoints.P2, currentJoints.P3, currentJoints.P4, currentJoints.R5, topImgStr, bottomImgStr);
            ActionRecord action = new(targetJoints.R1, targetJoints.P2, targetJoints.P3, targetJoints.P4, targetJoints.R5);
            records.Add(new(timestamp_ms, state, action));

            // 写入图像
            string imageDir = Path.Combine(RootDir, rootTimestamp, ImageDir);

            Mat.FromPixelData(topFrame.Height, topFrame.Width, MatType.CV_8UC1, topFrame.Buffer)
                .ImWrite(Path.Combine(imageDir, topImgStr));

            Mat.FromPixelData(bottomFrame.Height, bottomFrame.Width, MatType.CV_8UC1, bottomFrame.Buffer)
                .ImWrite(Path.Combine(imageDir, bottomImgStr));
        }

        public void Record(JointSpace current, JointSpace target)
        {
            long timestamp_ms = stopWatch.ElapsedMilliseconds;

            StateRecord state = new(current.R1, current.P2, current.P3, current.P4, current.R5, string.Empty, string.Empty);
            ActionRecord action = new(target.R1, target.P2, target.P3, target.P4, target.R5);
            records.Add(new(timestamp_ms, state, action));
        }
    }
}
