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
        [Fact]
        public async Task ProccessTopImageAsync_StubImage_InRange()
        {
            var procImgService = CreateAndInitImageService();
            FakeCameraControlService cameraService = new();
            var topImg = cameraService.GetTopFrame();

            const double expected_x = 4.31;
            const double expected_y = 0.55;

            //const string FakeTopImgPath = "FakeTopImage2.bmp";
            //Image<L8> img = Image.Load<L8>(FakeTopImgPath); // ¶¥²¿Í¼Ïñ
            //byte[] buffer = new byte[img.Width * img.Height];
            //img.CopyPixelDataTo(buffer);
            //CamFrame topImg = new(buffer, img.Width, img.Height);

            var (x, y, rz) = await procImgService.ProcessTopImgAsync(topImg);

            Assert.Equal(expected_x, x, 0.2);
            Assert.Equal(expected_y, y, 0.2);
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

            Assert.Equal(expected, distance, 0.2);

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
