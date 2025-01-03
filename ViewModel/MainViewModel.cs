using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DOF5RobotControl_GUI.Model;
using D5R;
using System.Windows;
using System.Diagnostics;
using System.Windows.Threading;
using Opc.Ua;
using System.Windows.Input;
using System.IO.Ports;

namespace DOF5RobotControl_GUI.ViewModel
{
    public struct JogParams
    {
        public JointSelect Joint {get; set;}
        public bool IsPositive {get; set;}
    };

    partial class MainViewModel : ObservableObject
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
        private JogHandler? jogHandler;
        CancellationTokenSource? updateStateTaskCancelSource;
        CancellationToken updateStateTaskCancelToken;

        readonly uint jogPeriod = 20;  // ms
        System.Timers.Timer? jogTimer;

        //public MainViewModel()
        //{

        //}

        public MainViewModel(MainWindow belong)
        {
            WindowBelonged = belong;
        }

        ~MainViewModel()
        {
            updateStateTaskCancelSource?.Cancel();
        }

        [ObservableProperty]
        private bool _systemConnected = false;
        [ObservableProperty]
        private string[] _portsAvailable = Array.Empty<string>();
        [ObservableProperty]
        private string _selectedPort = "";
        [ObservableProperty]
        private RoboticState _targetState = new(0, 0, 0, 0, 0);
        [ObservableProperty]
        private RoboticState _currentState = new(0, 0, 0, 0, 0);

        /***** Jog 相关 *****/

        public IEnumerable<JogMode> JogModes => Enum.GetValues(typeof(JogMode)).Cast<JogMode>();
        public IEnumerable<JogResolution> JogResolutions => Enum.GetValues(typeof(JogResolution)).Cast<JogResolution>();
        [ObservableProperty]
        private JogMode _jogModeSelected = JogMode.OneStep;
        [ObservableProperty]
        private JogResolution _jogResolutionSelected = JogResolution.Speed1mm;
        
        [RelayCommand]
        private void Jog(JogParams param)
        {
            if (JogModeSelected == JogMode.OneStep)
            {
                Debug.WriteLine($"{param.Joint}, {param.IsPositive}, {JogModeSelected}, {JogResolutionSelected}");

                double resolution = 0;
                switch (JogResolutionSelected)
                {
                    case JogResolution.Speed1mm:
                        resolution = 1;
                        break;
                    case JogResolution.Speed100um:
                        resolution = 0.1;
                        break;
                    case JogResolution.Speed10um:
                        resolution = 0.01;
                        break;
                    default:
                        Debug.WriteLine("Invalid JogResolutionSelected");
                        break;
                }

                switch(param.Joint)
                {
                    case JointSelect.R1:
                        TargetState.JointSpace.R1 += param.IsPositive ? resolution : -resolution;
                        break;
                    case JointSelect.P2:
                        TargetState.JointSpace.P2 += param.IsPositive ? resolution : -resolution;
                        break;
                    case JointSelect.P3:
                        TargetState.JointSpace.P3 += param.IsPositive ? resolution : -resolution;
                        break;
                    case JointSelect.P4:
                        TargetState.JointSpace.P4 += param.IsPositive ? resolution : -resolution;
                        break;
                    case JointSelect.R5:
                        TargetState.JointSpace.R5 += param.IsPositive ? resolution : -resolution;
                        break;
                    default:
                        Debug.WriteLine("Invalid JointSelect");
                        break;
                }

                robot?.JointsMoveAbsolute(TargetState.ToD5RJoints());
            }
            //TargetState.JointSpace.P2 += 0.1;
        }

        public void StartJogContinuous(JogParams param)
        {
            if (JogModeSelected != JogMode.Continuous)
            {
                return;
            }

            double resolution = 0;
            switch (JogResolutionSelected)
            {
                case JogResolution.Speed1mm:
                    resolution = 1;
                    break;
                case JogResolution.Speed100um:
                    resolution = 0.1;
                    break;
                case JogResolution.Speed10um:
                    resolution = 0.01;
                    break;
                default:
                    break;
            }

            if (!param.IsPositive)
                resolution = -resolution;  // 每秒步进量

            jogTimer = new(jogPeriod);
            resolution = resolution * jogPeriod / 1000;  // 每次控制的步进量

            switch (param.Joint)
            {
                case JointSelect.R1:
                    jogTimer.Elapsed += (source, e) =>
                    {
                        TargetState.JointSpace.R1 += resolution;
                        RobotRun();
                    };
                    break;
                case JointSelect.P2:
                    jogTimer.Elapsed += (source, e) =>
                    {
                        TargetState.JointSpace.P2 += resolution;
                        RobotRun();
                    };
                    break;
                case JointSelect.P3:
                    jogTimer.Elapsed += (source, e) =>
                    {
                        TargetState.JointSpace.P3 += resolution;
                        RobotRun();
                    };
                    break;
                case JointSelect.P4:
                    jogTimer.Elapsed += (source, e) =>
                    {
                        TargetState.JointSpace.P4 += resolution;
                        RobotRun();
                    };
                    break;
                case JointSelect.R5:
                    jogTimer.Elapsed += (source, e) =>
                    {
                        TargetState.JointSpace.R5 += resolution;
                        RobotRun();
                    };
                    break;
                default:
                    break;
            }

            jogTimer.Start();
        }

        public void StopJogContinuous()
        {
            jogTimer?.Stop();
            jogTimer = null;
        }

        /***** Jog 相关结束 *****/

        [RelayCommand]
        private void SetTargetJoints(Joints joints)
        {
            TargetState.SetFromD5RJoints(joints);
        }

        [RelayCommand]
        private void SetTargetJointsFromCurrent()
        {
            var joints = CurrentState.ToD5RJoints();
            TargetState.SetFromD5RJoints(joints);
        }

        ///// 处理振动相关 UI 逻辑 /////

        internal VibrateHelper? VibrateHelper;
        [ObservableProperty]
        private bool _isVibrating = false;
        [RelayCommand]
        private void ToggleVibrate()
        {
            if (VibrateHelper == null)
            {
                MessageBox.Show("While toggle vibrate: vibrateHelper is null!");
                return;
            }

            if (!IsVibrating)
            {
                VibrateHelper.Start();
                IsVibrating = true;
            }
            else
            {
                VibrateHelper.Stop();
                IsVibrating = false;
            }
        }

        ///// 处理振动结束 /////

        [RelayCommand]
        private void ToggleConnect()
        {
            if (SystemConnected)  // 如果目前系统已连接
            {
                robot?.Dispose();
                robot = null;
                jogHandler = null;
                SystemConnected = false;
                updateStateTaskCancelSource?.Cancel();
                updateStateTaskCancelSource = null;
            }
            else  // 系统未连接
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
                    jogHandler = new JogHandler(robot, TargetState);
                    SystemConnected = true;
                    VibrateHelper = new VibrateHelper(robot, TargetState);

                    updateStateTaskCancelSource = new();
                    updateStateTaskCancelToken = updateStateTaskCancelSource.Token;
                    Task.Run(UpdateCurrentStateTask, updateStateTaskCancelToken);
                }
                catch (RobotException err)
                {
                    MessageBox.Show("Error while Connecting: " + err.Code.ToString());
                    robot?.Dispose();
                    robot = null;
                    jogHandler = null;
                    SystemConnected = false;
                    VibrateHelper = null;
                    throw;
                }
            }
        }

        [RelayCommand]
        private void PortRefresh()
        {
            PortsAvailable = SerialPort.GetPortNames();
        }

        [RelayCommand]
        private void RobotRun()
        {
            if (robot == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            Joints j = TargetState.ToD5RJoints();
            try
            {
                robot.JointsMoveAbsolute(j);
            }
            catch (RobotException exc)
            {
                MessageBox.Show($"Jog error while running: {exc.Code}");
            }
        }

        [RelayCommand]
        private void RobotStop()
        {
            if (robot == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            if (!robot.Stop())
            {
                MessageBox.Show($"Error while stopping.");
                return;
            }
        }

        [RelayCommand]
        private void RobotSetZero()
        {
            if (robot == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            if (!robot.SetZero())
            {
                MessageBox.Show($"Error while setting zero.");
                return;
            }
        }

        [RelayCommand]
        private void OpenCamera()
        {
            if (robot == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            ManualControlWindow window = new(robot, TargetState);
            window.Show();
        }

        /***** TODO: 将下面的函数修改为 Command 模式 结束 *****/

        /***** OPC 相关代码 *****/

        public void OpcMapMethod(int method)
        {
            // 创建一个 EventArgs 实例来传递给 ButtonClicked 方法
            switch (method)
            {
                case 1: PortRefresh(); break;
                case 2: ToggleConnectCommand.Execute(null); break;
                case 3: SetTargetJointsCommand.Execute(ZeroPos); break;
                case 4: SetTargetJointsCommand.Execute(IdlePos); break;
                case 5: SetTargetJointsCommand.Execute(PreChangeJawPos); break;
                case 6: SetTargetJointsCommand.Execute(ChangeJawPos); break;
                case 7: SetTargetJointsCommand.Execute(AssemblePos1); break;
                case 8: SetTargetJointsCommand.Execute(AssemblePos2); break;
                case 9: SetTargetJointsCommand.Execute(AssemblePos3); break;
                case 10: SetTargetJointsCommand.Execute(PreFetchRingPos); break;
                case 11: SetTargetJointsCommand.Execute(FetchRingPos); break;
                case 12: RobotRun(); break;
                case 13: RobotStop(); break;
                case 14: RobotSetZero(); break;
            }

            // 备份，对照
            //switch (method)
            //{
            //    case 1: PortRefresh_Click(this, args); break;
            //    case 2: BtnConnect_Click(this, args); break;
            //    case 3: BtnZeroPos_Click(this. args); break;
            //    case 4: BtnIdlePos_Click(this, args); break;
            //    case 5: BtnPreChangeJawPos_Click(this, args); break;
            //    case 6: BtnChangeJawPos_Click(this, args); break;
            //    case 7: BtnAssemblePos1_Click(this, args); break;
            //    case 8: BtnAssemblePos2_Click(this, args); break;
            //    case 9: BtnAssemblePos3_Click(this, args); break;
            //    case 10: BtnPreFetchRingPos_Click(this, args); break;
            //    case 11: BtnFetchRingPos_Click(this, args); break;
            //    case 12: BtnRun_Click(this, args); break;
            //    case 13: BtnStop_Click(this, args); break;
            //    case 14: BtnSetZero_Click(this, args); break;
            //}
        }

        /***** OPC 相关代码结束 *****/

        /***** 任务相关代码 *****/

        private void UpdateCurrentStateTask()
        {
            while (robot != null && !updateStateTaskCancelToken.IsCancellationRequested)
            {
                try
                {
                    Joints joints = (Joints)robot.GetCurrentJoint();
                    WindowBelonged.Dispatcher.Invoke(() =>
                    {
                        CurrentState.SetFromD5RJoints(joints);
                    });
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

        /***** 任务相关代码结束 *****/
    }
}
