using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using SharpDX.XInput;
using System.Net.Http.Headers;
using System.Printing;

namespace DOF5RobotControl_GUI
{
    /// <summary>
    /// ManualControlWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ManualControlWindow : System.Windows.Window
    {
        private readonly VideoCapture capture;
        private readonly Mat frame;
        private readonly CancellationTokenSource captureCancelSource;
        private readonly CancellationTokenSource xInputCancelSource;
        private readonly CancellationToken captureCancelToken;
        private readonly CancellationToken xInputCancelToken;
        private readonly JogHandler jogHandler = new();
        private readonly int natorJogResolution = 100000;
        private readonly int RMDJogResolution = 20;

        public ManualControlWindow()
        {
            InitializeComponent();
            this.Closed += WindowClosed;

            capture = new VideoCapture(1);
            frame = new Mat();
            captureCancelSource = new();
            captureCancelToken = captureCancelSource.Token;
            xInputCancelSource = new();
            xInputCancelToken = xInputCancelSource.Token;

            Task.Run(CaptureCameraTask, captureCancelToken);
            Task.Run(XInputControlTask, xInputCancelToken);

            //var controller = new Controller();
            //if(controller.IsConnected)
            //{
            //    Debug.WriteLine("controller connected");
            //} else
            //{
            //    Debug.WriteLine("Controller not connected.");
            //}
        }

        private void WindowClosed(object? sender, EventArgs e)
        {
            captureCancelSource.Cancel();
            xInputCancelSource.Cancel();
        }

        private void CaptureCameraTask()
        {
            int retrys = 0;

            while (!captureCancelToken.IsCancellationRequested)
            {
                bool hasFrame = capture.Read(frame);
                if (!hasFrame)
                {
                    if (retrys++ >= 10)  // 重试超时，关闭线程
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("Fail to access camera frame.");
                        });
                        break;

                    }

                    Thread.Sleep(300);
                    continue;
                }

                Dispatcher.Invoke(() =>
                {
                    FrameImage.Source = BitmapSourceConverter.ToBitmapSource(frame);
                });

                Thread.Sleep(40);  // 25帧
            }

            // release resources
            capture.Dispose();
            frame?.Dispose();
        }

        private void TestXInputTask()
        {
            var controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };
            // Get 1st controller available
            Controller? controller = null;
            foreach (var selectControler in controllers)
            {
                if (selectControler.IsConnected)
                {
                    controller = selectControler;
                    break;
                }
            }

            if (controller == null)
            {
                Debug.WriteLine("No XInput controller installed");
            }
            else
            {
                Debug.WriteLine("Found a XInput controller available");
                Debug.WriteLine("Press buttons on the controller to display events or escape key to exit... ");

                // Poll events from joystick
                var previousState = controller.GetState();
                while (controller.IsConnected)
                {
                    var state = controller.GetState();
                    if (previousState.PacketNumber != state.PacketNumber)
                        Debug.WriteLine(state.Gamepad);
                    Thread.Sleep(10);
                    previousState = state;
                }
            }
            Debug.WriteLine("End XGamepadApp");
        }

        private void XInputControlTask()
        {
            var controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };
            // Get 1st controller available
            Controller? controller = null;
            foreach (var selectControler in controllers)
            {
                if (selectControler.IsConnected)
                {
                    controller = selectControler;
                    break;
                }
            }

            if (controller == null)
            {
                //Debug.WriteLine("No XInput controller installed");
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("No XInput controller installed.");
                });
            }
            else
            {
                Debug.WriteLine("Found a XInput controller available");
                //Debug.WriteLine("Press buttons on the controller to display events or escape key to exit... ");

                // Poll events from joystick
                var previousState = controller.GetState();
                while (controller.IsConnected && !xInputCancelToken.IsCancellationRequested)
                {
                    var state = controller.GetState();
                    //Debug.WriteLine(state.Gamepad);

                    // 根据手柄输入确定输出的位移量
                    D5RControl.Joints joints = new()
                    {
                        P2 = GamepadThumb2NatorsMap(state.Gamepad.LeftThumbX),
                        P3 = GamepadThumb2NatorsMap(state.Gamepad.LeftThumbY),
                        R1 = GamepadThumb2RMDsMap(state.Gamepad.RightThumbX),
                        R5 = GamepadThumb2RMDsMap(state.Gamepad.RightThumbY)
                    };

                    if (state.Gamepad.LeftTrigger > 10)
                        joints.P4 = -LinearMap(state.Gamepad.LeftTrigger, 10, 255, 100000, 1000000);
                    else if (state.Gamepad.RightTrigger > 10)
                        joints.P4 = LinearMap(state.Gamepad.RightTrigger, 10, 255, 100000, 1000000);

                    Debug.WriteLine($"R1:{joints.R1}, P2:{joints.P2}, P3:{joints.P3}, P4:{joints.P4}, R5:{joints.R5}");
                    if (!jogHandler.isJogging)
                    {
                        int result = D5RControl.JointsMoveRelative(joints);
                        if (result != 0)
                            Dispatcher.Invoke(() => MessageBox.Show("JointsMoveRelative error in xInputControlTask."));
                    }
                    Thread.Sleep(100);
                    previousState = state;
                }
            }
            //Debug.WriteLine("End XGamepadApp");
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A && !jogHandler.isJogging)
            {
                Debug.WriteLine("Key A pressed down.");
                D5RControl.Joints j = new()
                {
                    P2 = -natorJogResolution
                };
                jogHandler.StartJogging(j);
                //jogHandler.TestStartJogging();
            }
            else if (e.Key == Key.D && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    P2 = natorJogResolution
                };
                jogHandler.StartJogging(j);
                //jogHandler.TestStartJogging();
            }
            else if (e.Key == Key.W && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    P3 = natorJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.S && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    P3 = -natorJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.LeftShift && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    P4 = natorJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.LeftCtrl && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    P4 = -natorJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.Q && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    R1 = -RMDJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.R && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    R1 = RMDJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.J && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    R5 = -RMDJogResolution
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.U && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    R5 = RMDJogResolution
                };
                jogHandler.StartJogging(j);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                case Key.D:
                case Key.S:
                case Key.W:
                case Key.Q:
                case Key.E:
                case Key.J:
                case Key.U:
                case Key.LeftShift:
                case Key.LeftCtrl:
                    Debug.WriteLine("Key up");
                    jogHandler.StopJogging();
                    break;
                default:
                    break;
            }
            //if (e.Key == Key.A || e.Key == Key.D)
            //{
            //    Debug.WriteLine("Key up");
            //    jogHandler.StopJogging();
            //}
        }

        private static int GamepadThumb2NatorsMap(int gamepadValue)
        {
            const int inputMin = 10000;
            const int inputMax = 30000;
            const int outputMin = 100000;
            const int outputMax = 1000000;

            int sign = gamepadValue >= 0 ? 1 : -1;
            int gpValueAbs = Math.Abs(gamepadValue);
            int outputValue = LinearMap(gpValueAbs, inputMin, inputMax, outputMin, outputMax);
            //if (gpValueAbs >= inputMin)
            //{
            //    outputValue = (gpValueAbs - inputMin) * (outputMax - outputMin)
            //        / (inputMax - inputMin);
            //}

            return sign * outputValue;
        }

        private static int GamepadThumb2RMDsMap(int gamepadValue)
        {
            const int inputMin = 10000;
            const int inputMax = 30000;
            const int outputMin = 10;
            const int outputMax = 100;

            int sign = gamepadValue >= 0 ? 1 : -1;
            int gpValueAbs = Math.Abs(gamepadValue);
            int outputValue = LinearMap(gpValueAbs, inputMin, inputMax, outputMin, outputMax);

            return sign * outputValue;
        }

        /// <summary>
        /// 将 x 映射到 y，xMin 对应 yMin, xMax 对应 yMax
        /// </summary>
        /// <param name="x">映射的输入</param>
        /// <param name="xMin">输入的最小值</param>
        /// <param name="xMax">输入的最大值</param>
        /// <param name="yMin">输出的最小值</param>
        /// <param name="yMax">输出的最大值</param>
        /// <returns>返回 y 值</returns>
        private static int LinearMap(int x, int xMin, int xMax, int yMin, int yMax)
        {
            int y;

            if (x < xMin)
                y = 0;
            else if (x >= xMax)
                y = yMax;
            else
                y = yMin + (x - xMin) * (yMax - yMin) / (xMax - xMin);

            return y;
        }
    }
}
