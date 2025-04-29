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
        public struct Point<T>(T x, T y)
        {
            public T X { get; set; } = x;
            public T Y { get; set; } = y;
        }

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

        public Image PlotTop(CamFrame frame)
        {
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);
            var results = topPredictor.DetectObb(image); // 大概会花费 59±30 ms
            var plotted = results.PlotImage(image);

            return plotted;
        }

        public async Task<Image> PlotTopAsync(CamFrame frame)
        {
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);
            var result = await topPredictor.DetectObbAsync(image); // 大概会花费 59±30 ms
            var plotted = await result.PlotImageAsync(image);

            return plotted;
        }

        public async Task<YoloResult<ObbDetection>> TopDetectAsync(CamFrame frame)
        {
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);
            return await topPredictor.DetectObbAsync(image); // 大概会花费 59±30 ms
        }

        /// <summary>
        /// 异步检测夹钳末端的位姿
        /// </summary>
        /// <param name="frame">图像帧</param>
        /// <returns>夹钳末端的位姿(x(px), y(px), rz(rad))，注意坐标系为向右为 +x，向下为 +y，顺时针为 +rz</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<(double x, double y, double rz)> TopDetectTipAsync(CamFrame frame)
        {
            using var image = Image.LoadPixelData<L8>(frame.Buffer, frame.Width, frame.Height);
            var result = await topPredictor.DetectObbAsync(image); // 大概会花费 59±30 ms

            if (result.Count < 2)
                throw new InvalidOperationException("(Part of) gripper not detected.");

            if (result.Count != 2)
                throw new InvalidOperationException("Detection error occured. (More than 2 gripper tips are detected.");

            var tip1 = result[0];
            var tip2 = result[1];

            ObbDetection leftTip, rightTip;
            if (tip1.Bounds.X < tip2.Bounds.X)
            {
                leftTip = tip1;
                rightTip = tip2;
            } 
            else
            {
                leftTip = tip2;
                rightTip = tip1;
            }

            // 通过坐标变换，取夹钳末端的中点，注意这里图像的坐标是向右为 +x，向下为 +y，顺时针为 +rz
            Point<double> leftPoint = new(leftTip.Bounds.Left, leftTip.Bounds.Top);
            Point<double> leftCenter = new(leftTip.Bounds.X, leftTip.Bounds.Y);
            leftPoint = RotatePoint(leftCenter, leftPoint, leftTip.Angle);

            Point<double> rightPoint = new(rightTip.Bounds.Left, rightTip.Bounds.Bottom);
            Point<double> rightCenter = new(rightTip.Bounds.X, rightTip.Bounds.Y);
            rightPoint = RotatePoint(rightCenter, rightPoint, rightTip.Angle);

            Point<double> midPoint = new((leftPoint.X + rightPoint.X) / 2, (leftPoint.Y + rightPoint.Y) / 2);
            double rz = Math.Atan2(rightPoint.Y - leftPoint.Y, rightPoint.X - leftPoint.X) + Math.PI / 2;
            
            return (midPoint.X, midPoint.Y, rz);
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

        /// <summary>
        /// Rotate another point around a given center point by a certain angle
        /// 绕给定中心点旋转另一个点一定角度
        /// </summary>
        /// <param name="xc">中心点的 x 坐标</param>
        /// <param name="yc">中心点的 y 坐标</param>
        /// <param name="xp">旋转点的 x 坐标</param>
        /// <param name="yp">旋转点的 y 坐标</param>
        /// <param name="theta">旋转的角度 (rad)</param>
        /// <returns>返回一个元组包含旋转后的点的坐标：(x, y)</returns>
        private static (double x, double y) RotatePoint(double xc, double yc, double xp, double yp, double theta)
        {
            var xo = xp - xc;
            var yo = yp - yc;
            var cos_theta = Math.Cos(theta);
            var sin_theta = Math.Sin(theta);
            var res_xp = xo * cos_theta + yo * sin_theta;
            var res_yp = -xo * sin_theta + yo * cos_theta;
            return (xc + res_xp, yc + res_yp);
        }

        /// <summary>
        /// Rotate another point around a given center point by a certain angle
        /// 绕给定中心点旋转另一个点一定角度
        /// </summary>
        /// <param name="center">旋转中心</param>
        /// <param name="point">待旋转的点</param>
        /// <param name="theta">旋转的角度 (rad)</param>
        /// <returns>旋转后的点</returns>
        public static Point<double> RotatePoint(Point<double> center, Point<double> point, double theta)
        {
            //var x0 = point.X - center.X; // 将旋转点根据旋转中心移至原点
            //var y0 = point.Y - center.Y;
            //var phi0 = Math.Atan2(y0, x0); // 转换为极坐标形式
            //var r0 = Math.Sqrt(x0 * x0 + y0 * y0);
            //var x1 = r0 * Math.Cos(phi0 + theta); // 在极坐标系下旋转 theta 角并转换回 x-y 坐标
            //var y1 = r0 * Math.Sin(phi0 + theta);
            //var x11 = x1 + center.X; // 基于旋转中心将旋转后的点移回原来的位置
            //var y11 = y1 + center.Y;

            // 将上述计算化简得到如下简化形式
            var (x0, y0) = (point.X, point.Y);
            var (xc, yc) = (center.X, center.Y);
            var Ctheta = Math.Cos(theta);
            var Stheta = Math.Sin(theta);
            var x1 = xc + (x0 - xc) * Ctheta - (y0 - yc) * Stheta;
            var y1 = yc + (y0 - yc) * Ctheta + (x0 - xc) * Stheta;

            return new(x1, y1);
        }
    }
}
