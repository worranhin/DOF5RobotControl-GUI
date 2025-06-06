﻿namespace DOF5RobotControl_GUI.Services
{
    public interface ICamMotorControlService
    {
        bool IsConnected { get; }

        void Connect(string port);
        void Disconnect();
        void MoveRelativeRight(CamMotorControlService.MotorSelect id, int data);
        void MoveRelativeLeft(CamMotorControlService.MotorSelect id, int data);
        void MoveStepLeft(CamMotorControlService.MotorSelect id);
        void MoveStepRight(CamMotorControlService.MotorSelect id);
    }
}