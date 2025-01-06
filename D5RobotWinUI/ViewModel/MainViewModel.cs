using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D5R;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D5RobotWinUI.Model;
using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;

namespace D5RobotWinUI.ViewModel
{
    internal partial class MainViewModel : ObservableObject
    {
        public static readonly Joints ZeroPos = new(0, 0, 0, 0, 0);
        public static readonly Joints IdlePos = new(0, 0, -10000000, 0, 0);
        public static readonly Joints ChangeJawPos = new(0, -1500000, 8000000, 5000000, 0); // 0, -1.5, 8, 5, 0
        public static readonly Joints PreChangeJawPos = new(0, -1500000, 0, 0, 0);
        public static readonly Joints FetchRingPos = new(0, 10000000, 10000000, 0, 0); // 0, 10, 10, 0, 0
        public static readonly Joints PreFetchRingPos = new(0, 8673000, -15000000, -10000000, 0);
        public static readonly Joints AssemblePos1 = new(0, -600000, 900000, 9000000, 0); // 0, -0.6, 0.9, 9, 0
        public static readonly Joints PreAssemblePos2 = new(9000, 0, 0, 0, 0); // 90, 0, 0, 0, 0 -> 90, 14, -12, 5, 0 
        public static readonly Joints AssemblePos2 = new(9000, 14000000, -12000000, 5000000, 0); // 90, 0, 0, 0, 0 -> 90, 14, -12, 5, 0 
        public static readonly Joints AssemblePos3 = new(0, -2500000, 4000000, 7000000, 0); // 0, -2.5, 4, 7, 0

        public readonly int natorJogResolution = 30000;
        public readonly int RMDJogResolution = 20;
        public readonly MainWindow WindowBelonged;

        readonly string natorId = "usb:id:2250716012";

        private D5Robot? robot;
        CancellationTokenSource? updateStateTaskCancelSource;
        CancellationToken updateStateTaskCancelToken;

        readonly uint jogPeriod = 20;  // ms
        System.Timers.Timer? jogTimer;

        internal VibrateHelper? VibrateHelper;

        public MainViewModel(MainWindow belong)
        {
            WindowBelonged = belong;

            PortsAvailable = SerialPort.GetPortNames();
            if (PortsAvailable.Length > 0)
                SelectedPort = PortsAvailable[0];
            else
                SelectedPort = "";
        }

        ~MainViewModel()
        {
            updateStateTaskCancelSource?.Cancel();
        }

        [ObservableProperty]
        public partial string[] PortsAvailable { get; set; }
        [ObservableProperty]
        public partial string SelectedPort { get; set; }
        [ObservableProperty]
        public partial bool SystemConnected { get; set; } = false;
        [ObservableProperty]
        public partial RoboticState TargetState { get; set; } = new(0, 0, 0, 0, 0);
        [ObservableProperty]
        public partial RoboticState CurrentState { get; set; } = new(0, 0, 0, 0, 0);

        [RelayCommand]
        private void PortRefresh()
        {
            PortsAvailable = SerialPort.GetPortNames();
        }

        [RelayCommand]
        private async Task ToggleConnect()
        {
            if (SystemConnected)  // 如果目前系统已连接，则断开连接
            {
                robot?.Dispose();
                robot = null;
                SystemConnected = false;
                updateStateTaskCancelSource?.Cancel();
                updateStateTaskCancelSource = null;
            }
            else  // 系统未连接，则建立连接
            {
                string portName;
                if (SelectedPort.Length > 4)
                {
                    portName = "\\\\.\\" + SelectedPort;
                }
                else
                {
                    portName = SelectedPort;
                }

                try
                {
                    robot = new D5Robot(portName, natorId, 1, 2);
                    SystemConnected = true;
                    VibrateHelper = new VibrateHelper(robot, TargetState);

                    updateStateTaskCancelSource = new();
                    updateStateTaskCancelToken = updateStateTaskCancelSource.Token;
                    var updateStateTask = Task.Run(UpdateCurrentStateTask, updateStateTaskCancelToken);
                }
                catch (RobotException err)
                {
                    //MessageBox.Show("Error while Connecting: " + err.Code.ToString());
                    //Flyout.ShowAttachedFlyout()
                    ContentDialog dialog = new()
                    {
                        Title = "Error while Connecting",
                        Content = "Error Code: " + err.Code.ToString(),
                        CloseButtonText = "OK",
                        XamlRoot = WindowBelonged.Content.XamlRoot
                    };
                    var result = await dialog.ShowAsync();
                    robot?.Dispose();
                    robot = null;
                    SystemConnected = false;
                    VibrateHelper = null;
                    throw;
                }
            }
        }

        private async void UpdateCurrentStateTask()
        {
            while (robot != null && !updateStateTaskCancelToken.IsCancellationRequested)
            {
                try
                {
                    Joints joints = robot.GetCurrentJoint();
                    await WindowBelonged.Dispatcher.RunIdleAsync((x) =>
                    {
                        CurrentState.SetFromD5RJoints(joints);
                    });
                    //WindowBelonged.Dispatcher.Invoke(() =>
                    //{
                    //    CurrentState.SetFromD5RJoints(joints);
                    //});
                }
                catch (RobotException exc)
                {
                    Debug.WriteLine(exc.Message);
                    if (exc.Code != ErrorCode.RMDFormatError && exc.Code != ErrorCode.SerialSendError)
                        throw;
                }

                Thread.Sleep(1000);
            }
        }
    }
}
