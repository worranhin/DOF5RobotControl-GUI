using Xunit;
using DOF5RobotControl_GUI.Services;
using System.Threading.Tasks;
using System.Diagnostics;
using Xunit.Abstractions;

namespace UnitTest
{
    public class YoloDectectionServiceTests(ITestOutputHelper testOutputHelper)
    {
        [Fact]
        public void TestRotatePoint()
        {
            var point1 = YoloDetectionService.RotatePoint(new(0, 0), new(1, 0), Math.PI / 2);
            Assert.Equal(0, point1.X, 1e-6);
            Assert.Equal(1, point1.Y, 1e-6);

            var point2 = YoloDetectionService.RotatePoint(new(1, 1), new(2, 1), Math.PI);
            Assert.Equal(0, point2.X, 1e-6);
            Assert.Equal(1, point2.Y, 1e-6);

            var point3 = YoloDetectionService.RotatePoint(new(1, -1), new(2, -2), Math.PI);
            Assert.Equal(0, point3.X, 1e-6);
            Assert.Equal(0, point3.Y, 1e-6);

            var point4 = YoloDetectionService.RotatePoint(new(1, -1), new(2, -2), -Math.PI / 2);
            Assert.Equal(0, point4.X, 1e-6);
            Assert.Equal(-2, point4.Y, 1e-6);
        }

        [Fact]
        public async Task TestYoloDetect()
        {
            var yoloService = new YoloDetectionService();
            var cameraService = new DummyCameraControlService();

            var frame = cameraService.GetTopFrame();

            const int testTimes = 10;
            long totalTime = 0;
            for (int i = 0; i < testTimes; i++)
            {
                var sw = Stopwatch.StartNew();
                var (x, y, rz) = await yoloService.TopDetectTipAsync(frame);
                var t = sw.ElapsedMilliseconds;
                totalTime += t;
                sw.Stop();

                Assert.InRange(x, 1000, 2000);
                Assert.InRange(y, 1000, 2000);
                Assert.InRange(rz, -10, 10);
            }

            var averageTime = (double)totalTime / testTimes;
            testOutputHelper.WriteLine($"Average run time: {averageTime} ms");
        }
    }
}