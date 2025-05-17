using Compunet.YoloSharp.Data;
using DOF5RobotControl_GUI.Model;
using OpenCvSharp;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VisionLibrary;

namespace DOF5RobotControl_GUI.Services
{
    public class ProcessImageService(IYoloDetectionService yoloService) : IProcessImageService
    {
        public struct Point<T>(T x, T y)
        {
            public T X { get; set; } = x;
            public T Y { get; set; } = y;
        }

        const double PixelToMMScale = 0.00945084; // 图像像素与物理位置的映射关系 (单位：mm/px)

        readonly VisionWrapper vision = new("./Assets/HalconModels/");

        private bool hasInitialized = false;

        /// <summary>
        /// 对图像作预处理，必须在每次相机移动后调用
        /// </summary>
        /// <param name="topFrame">顶部相机图像</param>
        /// <param name="bottomFrame">底部相机图像</param>
        public void Init(CamFrame topFrame, CamFrame bottomFrame)
        {
            GCHandle topHandle = GCHandle.Alloc(topFrame.Buffer, GCHandleType.Pinned);
            try
            {
                vision.JawLibSegmentation(topHandle.AddrOfPinnedObject(), topFrame.Width, topFrame.Height, topFrame.Stride);
            }
            catch (VisionException ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                topHandle.Free();
            }

            GCHandle bottomHandle = GCHandle.Alloc(bottomFrame.Buffer, GCHandleType.Pinned);
            try
            {
                vision.GetHorizontalLine(bottomHandle.AddrOfPinnedObject(), bottomFrame.Width, bottomFrame.Height, bottomFrame.Stride);
            }
            catch (VisionException ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                bottomHandle.Free();
            }

            hasInitialized = true;
        }

        /// <summary>
        /// 处理顶部相机的图像，若移动过相机必须先调用 Init()
        /// </summary>
        /// <param name="frame">顶部图像帧</param>
        /// <returns>元组 (error_x, error_y, error_rz) 单位为 mm 和 rad</returns>
        public async Task<(double px, double py, double rz)> ProcessTopImgAsync(CamFrame frame)
        {
            if (!hasInitialized)
                throw new InvalidOperationException("Init should be called before process image after the camera moved.");

            var rawBuffer = frame.Buffer;
            var width = frame.Width;
            var height = frame.Height;
            var stride = frame.Stride;

            GCHandle handle = GCHandle.Alloc(rawBuffer, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();

                // 通过 YOLO 获取夹钳位姿
                var getGripperPoseTask = GetGripperPoseAsync(frame);

                // 通过 Halcon 模板匹配获取钳口位姿
                var getJawPoseTask = Task.Run(() => vision.GetJawPos(pointer, width, height, stride));

                // 等待任務完成并获取处理结果
                var (x_g, y_g, rz_g) = await getGripperPoseTask;
                var (x_j, y_j, rz_j) = await getJawPoseTask;
                rz_j = -rz_j; // GetJawPos 获取的角度好像逆时针为正 TODO: 确认这个地方

                Debug.WriteLine($"Gripper pose: x={x_g}, y={y_g}, rz={rz_g}");
                Debug.WriteLine($"Jaw pose: x={x_j}, y={y_j}, rz={rz_j}");

                // 计算它们的位姿差值
                double dx = x_j - x_g;
                double dy = y_j - y_g;
                double drz = rz_j - rz_g;

                // 将图像坐标转换为机器人坐标
                var err_x = -dy * PixelToMMScale;
                var err_y = -dx * PixelToMMScale;
                var err_rz = -drz;

                return (err_x, err_y, err_rz);
            }
            catch (VisionException ex)
            {
                throw new InvalidOperationException("Error occured when process image", ex);
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// 处理底部相机的图像，若移动过相机必须先调用 Init()
        /// </summary>
        /// <param name="frame">底部相机图像</param>
        /// <returns>夹钳到钳口库的竖直方向上的距离，单位 mm</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public double ProcessBottomImage(CamFrame frame)
        {
            var result = yoloService.BottomDetect(frame);
            if (result.Count != 2)
                throw new InvalidOperationException($"Detection error occured. Expect 2 results, but got {result.Count}");

            return ProcessBottomYoloResult(result);
        }

        /// <summary>
        /// 异步地处理底部相机的图像，若移动过相机必须先调用 Init()
        /// </summary>
        /// <param name="frame">底部相机图像</param>
        /// <returns>夹钳到钳口库的竖直方向上的距离，单位 mm</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<double> ProcessBottomImageAsync(CamFrame frame)
        {
            if (!hasInitialized)
                throw new InvalidOperationException("Init should be called before process image after the camera moved.");

            var result = await yoloService.BottomDetectAsync(frame);
            if (result.Count != 2)
                throw new InvalidOperationException($"Detection error occured. Expect 2 results, but got {result.Count}");

            return ProcessBottomYoloResult(result);
        }


        //public async Task<(double x, double y, double rz)> GetEntranceErrorAsync(CamFrame topImg)
        //{
        //    if (!hasInitialized)
        //        throw new InvalidOperationException("Init should be called before process image after the camera moved.");

        //    // 获取夹钳前端位姿
        //    var getGripperPoseTask = GetGripperPoseAsync(topImg);

        //    // 获取钳口入口位姿
        //    var getJawPoseTask = GetJawPoseAsync(topImg);

        //    var (x_g, y_g, rz_g) = await getGripperPoseTask;
        //    var (x_j, y_j, rz_j) = await getJawPoseTask;


        //    // 转换为机器人坐标系，返回位姿差
        //}

        /// <summary>
        /// 处理顶部图像 YOLO 检测的结果
        /// </summary>
        /// <param name="result">YOLO 检测的结果</param>
        /// <returns>夹钳末端的位姿(x(px), y(px), rz(rad))，注意坐标系为向右为 +x，向下为 +y，顺时针为 +rz</returns>
        private static (double x, double y, double rz) ProcessTopYoloResult(YoloResult<ObbDetection> result)
        {
            ObbDetection[] goodResults;

            /// 对检测结果进行预处理 ///

            // 如果初始检测结果小于 2 个，则丢出异常
            if (result.Count < 2)
                throw new InvalidOperationException("(Part of) gripper not detected.");

            if (result.Count == 2)  // 如果检测结果数等于 2 个，就直接取这个结果
            {
                goodResults = [.. result];
            }
            else  // 如果初始检测结果大于 2 个，做后续处理
            {
                const double threshold = 0.8;
                var thresholdQuery = from item in result
                                     where item.Confidence > threshold
                                     select item;

                if (thresholdQuery.Count() == 2)  // 如果去除置信度低的结果数刚好为 2 则直接取这个结果
                {
                    goodResults = [.. thresholdQuery];
                }
                else  // 否则使用分类的方法
                {
                    SortedSet<ObbDetection> res1 = new(new DetectionComparer());
                    SortedSet<ObbDetection> res2 = new(new DetectionComparer());

                    foreach (var detection in result)
                    {
                        // 首次迭代的情况
                        if (res1.Count == 0)
                        {
                            res1.Add(detection);
                            continue;
                        }

                        var detection1 = res1.First();
                        if (Math.Abs(detection.Bounds.X - detection1.Bounds.X) < detection1.Bounds.Width / 2 &&
                            Math.Abs(detection.Bounds.Y - detection1.Bounds.Y) < detection1.Bounds.Height / 2)
                            res1.Add(detection);
                        else
                            res2.Add(detection);
                    }

                    if (res1.Max is not null && res2.Max is not null)
                        goodResults = [res1.Max, res2.Max];
                    else
                        throw new InvalidOperationException("Max should not be null at this place");
                }
            }

            // 获取夹钳左右两边的结果

            Debug.Assert(goodResults.Length == 2);
            var tip1 = goodResults[0];
            var tip2 = goodResults[1];

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
            Point<double> leftCenter = new(leftTip.Bounds.X, leftTip.Bounds.Y);
            Point<double> rightCenter = new(rightTip.Bounds.X, rightTip.Bounds.Y);

            Point<double> leftPoint = new(leftCenter.X - leftTip.Bounds.Width / 2.0, leftCenter.Y);
            Point<double> rightPoint = new(rightCenter.X - rightTip.Bounds.Width / 2.0, rightCenter.Y);

            leftPoint = RotatePoint(leftCenter, leftPoint, leftTip.Angle * Math.PI / 180.0);
            rightPoint = RotatePoint(rightCenter, rightPoint, rightTip.Angle * Math.PI / 180.0);

            //Point<double> pll, plr, prl, prr;  // 分别对应左边夹钳末端左角点，右角点，右边夹钳末端的左角点，右角点

            //pll = new(leftTip.Bounds.X - leftTip.Bounds.Width / 2.0, leftTip.Bounds.Y + leftTip.Bounds.Height / 2.0);
            //pll = RotatePoint(leftCenter, pll, leftTip.Angle * Math.PI / 180.0);
            //plr = new(leftTip.Bounds.X - leftTip.Bounds.Width / 2.0, leftTip.Bounds.Y - leftTip.Bounds.Height / 2.0);
            //plr = RotatePoint(leftCenter, plr, leftTip.Angle * Math.PI / 180.0);

            //prl = new(rightTip.Bounds.X - rightTip.Bounds.Width / 2.0, rightTip.Bounds.Y + rightTip.Bounds.Height / 2.0);
            //prl = RotatePoint(rightCenter, prl, rightTip.Angle * Math.PI / 180.0);
            //prr = new(rightTip.Bounds.X - rightTip.Bounds.Width / 2.0, rightTip.Bounds.Y - rightTip.Bounds.Height / 2.0);
            //prr = RotatePoint(rightCenter, prr, rightTip.Angle * Math.PI / 180.0);

            //Point<double> rightPoint = new(rightTip.Bounds.X - rightTip.Bounds.Width / 2.0, rightTip.Bounds.Y + rightTip.Bounds.Height / 2.0);
            //rightPoint = RotatePoint(rightCenter, rightPoint, rightTip.Angle * Math.PI / 180);

            Point<double> midPoint = new((leftPoint.X + rightPoint.X) / 2, (leftPoint.Y + rightPoint.Y) / 2);
            //Point<double> midPoint = new((pll.X + plr.X + prl.X + prr.X) / 4.0, (pll.Y + plr.Y + prl.Y + prr.Y) / 4.0);
            double rz = Math.Atan2(rightPoint.Y - leftPoint.Y, rightPoint.X - leftPoint.X);

            return (midPoint.X, midPoint.Y, rz);
        }

        /// <summary>
        /// 处理底部图像 YOLO 检测的结果
        /// </summary>
        /// <param name="result">YOLO 检测的结果</param>
        /// <returns>夹钳到钳口库的竖直方向上的距离，单位 mm</returns>
        private double ProcessBottomYoloResult(YoloResult<Detection> result)
        {
            var left = result[0];
            var right = result[1];
            var x_mean = (left.Bounds.X + right.Bounds.X) / 2.0;
            var y_mean = (left.Bounds.Y + right.Bounds.Y) / 2.0;
            var (a, b) = vision.GetJawLibLine();  // 获取钳口库线的参数 y = ax + b
            double A = a, B = -1, C = b;  // 转换为标准形式 Ax + By + C = 0
            double distance_p = Math.Abs(A * x_mean + B * y_mean + C) / Math.Sqrt(A * A + B * B);  // 计算点到直线距离
            return -distance_p * PixelToMMScale;  // 转换为 mm
        }

        /// <summary>
        /// Rotate another point around a given center point by a certain angle
        /// 绕给定中心点旋转另一个点一定角度
        /// </summary>
        /// <param name="center">旋转中心</param>
        /// <param name="point">待旋转的点</param>
        /// <param name="theta">旋转的角度 (rad)</param>
        /// <returns>旋转后的点</returns>
        private static Point<double> RotatePoint(Point<double> center, Point<double> point, double theta)
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
            double Ctheta = Math.Cos(theta);
            double Stheta = Math.Sin(theta);
            double x1 = xc + (x0 - xc) * Ctheta - (y0 - yc) * Stheta;
            double y1 = yc + (y0 - yc) * Ctheta + (x0 - xc) * Stheta;

            return new(x1, y1);
        }

        /// <summary>
        /// 异步检测夹钳末端的位姿
        /// </summary>
        /// <param name="frame">图像帧</param>
        /// <returns>夹钳末端的位姿(x(px), y(px), rz(rad))，注意坐标系为向右为 +x，向下为 +y，顺时针为 +rz</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<(double x, double y, double rz)> GetGripperPoseAsync(CamFrame frame)
        {
            var result = await yoloService.TopObbDetectAsync(frame);
            return ProcessTopYoloResult(result);
        }

        /// <summary>
        /// 异步获取钳口中心点位姿
        /// </summary>
        /// <param name="topImg">顶部相机图像</param>
        /// <returns>钳口中心点位姿，单位为 pixel 和 rad</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<(double x, double y, double rz)> GetJawPoseAsync(CamFrame topImg)
        {
            var rawBuffer = topImg.Buffer;
            var width = topImg.Width;
            var height = topImg.Height;
            var stride = topImg.Stride;

            GCHandle handle = GCHandle.Alloc(rawBuffer, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();

                // 通过 Halcon 模板匹配获取钳口位姿
                var getJawPoseTask = Task.Run(() => vision.GetJawPos(pointer, width, height, stride));

                // 等待任務完成并获取处理结果
                var (x_j, y_j, rz_j) = await getJawPoseTask;
                rz_j = -rz_j; // GetJawPos 获取的角度好像逆时针为正 TODO: 确认这个地方

                Debug.WriteLine($"Jaw pose: x={x_j}, y={y_j}, rz={rz_j}");

                return (x_j, y_j, rz_j);
            }
            catch (VisionException ex)
            {
                throw new InvalidOperationException("Error occured when process image", ex);
            }
            finally
            {
                handle.Free();
            }
        }

        class DetectionComparer : IComparer<ObbDetection>
        {
            public int Compare(ObbDetection? x, ObbDetection? y)
            {
                if (x != null && y != null)
                    return (int)(x.Confidence * 100) - (int)(y.Confidence * 100);
                else if (x == null && y != null)
                    return -1;
                else if (x != null && y == null)
                    return 1;
                else
                    return 0;
            }
        }
    }
}
