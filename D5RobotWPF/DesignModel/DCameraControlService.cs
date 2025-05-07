using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.DesignModel
{
    class DCameraControlService : ICameraControlService
    {
        public bool CameraIsOpened => false;

        public bool CamMotorIsConnected => false;

        public void CloseCamera()
        {
            throw new NotImplementedException();
        }

        public void ConnectCamMotor(string port)
        {
            throw new NotImplementedException();
        }

        public void DisconnectCamMotor()
        {
            throw new NotImplementedException();
        }

        public CamFrame GetBottomFrame()
        {
            throw new NotImplementedException();
        }

        public CamFrame GetTopFrame()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void RegisterCallback(EventHandler<CamFrame> TopFrameReceivedHandler, EventHandler<CamFrame> BottomFrameReceivedHandler)
        {
            throw new NotImplementedException();
        }

        public void UnRegisterCallback(EventHandler<CamFrame> TopFrameReceivedHandler, EventHandler<CamFrame> BottomFrameReceivedHandler)
        {
            throw new NotImplementedException();
        }
    }
}
