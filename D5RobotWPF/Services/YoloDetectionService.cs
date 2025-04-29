using Compunet.YoloSharp;
using Compunet.YoloSharp.Data;
using Compunet.YoloSharp.Plotting;
using ControlzEx.Theming;
using DOF5RobotControl_GUI.Model;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DOF5RobotControl_GUI.Services
{
    public class YoloDetectionService : IYoloDetectionService
    {
        const string yoloModelPath = "YoloModel.onnx";
        const string yoloModelTopPath = "Assets/TopC_v0.2_best.onnx";

        readonly YoloPredictor topPredictor;

        public YoloDetectionService()
        {
            topPredictor = new(yoloModelTopPath);
        }

        ~YoloDetectionService()
        {
            topPredictor.Dispose();
        }

        /// <summary>
        /// 对传入图像进行目标检测，并返回一个带检测框的图像，调用方需要显式调用返回对象的 Dispose() 方法
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public Image Plot(CamFrame frame)
        {
            using var predictor = new YoloPredictor(yoloModelPath);
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);

            var result = predictor.Detect(image);
            var plotted = result.PlotImage(image);

            return plotted;
        }

        public async Task<Image> PlotAsync(CamFrame frame)
        {
            using var predictor = new YoloPredictor(yoloModelPath);
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);

            var result = await predictor.DetectAsync(image);
            var plotted = await result.PlotImageAsync(image);

            return plotted;
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

        public int Detect(CamFrame frame, out YoloResult<Detection> results)
        {
            using var predictor = new YoloPredictor(yoloModelPath);
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);

            var result = predictor.Detect(image);
            results = result;
            return result.Count;
        }

        public async Task<YoloResult<Detection>> DetectAsync(CamFrame frame)
        {
            using var predictor = new YoloPredictor(yoloModelPath);
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);

            var result = await predictor.DetectAsync(image);
            return result;
        }
    }
}
