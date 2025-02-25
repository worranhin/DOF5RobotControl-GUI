using DOF5RobotControl_GUI.Model;
using OpenCvSharp;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace DOF5RobotControl_GUI.Services
{
    // 用于采集数据的类型
    public record StateRecord(long Timestamp, double R1, double P2, double P3, double P4, double R5, string TopImg, string BottomImg);

    public class DataRecordService : IDataRecordService
    {
        private List<StateRecord> records = [];

        public void Record(JointSpace joints, CamFrame topFrame, CamFrame bottomFrame)
        {
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            long timestamp_ms = currentTime.ToUnixTimeMilliseconds();
            string topImgStr = "TopImg_" + timestamp_ms.ToString() + ".bmp";
            string bottomImgStr = "BottomImg_" + timestamp_ms.ToString() + ".bmp";
            StateRecord record = new(timestamp_ms, joints.R1, joints.P2, joints.P3, joints.P4, joints.R5, topImgStr, bottomImgStr);
            records.Add(record);
            string jsonStr = JsonSerializer.Serialize(records);
            Debug.WriteLine(jsonStr);

            string rootDir = "Records_" + timestamp_ms.ToString();
            Directory.CreateDirectory(rootDir);
            var jsonPath = Path.Combine(rootDir, "data.json");
            using (StreamWriter sw = File.CreateText(jsonPath))
            {
                sw.WriteLine(jsonStr);
            }

            string imageDir = Path.Combine(rootDir, "Images");
            Directory.CreateDirectory(imageDir);

            byte[] imageBytes = topFrame.Buffer;
            string imagePath = Path.Combine(imageDir, topImgStr);
            var img = Mat.FromPixelData(topFrame.Height, topFrame.Width, MatType.CV_8UC1, imageBytes);
            img.ImWrite(imagePath);
        }
    }
}
