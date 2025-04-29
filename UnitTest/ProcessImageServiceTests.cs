using DOF5RobotControl_GUI.Services;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace UnitTest {

    public class ProcessImageServiceTests(ITestOutputHelper outputHelper)
    {
        [Fact]
        public async Task TestProccessTopImage()
        {
            var yoloService = new YoloDetectionService();
            var processImgService = new ProcessImageService(yoloService);
            var cameraService = new DummyCameraControlService();

            var topImg = cameraService.GetTopFrame();
            var bottomImg = cameraService.GetBottomFrame();

            processImgService.Init(topImg, bottomImg);
            var (x, y, rz) = await processImgService.ProcessTopImgAsync(topImg);
            Assert.InRange(x, 0, 15);
            Assert.InRange(y, -2, 2);
            Assert.InRange(rz, -1, 1);

            outputHelper.WriteLine($"error in x: {x}, y: {y}, rz: {rz}");
        }
    }
}
