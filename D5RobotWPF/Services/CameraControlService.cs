using DOF5RobotControl_GUI.Model;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Services
{
    public class CameraControlService : ICameraControlService
    {
        private readonly ICamMotorControlService _camMotorCtrlService;

        public bool CameraIsOpened { get; private set; } = false;
        public bool CamMotorIsConnected { get; private set; } = false;

        public CameraControlService(ICamMotorControlService camMotorCtrlService)
        {
            _camMotorCtrlService = camMotorCtrlService;
        }

        public void OpenCamera()
        {
            TopCamera.Instance.Open(true);
            BottomCamera.Instance.Open(true);
            CameraIsOpened = true;
        }

        public void CloseCamera()
        {
            TopCamera.Instance.Close();
            BottomCamera.Instance.Close();
            CameraIsOpened = false;
        }

        /// <summary>
        /// 注册相机接收到帧的回调函数
        /// </summary>
        /// <param name="TopFrameReceivedHandler">顶部相机接收帧时的回调函数</param>
        /// <param name="BottomFrameReceivedHandler">底部相机接收帧时的回调函数</param>
        public void RegisterCallback(EventHandler<CamFrame> TopFrameReceivedHandler, EventHandler<CamFrame> BottomFrameReceivedHandler)
        {
            TopCamera.Instance.FrameReceived += TopFrameReceivedHandler;
            BottomCamera.Instance.FrameReceived += BottomFrameReceivedHandler;
        }

        public void UnRegisterCallback(EventHandler<CamFrame> TopFrameReceivedHandler, EventHandler<CamFrame> BottomFrameReceivedHandler)
        {
            TopCamera.Instance.FrameReceived -= TopFrameReceivedHandler;
            BottomCamera.Instance.FrameReceived -= BottomFrameReceivedHandler;
        }

        public CamFrame GetTopFrame()
        {
            return TopCamera.Instance.LastFrame;
        }

        public CamFrame GetBottomFrame()
        {
            return BottomCamera.Instance.LastFrame;
        }

        public void ConnectCamMotor(string port)
        {
            if (!_camMotorCtrlService.IsConnected)
            {
                _camMotorCtrlService.Connect(port);
                CamMotorIsConnected = true;
            }
        }

        public void DisconnectCamMotor()
        {
            _camMotorCtrlService.Disconnect();
            CamMotorIsConnected = false;
        }

        /// <summary>
        /// 移动顶部相机
        /// </summary>
        /// <param name="distance">移动的距离，正为向右，负为向左，单位 mm</param>
        public void MoveTopCamera(int distance)
        {
            if (distance > 0)
                _camMotorCtrlService.MoveRelativeRight(CamMotorControlService.MotorSelect.Top, distance);
            else if (distance < 0)
                _camMotorCtrlService.MoveRelativeLeft(CamMotorControlService.MotorSelect.Top, -distance);
        }

        /// <summary>
        /// 移动底部相机
        /// </summary>
        /// <param name="angle">移动的角度，正为向右，负为向左，单位 ？</param>
        public void MoveBottomCamera(int angle)
        {
            if (angle > 0)
                _camMotorCtrlService.MoveRelativeRight(CamMotorControlService.MotorSelect.Bottom, angle);
            else if (angle < 0)
                _camMotorCtrlService.MoveRelativeLeft(CamMotorControlService.MotorSelect.Bottom, -angle);
        }
    }
}
