using Compunet.YoloSharp;
using Compunet.YoloSharp.Data;
using Compunet.YoloSharp.Plotting;
using DOF5RobotControl_GUI.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DOF5RobotControl_GUI.Services
{
    public class YoloDetectionService : IYoloDetectionService
    {
        const string TopModelPath = "Assets/YoloModels/TopCam_v0.2.onnx";
        const string BottomModelPath = "Assets/YoloModels/BottomCam_v0.3.onnx";
        const string BottomKeypointPath = "Assets/YoloModels/BottomKeypoint_v0.2.onnx";

        readonly YoloPredictor topPredictor;
        readonly YoloPredictor bottomPredictor;
        readonly YoloPredictor bottomKeypointPredictor;

        public YoloDetectionService()
        {
            topPredictor = new(TopModelPath);
            bottomPredictor = new(BottomModelPath);
            bottomKeypointPredictor = new(BottomKeypointPath);
        }

        ~YoloDetectionService()
        {
            topPredictor.Dispose();
            bottomPredictor.Dispose();
        }

        public Image TopPlot(CamFrame frame)
        {
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);
            var results = topPredictor.DetectObb(image); // 大概会花费 59±30 ms
            var plotted = results.PlotImage(image);

            return plotted;
        }

        public async Task<Image> TopPlotAsync(CamFrame frame)
        {
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);
            var result = await topPredictor.DetectObbAsync(image); // 大概会花费 59±30 ms
            var plotted = await result.PlotImageAsync(image);

            return plotted;
        }

        public async Task<YoloResult<ObbDetection>> TopObbDetectAsync(CamFrame frame)
        {
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);
            return await topPredictor.DetectObbAsync(image); // 大概会花费 59±30 ms
        }

        /// <summary>
        /// 异步地对底部相机图像进行目标检测
        /// </summary>
        /// <param name="frame">底部相机图像</param>
        /// <returns>检测的结果</returns>
        public async Task<YoloResult<Detection>> BottomDetectAsync(CamFrame frame)
        {
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);
            return await bottomPredictor.DetectAsync(image);
        }

        /// <summary>
        /// 对底部相机图像进行一次姿态检测
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public YoloResult<Pose> BottomPose(CamFrame frame)
        {
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);
            return bottomKeypointPredictor.Pose(image);
        }
    }
}
