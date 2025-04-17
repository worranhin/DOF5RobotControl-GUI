using DOF5RobotControl_GUI.Model;
using OpenCvSharp;
using System.Collections.Concurrent;
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
        private record ImageRecord(CamFrame Frame, string Id);

        private static readonly string RootDir = "Records";
        private static readonly string ImageDir = "Images";
        private readonly Stopwatch stopWatch = new();
        private string rootTimestamp = string.Empty;
        private List<DataRecord> records = [];

        private BlockingCollection<ImageRecord>? imageSaveQueue;
        private Task? saveImageTask;

        public void Start()
        {
            // 初始化记录列表
            records = [];

            // 初始化记录路径并创建文件夹
            rootTimestamp = DateTimeOffset.UtcNow.ToString("yyMMddHHmmss");
            Directory.CreateDirectory(RootDir);
            Directory.CreateDirectory(Path.Combine(RootDir, rootTimestamp));
            Directory.CreateDirectory(Path.Combine(RootDir, rootTimestamp, ImageDir));

            // 初始化图像保存队列并开始处理任务
            imageSaveQueue = new(200);
            saveImageTask = Task.Run(StartSaveImageTask);

            // 开始计时
            stopWatch.Restart();
        }

        public void Stop()
        {
            //StopAsync().Wait();
            // 停止计时
            stopWatch.Stop();

            // 写入文件
            var jsonPath = Path.Combine(RootDir, rootTimestamp, "data.json");
            using (StreamWriter sw = File.CreateText(jsonPath))
            {
                string jsonStr = JsonSerializer.Serialize(records);
                sw.WriteLine(jsonStr);
            }

            // 结束图像保存线程
            if (imageSaveQueue != null)
            {
                imageSaveQueue.CompleteAdding();

                if (saveImageTask == null)
                    throw new InvalidOperationException("saveImageTask is null in StopAsync()");
                saveImageTask.Wait();
                //saveImageTask.Wait();

                imageSaveQueue.Dispose();
                imageSaveQueue = null;
            }
        }

        public async Task StopAsync()
        {
            // 停止计时
            stopWatch.Stop();

            // 写入文件
            var jsonPath = Path.Combine(RootDir, rootTimestamp, "data.json");
            using (StreamWriter sw = File.CreateText(jsonPath))
            {
                string jsonStr = JsonSerializer.Serialize(records);
                sw.WriteLine(jsonStr);
            }

            // 结束图像保存线程
            if (imageSaveQueue != null)
            {
                imageSaveQueue.CompleteAdding();

                if (saveImageTask == null)
                    throw new InvalidOperationException("saveImageTask is null in StopAsync()");
                await saveImageTask;
                //saveImageTask.Wait();

                imageSaveQueue.Dispose();
                imageSaveQueue = null;
            }
        }

        /// <summary>
        /// Record a group of data.
        /// 记录一组数据。
        /// </summary>
        /// <param name="currentJoints"></param>
        /// <param name="targetJoints"></param>
        /// <param name="topFrame"></param>
        /// <param name="bottomFrame"></param>
        public void Record(JointSpace currentJoints, JointSpace targetJoints, CamFrame topFrame, CamFrame bottomFrame)
        {
            long timestamp_ms = stopWatch.ElapsedMilliseconds;
            string topImgStr = "TopImg_" + timestamp_ms.ToString() + ".png";
            string bottomImgStr = "BottomImg_" + timestamp_ms.ToString() + ".png";

            // 添加记录
            StateRecord state = new(currentJoints.R1, currentJoints.P2, currentJoints.P3, currentJoints.P4, currentJoints.R5, topImgStr, bottomImgStr);
            ActionRecord action = new(targetJoints.R1, targetJoints.P2, targetJoints.P3, targetJoints.P4, targetJoints.R5);
            records.Add(new(timestamp_ms, state, action));

            // 添加图像至保存队列
            if (imageSaveQueue == null)
                throw new InvalidOperationException("`Start` must be called before recording.");
            imageSaveQueue.Add(new(topFrame, topImgStr));
            imageSaveQueue.Add(new(bottomFrame, bottomImgStr));
        }

        /// <summary>
        /// Record data without image.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        public void Record(JointSpace current, JointSpace target)
        {
            long timestamp_ms = stopWatch.ElapsedMilliseconds;

            StateRecord state = new(current.R1, current.P2, current.P3, current.P4, current.R5, string.Empty, string.Empty);
            ActionRecord action = new(target.R1, target.P2, target.P3, target.P4, target.R5);
            records.Add(new(timestamp_ms, state, action));
        }

        private void StartSaveImageTask()
        {
            string imageDir = Path.Combine(RootDir, rootTimestamp, ImageDir);

            if (imageSaveQueue == null)
                throw new InvalidOperationException("`imageSaveQueue` is null in `SaveImageTask`. Check the Start function.");

            while (!imageSaveQueue.IsCompleted)
            {
                ImageRecord? record = null;
                try
                {
                    record = imageSaveQueue.Take();
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                if (record != null)
                {
                    var frame = record.Frame;

                    string imgStr = record.Id;

                    Mat.FromPixelData(frame.Height, frame.Width, MatType.CV_8UC1, frame.Buffer)
                    .Resize(new Size(324, 256))
                    .ImWrite(Path.Combine(imageDir, imgStr));
                }
            }

            Debug.WriteLine("Stop saving image.");
            return;
        }
    }
}
