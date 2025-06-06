﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO.Ports;

namespace DOF5RobotControl_GUI.ViewModel
{
    internal partial class ConfigViewModel : ObservableObject
    {
        public ConfigViewModel()
        {
            PortsAvailable = SerialPort.GetPortNames();
            if (PortsAvailable.Length > 0)
            {
                if (PortsAvailable.Contains(Properties.Settings.Default.RmdPort))
                    RmdPort = Properties.Settings.Default.RmdPort;
                else
                    RmdPort = PortsAvailable[0];

                if (PortsAvailable.Contains(Properties.Settings.Default.CamMotorPort))
                    CamMotorPort = Properties.Settings.Default.CamMotorPort;
                else
                    CamMotorPort = PortsAvailable[0];

                PropertiesNotSaved = false;
            }
        }

        public bool PropertiesNotSaved { get; private set; } = false;

        [ObservableProperty]
        private string[] portsAvailable = [];

        [ObservableProperty]
        private string rmdPort = string.Empty;
        partial void OnRmdPortChanged(string value)
        {
            PropertiesNotSaved = true;
        }

        [ObservableProperty]
        private string camMotorPort = string.Empty;
        partial void OnCamMotorPortChanged(string value)
        {
            PropertiesNotSaved = true;
        }

        /// <summary>
        /// 刷新端口
        /// </summary>
        [RelayCommand]
        private void PortRefresh()
        {
            PortsAvailable = SerialPort.GetPortNames();
        }

        /// <summary>
        /// 保存配置到用户端本地
        /// </summary>
        [RelayCommand]
        private void SaveProperties()
        {
            Properties.Settings.Default.RmdPort = RmdPort;
            Properties.Settings.Default.CamMotorPort = CamMotorPort;
            Properties.Settings.Default.Save();
            PropertiesNotSaved = false;
        }
    }
}
