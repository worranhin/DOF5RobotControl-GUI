using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;


//using OpenCvSharp.WpfExtensions;
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

namespace DOF5RobotControl_GUI
{
    /// <summary>
    /// ManualControlWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ManualControlWindow : System.Windows.Window
    {
        private VideoCapture capture;
        private Mat frame;
        CancellationTokenSource captureCancelSource;
        CancellationToken captureCancelToken;
        readonly JogHandler jogHandler = new();

        public ManualControlWindow()
        {
            InitializeComponent();
            this.Closed += WindowClosed;

            capture = new VideoCapture(0);
            frame = new Mat();
            captureCancelSource = new();
            captureCancelToken = captureCancelSource.Token;
            Task.Run(CaptureCamera, captureCancelToken);
        }

        private void WindowClosed(object? sender, EventArgs e)
        {
            captureCancelSource.Cancel();
        }

        private void CaptureCamera()
        {
            int retrys = 0;

            while (!captureCancelToken.IsCancellationRequested)
            {
                bool hasFrame = capture.Read(frame);
                if (!hasFrame)
                {
                    if (retrys++ >= 10)
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    P2 = -100
                };
                jogHandler.StartJogging(j);
                //jogHandler.TestStartJogging();
            } else if(e.Key == Key.D && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    P2 = 100
                };
                jogHandler.StartJogging(j);
                //jogHandler.TestStartJogging();
            }
            else if (e.Key == Key.W && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    P3 = 100
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.S && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    P3 = -100
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.LeftShift && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    P4 = 100
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.LeftCtrl && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    P4 = -100
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.Q && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    R1 = -100
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.R && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    R1 = 100
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.J && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    R5 = -100
                };
                jogHandler.StartJogging(j);
            }
            else if (e.Key == Key.U && !jogHandler.isJogging)
            {
                D5RControl.Joints j = new()
                {
                    R5 = 100
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
    }
}
