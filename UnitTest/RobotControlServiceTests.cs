using DOF5RobotControl_GUI.Services;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading.Tasks;

namespace UnitTest
{
    public class RobotControlServiceTests
    {
        const string RmdPort = "COM16";
        RobotControlService robotService = new();

        //[Fact]
        //public void TestConnect()
        //{
        //    robotService.Connect(RmdPort);

        //    Assert.True(robotService.IsConnected, "Fail to connect robot");

        //    robotService.Disconnect();
        //}

        //[Fact]
        //public async Task TestGetJointValue()
        //{
        //    //TryConnect();
        //    try
        //    {
        //        robotService.Connect("COM16");
        //    } catch (FileNotFoundException)
        //    {
        //        return;
        //    }
        //    Assert.True(robotService.IsConnected, "Fail to connect robot");

        //    double value = robotService.GetJointValue(1);
        //    Assert.InRange(value, -90, 90);
        //    value = robotService.GetJointValue(2);
        //    Assert.InRange(value, -15, 15);
        //    value = robotService.GetJointValue(3);
        //    Assert.InRange(value, -15, 15);
        //    value = robotService.GetJointValue(4);
        //    Assert.InRange(value, -15, 15);
        //    await Task.Delay(1000);
        //    value = robotService.GetJointValue(5);
        //    Assert.InRange(value, -90, 90);
        //    //var joint = robotService.CurrentJoints;

        //    robotService.Disconnect();
        //}

        //[Fact]
        //public async Task TestJointMove()
        //{
        //    try
        //    {
        //        robotService.Connect("COM16");
        //    }
        //    catch (FileNotFoundException)
        //    {
        //        return;
        //    }
        //    Assert.True(robotService.IsConnected, "Fail to connect robot");

        //    const double f = 5;
        //    var trackX = (double t) => Math.Sin(2 * Math.PI * f * t);

        //    robotService.JointMoveAbsolute(4, 0);
        //    await Task.Delay(1000);

        //    var sw = Stopwatch.StartNew();
        //    while (robotService.IsConnected)
        //    {
        //        var t = sw.ElapsedMilliseconds / 1000.0;  // 单位 s
        //        if (t > 10)
        //            break;
                
        //        var x = trackX(t);

        //        robotService.JointMoveAbsolute(4, x);
        //        await Task.Delay(1);                
        //    }

        //    robotService.JointMoveAbsolute(4, 0);
        //    await Task.Delay(1000);
        //}

        //[Fact]
        //public async Task TestMove()
        //{
        //    var result = TryConnect();
        //    Assert.True(robotService.IsConnected, "Fail to connect robot");

        //    robotService.JointMoveAbsolute(1, 5.0);
        //    await Task.Delay(1000);
        //    Assert.Equal(5.0, robotService.GetJointValue(1), 0.1);
        //}

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
                    robotService.Disconnect();
                    return false;
                }
            });

            return result;
        }
    }
}
