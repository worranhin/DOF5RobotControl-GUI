using Compunet.YoloSharp.Data;
using DOF5RobotControl_GUI.Model;
using SixLabors.ImageSharp;

namespace DOF5RobotControl_GUI.Services
{
    public interface IYoloDetectionService
    {
        int Detect(CamFrame frame, out YoloResult<Detection> results);
        Task<YoloResult<Detection>> DetectAsync(CamFrame frame);
        Image Plot(CamFrame frame);
        Task<Image> PlotAsync(CamFrame frame);
    }
}