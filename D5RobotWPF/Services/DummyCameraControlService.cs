using DOF5RobotControl_GUI.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Filters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Services
{
    public class DummyCameraControlService : ICameraControlService
    {
        public bool CameraIsOpened { get; private set; }
        public bool CamMotorIsConnected { get; private set; }

        const string MockTopImgPath = "MockTopImage.png";
        const string MockBottomImgPath = "MockBottomImage.png";

        private event EventHandler<CamFrame>? TopFrameReceived;
        private event EventHandler<CamFrame>? BottomFrameReceived;
        private CancellationTokenSource? captureCancelSource;

        private readonly CamFrame lastTopFrame;
        private readonly CamFrame lastBottomFrame;

        public DummyCameraControlService()
        {
            // 读取虚假的图像
            {
                Image<L8> img = Image.Load<L8>(MockTopImgPath); // 顶部图像
                byte[] buffer = new byte[img.Width * img.Height];
                img.CopyPixelDataTo(buffer);
                CamFrame frame = new(buffer, img.Width, img.Height);
                lastTopFrame = frame;
            }

            {
                Image<L8> img = Image.Load<L8>(MockBottomImgPath); // 底部图像
                byte[] buffer = new byte[img.Width * img.Height];
                img.CopyPixelDataTo(buffer);
                CamFrame frame = new(buffer, img.Width, img.Height);
                lastBottomFrame = frame;
            }
        }


        public void ConnectCamMotor(string port)
        {
            Debug.WriteLine("Dummy CamMotor connected");
            CamMotorIsConnected = true;
        }

        public void DisconnectCamMotor()
        {
            Debug.WriteLine("Dummy cam motor disconnected");
            CamMotorIsConnected = false;
        }

        public CamFrame GetBottomFrame()
        {
            return lastBottomFrame;
        }

        public CamFrame GetTopFrame()
        {
            return lastTopFrame;
        }

        public void MoveBottomCamera(int angle)
        {
            throw new NotImplementedException();
        }

        public void MoveTopCamera(int distance)
        {
            throw new NotImplementedException();
        }

        public void OpenCamera()
        {
            Debug.WriteLine("Dummy camera is opening.");
            captureCancelSource = new();
            var token = captureCancelSource.Token;
            Task.Run(() =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        TopFrameReceived?.Invoke(this, GetTopFrame());
                        BottomFrameReceived?.Invoke(this, GetBottomFrame());
                        Thread.Sleep(1000);
                    }
                }
                finally
                {
                    captureCancelSource.Dispose();
                    Debug.WriteLine("captureCancelSource is disposed");
                }
            });

            CameraIsOpened = true;
            Debug.WriteLine("Dummy camera is opened.");
        }

        public void CloseCamera()
        {
            captureCancelSource?.Cancel();
            CameraIsOpened = false;
            Debug.WriteLine("Dummy camera is closed");
        }

        public void RegisterCallback(EventHandler<CamFrame> TopFrameReceivedHandler, EventHandler<CamFrame> BottomFrameReceivedHandler)
        {
            TopFrameReceived += TopFrameReceivedHandler;
            BottomFrameReceived += BottomFrameReceivedHandler;
        }

        public void UnRegisterCallback(EventHandler<CamFrame> TopFrameReceivedHandler, EventHandler<CamFrame> BottomFrameReceivedHandler)
        {
            TopFrameReceived -= TopFrameReceivedHandler;
            BottomFrameReceived -= BottomFrameReceivedHandler;
        }

        private CamFrame GenDummyImg()
        {
            int width = 200;
            int height = 200;
            byte[] buffer = new byte[width * height];
            Random rnd = new();
            rnd.NextBytes(buffer);

            CamFrame frame = new(buffer, width, height);
            return frame;
        }
    }
}
