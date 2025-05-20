using Compunet.YoloSharp.Data;
using DOF5RobotControl_GUI.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DOF5RobotControl_GUI.Services
{
    public interface IYoloDetectionService
    {
        /// <summary>
        /// 执行一次目标检测
        /// </summary>
        /// <param name="frame">图像帧</param>
        /// <param name="results">返回的检测结果</param>
        /// <returns>检测到的物体数量</returns>
        //int Detect(CamFrame frame, out YoloResult<Detection> results);

        /// <summary>
        /// 对图像进行目标检测
        /// </summary>
        /// <param name="frame">图像帧</param>
        /// <returns>检测的结果</returns>
        //Task<YoloResult<Detection>> DetectAsync(CamFrame frame);

        /// <summary>
        /// 对传入图像进行目标检测，并返回一个带检测框的图像，调用方需要显式调用返回对象的 Dispose() 方法
        /// </summary>
        /// <param name="frame">图像帧</param>
        /// <returns>一个 Image 对象，使用完后请调用 Dispose 方法</returns>
        //Image Plot(CamFrame frame);

        /// <summary>
        /// 异步地对传入图像进行目标检测，并返回一个带检测框的图像，调用方需要显式调用返回对象的 Dispose() 方法
        /// </summary>
        /// <param name="frame">图像帧</param>
        /// <returns>一个 Image 对象，使用完后请调用 Dispose 方法</returns>
        //Task<Image> PlotAsync(CamFrame frame);

        /// <summary>
        /// 对传入图像进行 OBB 目标检测，并返回一个带检测框的图像，调用方需要显式调用返回对象的 Dispose() 方法
        /// </summary>
        /// <param name="frame">图像帧</param>
        /// <returns>一个 Image 对象，使用完后请调用 Dispose 方法</returns>
        Image TopPlot(CamFrame frame);

        /// <summary>
        /// 异步地对传入图像进行 OBB 目标检测，并返回一个带检测框的图像，调用方需要显式调用返回对象的 Dispose() 方法
        /// </summary>
        /// <param name="frame">图像帧</param>
        /// <returns>一个 Image 对象，使用完后请调用 Dispose 方法</returns>
        Task<Image> TopPlotAsync(CamFrame frame);

        /// <summary>
        /// 对顶部相机的图像进行一次 OBB 检测
        /// </summary>
        /// <param name="frame">顶部图像</param>
        /// <returns>检测的结果</returns>
        Task<YoloResult<ObbDetection>> TopObbDetectAsync(CamFrame frame);

        /// <summary>
        /// 对底部相机图像进行目标检测
        /// </summary>
        /// <param name="frame">底部相机图像</param>
        /// <returns>检测的结果</returns>
        YoloResult<Detection> BottomDetect(CamFrame frame);

        /// <summary>
        /// 异步地对底部相机图像进行目标检测
        /// </summary>
        /// <param name="frame">底部相机图像</param>
        /// <returns>检测的结果</returns>
        Task<YoloResult<Detection>> BottomDetectAsync(CamFrame frame);

        /// <summary>
        /// 对底部相机图像进行一次姿态检测
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        YoloResult<Pose> BottomPose(CamFrame frame);

        /// <summary>
        /// 对传入图像进行目标检测，并返回一个带检测框的图像，调用方需要显式调用返回对象的 Dispose() 方法
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        Image BottomPlot(CamFrame frame);
    }
}