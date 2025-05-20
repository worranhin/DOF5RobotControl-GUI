using Xunit;
using DOF5RobotControl_GUI.Services;
using System.Threading.Tasks;
using System.Diagnostics;
using Xunit.Abstractions;
using DOF5RobotControl_GUI;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;

namespace UnitTest
{
    public class YoloDectectionServiceTests(ITestOutputHelper testOutputHelper)
    {
        //[Fact]
        //public void TestRotatePoint()
        //{
        //    var point1 = YoloDetectionService.RotatePoint(new(0, 0), new(1, 0), Math.PI / 2);
        //    Assert.Equal(0, point1.X, 1e-6);
        //    Assert.Equal(1, point1.Y, 1e-6);

        //    var point2 = YoloDetectionService.RotatePoint(new(1, 1), new(2, 1), Math.PI);
        //    Assert.Equal(0, point2.X, 1e-6);
        //    Assert.Equal(1, point2.Y, 1e-6);

        //    var point3 = YoloDetectionService.RotatePoint(new(1, -1), new(2, -2), Math.PI);
        //    Assert.Equal(0, point3.X, 1e-6);
        //    Assert.Equal(0, point3.Y, 1e-6);

        //    var point4 = YoloDetectionService.RotatePoint(new(1, -1), new(2, -2), -Math.PI / 2);
        //    Assert.Equal(0, point4.X, 1e-6);
        //    Assert.Equal(-2, point4.Y, 1e-6);
        //}

        readonly YoloDetectionService yoloService = new();
        readonly FakeCameraControlService cameraService = new();

        [Fact]
        public async Task TestTopDetect()
        {     
            var frame = cameraService.GetTopFrame();

            const int testTimes = 10;
            long totalTime = 0;
            for (int i = 0; i < testTimes; i++)
            {
                var sw = Stopwatch.StartNew();
                var result = await yoloService.TopObbDetectAsync(frame);
                var t = sw.ElapsedMilliseconds;
                totalTime += t;
                sw.Stop();

                Assert.Equal(2, result.Count);
                Assert.InRange(result[0].Bounds.X, 1000, 2000);
                Assert.InRange(result[0].Bounds.Y, 1000, 2000);
                Assert.InRange(result[0].Angle, 0, 180);
            }

            var averageTime = (double)totalTime / testTimes;
            testOutputHelper.WriteLine($"Average run time: {averageTime} ms");
        }

        [Fact]
        public async Task TestBottomDetect()
        {
            var frame = cameraService.GetBottomFrame();

            var result = await yoloService.BottomDetectAsync(frame);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void TestBottomPose()
        {
            var frame = cameraService.GetBottomFrame();
            var result = yoloService.BottomPose(frame);
            Assert.NotNull(result);
            Assert.True(result.Count > 0);

            var result0 = result[0];
            Assert.Equal(4, result0.Count());

            double x_mean = 0;
            double y_mean = 0;

            for (int i = 0; i < result0.Count(); i++)
            {
                var point = result0[i].Point;
                x_mean += (point.X - x_mean) / (i + 1);
                y_mean += (point.Y - y_mean) / (i + 1);
            }

            testOutputHelper.WriteLine($"Center point is ({x_mean}, {y_mean})");

            Assert.True(result0.Bounds.Left < x_mean && result0.Bounds.Right > x_mean);
            Assert.True(result0.Bounds.Top < y_mean && result0.Bounds.Bottom > y_mean);
        }
    }
}