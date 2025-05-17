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
        const double tolerance = 0.3;  // 视觉处理的误差，单位 mm

        [Fact]
        public async Task ProccessTopImageAsync_StubImage_InRange()
        {
            var procImgService = CreateAndInitImageService();
            FakeCameraControlService cameraService = new();
            var topImg = cameraService.GetTopFrame();

            const double expected_x = 4.31;
            const double expected_y = 0.55;

            //const string FakeTopImgPath = "FakeTopImage2.bmp";
            //Image<L8> img = Image.Load<L8>(FakeTopImgPath); // 顶部图像
            //byte[] buffer = new byte[img.Width * img.Height];
            //img.CopyPixelDataTo(buffer);
            //CamFrame topImg = new(buffer, img.Width, img.Height);

            var (x, y, rz) = await procImgService.ProcessTopImgAsync(topImg);

            Assert.Equal(expected_x, x, tolerance);
            Assert.Equal(expected_y, y, tolerance);
            Assert.InRange(rz, -0.1, 0.1);

            outputHelper.WriteLine($"error in x: {x}, y: {y}, rz: {rz}");
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

        [Fact]
        public async Task GetEntranceErrorAsync_StubImage_Expected()
        {
            var procImgService = CreateAndInitImageService();
            FakeCameraControlService cameraService = new();
            var topImg = cameraService.GetTopFrame();

            const double expected_x = 0.945;
            const double expected_y = 0.227;

            var (x, y, rz) = await procImgService.GetEntranceErrorAsync(topImg);

            Assert.Equal(expected_x, x, tolerance);
            Assert.Equal(expected_y, y, tolerance);
            Assert.InRange(rz, -0.1, 0.1);
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
