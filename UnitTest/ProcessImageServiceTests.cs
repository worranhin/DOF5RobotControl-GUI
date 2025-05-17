using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace UnitTest {

    public class ProcessImageServiceTests
    {
        FakeCameraControlService cameraService;
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

            Assert.InRange(x, 0, 8);
            Assert.InRange(y, -0.5, 0.5);
            Assert.InRange(rz, -0.1, 0.1);
            Assert.Equal(expected_x, x, expected_x * 0.2);
            Assert.Equal(expected_y, y, expected_y * 0.2);

            outputHelper.WriteLine($"error in x: {x}, y: {y}, rz: {rz}");
        }

        [Fact]
        public async Task ProccessBottomImageAsync_StubImage_InRange()
        {
            var procImgService = CreateAndInitImageService();
            FakeCameraControlService cameraService = new();
            var bottomImg = cameraService.GetBottomFrame();

            var distance = await processImgService.ProcessBottomImageAsync(bottomImg);

            Assert.InRange(distance, -8, 0);

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
