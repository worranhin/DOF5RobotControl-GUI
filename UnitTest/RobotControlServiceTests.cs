using DOF5RobotControl_GUI.Services;
using System.IO.Ports;

namespace UnitTest
{
    public class RobotControlServiceTests
    {
        RobotControlService robotService = new();

        [Fact]
        public void TestConnect()
        {
            var result = TryConnect();

            Assert.True(result, "Fail to connect robot");

            robotService.Disconnect();
        }

        [Fact]
        public async Task TestMove()
        {
            var result = TryConnect();
            Assert.True(robotService.IsConnected, "Fail to connect robot");

            robotService.JointMoveAbsolute(1, 5.0);
            await Task.Delay(1000);
            Assert.Equal(5.0, robotService.GetJointValue(1), 0.1);
        }

        private bool TryConnect()
        {
            var ports = SerialPort.GetPortNames();

            var result = ports.Any(port =>
            {
                try
                {
                    robotService.Connect(port);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            });

            return result;
        }
    }
}
