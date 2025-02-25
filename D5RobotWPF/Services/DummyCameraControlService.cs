using DOF5RobotControl_GUI.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Services
{
    internal class DummyCameraControlService : ICameraControlService
    {
        private event EventHandler<CamFrame>? TopFrameReceived;
        private event EventHandler<CamFrame>? BottomFrameReceived;
        private CancellationTokenSource? captureCancelSource;               

        public void ConnectCamMotor(string port)
        {
            Debug.WriteLine("Dummy CamMotor connected");
        }

        public void DisconnectCamMotor()
        {
            Debug.WriteLine("Dummy cam motor disconnected");
        }

        public CamFrame GetBottomFrame()
        {
            return GenDummyImg();
        }

        public CamFrame GetTopFrame()
        {
            return GenDummyImg();
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
            Debug.WriteLine("Dummy camera is opened.");
            Task.Run(() =>
            {
                captureCancelSource = new();
                try
                {
                    while (!captureCancelSource.Token.IsCancellationRequested)
                    {
                        TopFrameReceived?.Invoke(this, GenDummyImg());
                        BottomFrameReceived?.Invoke(this, GenDummyImg());
                        Thread.Sleep(1000);
                    }
                }
                finally
                {
                    captureCancelSource.Dispose();
                }
            });
        }

        public void CloseCamera()
        {
            Debug.WriteLine("Dummy camera is closed");
            captureCancelSource?.Cancel();
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
