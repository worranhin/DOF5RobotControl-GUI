using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace UnitTest {

    public class ProcessImageServiceTests(ITestOutputHelper outputHelper)
    {
        const double tolerance = 0.1;  // 视觉处理的误差，单位 mm

        [Fact]
        public async Task GetEntranceErrorAsync_StubImage_Expected()
        {
            var procImgService = CreateAndInitImageService();
            FakeCameraControlService cameraService = new();
            var topImg = cameraService.GetTopFrame();

            const double expected_x = 0.784;
            const double expected_y = 0.36;

            var (x, y, rz) = await procImgService.GetEntranceErrorAsync(topImg);

            Assert.Equal(expected_x, x, tolerance);
            Assert.Equal(expected_y, y, tolerance);
            Assert.InRange(rz, -0.1, 0.1);
        }

        [Fact]
        public async Task GetJawErrorAsync_StubImage_Expected()
        {
            var procImgService = CreateAndInitImageService();
            FakeCameraControlService cameraService = new();
            var topImg = cameraService.GetTopFrame();

            const double expected_x = 4.68;
            const double expected_y = 0.53;

            var (x, y, rz) = await procImgService.GetJawErrorAsync(topImg);

            Assert.Equal(expected_x, x, tolerance);
            Assert.Equal(expected_y, y, tolerance);
            Assert.Equal(0, rz, tolerance);

            outputHelper.WriteLine($"x={x}, y={y}, rz={rz}");
        }

        [Fact]
        public async Task ProccessBottomImageAsync_StubImage_InRange()
        {
            var procImgService = CreateAndInitImageService();
            var cameraService = new FakeCameraControlService();
            var bottomImg = cameraService.GetBottomFrame();

            const double expected = -6.636;

            var distance = await procImgService.ProcessBottomImageAsync(bottomImg);

            Assert.Equal(expected, distance, tolerance);

            outputHelper.WriteLine($"error in z: {distance}");
        }

        private ProcessImageService CreateAndInitImageService()
        {
            ProcessImageService procImgService = new(new YoloDetectionService());
            
            FakeCameraControlService cameraService = new();
            var top = cameraService.GetTopFrame();
            var bottom = cameraService.GetBottomFrame();
            procImgService.Init(top, bottom);

            return procImgService;
        }
    }
}
