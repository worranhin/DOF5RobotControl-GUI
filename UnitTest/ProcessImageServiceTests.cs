using DOF5RobotControl_GUI.Services;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace UnitTest {

    public class ProcessImageServiceTests
    {
        DummyCameraControlService cameraService;
        YoloDetectionService yoloService;
        ProcessImageService processImgService;
        ITestOutputHelper outputHelper;

        public ProcessImageServiceTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;

            cameraService = new();
            yoloService = new();
            processImgService = new(yoloService);

            var topImg = cameraService.GetTopFrame();
            var bottomImg = cameraService.GetBottomFrame();

            processImgService.Init(topImg, bottomImg);
        }

        [Fact]
        public async Task TestProccessTopImage()
        {
            var topImg = cameraService.GetTopFrame();

            var (x, y, rz) = await processImgService.ProcessTopImgAsync(topImg);
            Assert.InRange(x, 0, 15);
            Assert.InRange(y, -2, 2);
            Assert.InRange(rz, -1, 1);

            outputHelper.WriteLine($"error in x: {x}, y: {y}, rz: {rz}");
        }

        [Fact]
        public void TestProccessBottomImage()
        {
            double t_sum = 0;
            uint count = 10;
            for (int i = 0; i < count; i++)
            {
                var sw = Stopwatch.StartNew();
                var bottomImg = cameraService.GetBottomFrame();
                var distance = processImgService.ProcessBottomImage(bottomImg);
                var t = sw.ElapsedMilliseconds;
                t_sum += t;
                Assert.InRange(distance, -15, 0);
            }

            double t_mean = t_sum / count;
            outputHelper.WriteLine($"Average run time: {t_mean} ms");
        }
    }
}
