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
using System.Media;
using System.Runtime.CompilerServices;

namespace DOF5RobotControl_GUI
{
    /// <summary>
    /// ManualControlWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ManualControlWindow : System.Windows.Window
    {
        private readonly static SoundPlayer lowPlayer = new("res/Low.wav");
        private readonly static SoundPlayer mediumPlayer = new("res/Medium.wav");
        private readonly static SoundPlayer highPlayer = new("res/High.wav");
        private readonly static int controlPeriod = 20;  // ms
        private readonly CancellationTokenSource captureCancelSource;
        private readonly VideoCapture capture;
        private readonly Mat frame;
        private readonly CancellationTokenSource xInputCancelSource;
        private readonly CancellationToken captureCancelToken;
        private readonly CancellationToken xInputCancelToken;
        private readonly JogHandler jogHandler = new();
        private readonly int natorJogResolution = 100000;
        private readonly int RMDJogResolution = 20;
        private int speedLevel = 0;

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

            // 运行两个 Task
            Task.Run(CaptureCameraTask, captureCancelToken);
            Task.Run(XInputControlTask, xInputCancelToken);
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
            const int ThumbsThreshold = 15000;
            const int TrigerThreshold = 120;

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
                Dispatcher.Invoke(() => MessageBox.Show("No XInput controller installed."));
            }
            else
            {
                Debug.WriteLine("Found a XInput controller available");

                //Debug.WriteLine("Press buttons on the controller to display events or escape key to exit... ");

                // Poll events from joystick
                while (controller.IsConnected && !xInputCancelToken.IsCancellationRequested)
                {
                    var state = controller.GetState();

                    // 判断是否切换速度
                    var res = controller.GetKeystroke(DeviceQueryType.Gamepad, out Keystroke ks);
                    //Debug.WriteLine($"result:{res}");
                    //Debug.WriteLine($"Flags:{ks.Flags}, VirtualKey:{ks.VirtualKey}");
                    if (res == 0 && ks.Flags == KeyStrokeFlags.KeyDown)
                    {
                        if (ks.VirtualKey == GamepadKeyCode.DPadUp)
                        {
                            speedLevel++;
                            speedLevel = speedLevel > 2 ? 2 : speedLevel;
                            PlaySound(speedLevel);
                            continue;
                        }
                        else if (ks.VirtualKey == GamepadKeyCode.DPadDown)
                        {
                            speedLevel--;
                            speedLevel = speedLevel < 0 ? 0 : speedLevel;
                            PlaySound(speedLevel);
                            continue;
                        }
                    }

                    // 根据手柄输入确定输出的位移量
                    D5RControl.Joints joints = new();

                    if (state.Gamepad.RightThumbX <= -ThumbsThreshold)
                        joints.R1 = SelectRMDSpeed(speedLevel);
                    else if (state.Gamepad.RightThumbX >= ThumbsThreshold)
                        joints.R1 = -SelectRMDSpeed(speedLevel);

                    if (state.Gamepad.LeftThumbX <= -ThumbsThreshold)
                        joints.P2 = SelectNatorSpeed(speedLevel);
                    else if (state.Gamepad.LeftThumbX >= ThumbsThreshold)
                        joints.P2 = -SelectNatorSpeed(speedLevel);

                    if (state.Gamepad.LeftThumbY <= -ThumbsThreshold)
                        joints.P3 = -SelectNatorSpeed(speedLevel);
                    else if (state.Gamepad.LeftThumbY >= ThumbsThreshold)
                        joints.P3 = SelectNatorSpeed(speedLevel);

                    if (state.Gamepad.LeftTrigger >= TrigerThreshold)
                        joints.P4 = -SelectNatorSpeed(speedLevel);
                    else if (state.Gamepad.RightTrigger >= TrigerThreshold)
                        joints.P4 = SelectNatorSpeed(speedLevel);

                    if (state.Gamepad.RightThumbY <= -ThumbsThreshold)
                        joints.R5 = -SelectRMDSpeed(speedLevel);
                    else if (state.Gamepad.RightThumbY >= ThumbsThreshold)
                        joints.R5 = SelectRMDSpeed(speedLevel);

                    Debug.WriteLine($"R1:{joints.R1}, P2:{joints.P2}, P3:{joints.P3}, P4:{joints.P4}, R5:{joints.R5}");
                    if (!jogHandler.isJogging)
                    {
                        int result = D5RControl.JointsMoveRelative(joints);
                        if (result != 0)
                        {
                            Dispatcher.Invoke(() => MessageBox.Show("JointsMoveRelative error in xInputControlTask."));
                            break;
                        }
                    }
                    Thread.Sleep(controlPeriod);  // 控制周期
                }

                if (!xInputCancelToken.IsCancellationRequested)
                {
                    Dispatcher.Invoke(() => MessageBox.Show("Gamepad disconnected!"));
                    xInputCancelSource.Cancel();
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

        private static int SelectNatorSpeed(int level)
        {
            int value = 0;
            switch (level)
            {
                case 0:
                    value = 10000 / 1000 * controlPeriod;  // 0.01 mm/s
                    break;
                case 1:
                    value = 100000 / 1000 * controlPeriod;  // 0.1mm/s
                    break;
                case 2:
                    value = 1000000 / 1000 * controlPeriod;  // 1 mm/s
                    break;
                default:
                    break;
            }

            return value;
        }

        private static int SelectRMDSpeed(int level)
        {
            int value = 0;
            switch (level)
            {
                case 0:
                    value = 1 / 1000 * controlPeriod;  // 0.01 degree/s
                    break;
                case 1:
                    value = 10 / 1000 * controlPeriod; // 0.1 degree/s
                    break;
                case 2:
                    value = 100 / 1000 * controlPeriod;  // 1 degree/s
                    break;
                default:
                    break;
            }

            return value;
        }

        private static void PlaySound(int level)
        {
            switch (level)
            {
                case 0:
                    lowPlayer.Play();
                    break;
                case 1:
                    mediumPlayer.Play();
                    break;
                case 2:
                    highPlayer.Play();
                    break;
                default:
                    break;
            }
        }
    }
}
