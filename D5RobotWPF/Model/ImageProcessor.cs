using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using VisionLibrary;

namespace DOF5RobotControl_GUI.Model
{
    internal class ImageProcessor
    {
        static readonly VisionWrapper vision = new();

        /// <summary>
        /// 处理顶部相机图像，获得任务空间信息
        /// </summary>
        /// <param name="topBitmap">顶部相机的图像</param>
        /// <returns>返回一个包含 x, y, rz 误差值的元组，单位为 mm 或角度</returns>
        public static async Task<(double px, double py, double rz)> ProcessTopImgAsync(BitmapSource topBitmap)
        {
            int width, height, stride;
            byte[] rawBuffer;

            width = topBitmap.PixelWidth;
            height = topBitmap.PixelHeight;
            stride = width * ((topBitmap.Format.BitsPerPixel + 7) / 8); // 每行的字节数 ( + 7) / 8 是为了向上取整
            rawBuffer = new byte[height * stride];
            topBitmap.CopyPixels(rawBuffer, stride, 0);

            TaskSpaceError error = new();
            GCHandle handle = GCHandle.Alloc(rawBuffer, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                error = await Task.Run(() => vision.GetTaskSpaceError(pointer, width, height, stride, MatchingMode.ROUGH));
                Debug.WriteLine(error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in VisionWrapper: " + ex.Message);
            }
            finally
            {
                handle.Free();
            }

            return (error.Px, error.Py, error.Rz);
        }

        /// <summary>
        /// 异步处理底部相机的图像，获得夹钳与钳口的竖直距离
        /// </summary>
        /// <param name="bottomBitmap">底部相机图像</param>
        /// <returns>距离，单位 mm</returns>
        public static async Task<double> ProcessBottomImgAsync(BitmapSource bottomBitmap)
        {
            int width, height, stride;
            byte[] rawBuffer;

            width = bottomBitmap.PixelWidth;
            height = bottomBitmap.PixelHeight;
            stride = width * ((bottomBitmap.Format.BitsPerPixel + 7) / 8); // 每行的字节数 ( + 7) / 8 是为了向上取整
            rawBuffer = new byte[height * stride];
            bottomBitmap.CopyPixels(rawBuffer, stride, 0);

            double verticalError = 0.0;
            GCHandle handle = GCHandle.Alloc(rawBuffer, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                verticalError = await Task.Run(() => vision.GetVerticalError(pointer, width, height, stride));
                Debug.WriteLine(verticalError);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in VisionWrapper: " + ex.Message);
            }
            finally
            {
                handle.Free();
            }
            return verticalError;
        }
    }
}
