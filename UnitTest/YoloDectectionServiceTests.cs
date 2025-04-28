using Xunit;
using DOF5RobotControl_GUI.Services;

namespace UnitTest
{
    public class YoloDectectionServiceTests
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
    }
}