using DOF5RobotControl_GUI.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Services
{
    // 用于采集数据的类型
    public record StateRecord(long Timestamp, double R1, double P2, double P3, double P4, double R5, string TopImg, string BottomImg);

    public class DataRecordService : IDataRecordService
    {
        public void Record(JointSpace joints, CamFrame topFrame, CamFrame bottomFrame)
        {
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            long timestamp_ms = currentTime.ToUnixTimeMilliseconds();
            string topImgStr = "TopImg_" + timestamp_ms.ToString();
            string bottomImgStr = "BottomImg_" + timestamp_ms.ToString();
            StateRecord record = new(timestamp_ms, joints.R1, joints.P2, joints.P3, joints.P4, joints.R5, topImgStr, bottomImgStr);
            string jsonStr = JsonSerializer.Serialize(record);
            Debug.WriteLine(jsonStr);
        }
    }
}
