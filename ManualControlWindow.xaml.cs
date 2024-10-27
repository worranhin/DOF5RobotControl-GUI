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
            while (!captureCancelToken.IsCancellationRequested)
            {
                capture.Read(frame);
                if (frame == null) break;
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
    }
}
