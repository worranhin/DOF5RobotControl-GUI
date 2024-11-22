using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.ViewModel;
using Window = System.Windows.Window;
using Joints = DOF5RobotControl_GUI.Model.D5Robot.Joints;

namespace DOF5RobotControl_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        readonly Joints ZeroPos = new(0, 0, 0, 0, 0);
        readonly Joints IdlePos = new(0, 0, -14000000, -10000000, 0);
        readonly Joints ChangeJawPos = new(0, -72195, 5174842, -6912012, 0);
        readonly Joints PreChangeJawPos = new(0, 5000000, -5000000, -15184980, 0);
        readonly Joints FetchRingPos = new(0, 8673000, 4000000, -10000000, 0);
        readonly Joints PreFetchRingPos = new(0, 8673000, -15000000, -10000000, 0);
        readonly Joints AssemblePos1 = new(9000, 15686500, -16819200, -5759600, -10);
        readonly Joints AssemblePos2 = new(6000, -8027000, -15911400, 1783100, 0);
        readonly Joints AssemblePos3 = new(0, 0, 7004200, 15275000, 0);

        //private readonly JointsPosition ZeroPos = new(0, 0, 0, 0, 0);
        //private readonly JointsPosition IdlePos = new(0, 0, -15000000, -10000000, 0);
        //private readonly JointsPosition ChangeJawPos = new(0, -72195, 5174842, -6912012, 0);
        //private readonly JointsPosition PreChangeJawPos = new(0, -72195, -15000000, -6912012, 0);
        //private readonly JointsPosition FetchRingPos = new(0, 8673000, 4000000, -10000000, 0);
        //private readonly JointsPosition PreFetchRingPos = new(0, 8673000, -15000000, -10000000, 0);
        //private readonly JointsPosition AssemblePos1 = new(9000, 15686500, -16819200, -5759600, -10);
        //private readonly JointsPosition AssemblePos2 = new(6000, -8027000, -15911400, 1783100, 0);
        //private readonly JointsPosition AssemblePos3 = new(0, 0, 7004200, 15275000, 0);

        readonly int natorJogResolution = 100000;
        readonly int RMDJogResolution = 20;
        readonly string natorId = "usb:id:7547982319";
        private readonly MainViewModel viewModel = new();
        private D5Robot? robot;
        private JogHandler? jogHandler;

        public MainWindow()
        {
            InitializeComponent();

            // 初始化 Serial
            viewModel.PortsAvailable = SerialPort.GetPortNames();
            if (viewModel.PortsAvailable.Length > 0)
                viewModel.SelectedPort = viewModel.PortsAvailable[0];

            DataContext = viewModel;
        }

        private void PortRefresh_Click(object sender, RoutedEventArgs e)
        {
            viewModel.PortsAvailable = SerialPort.GetPortNames();
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.SystemConnected)  // 如果目前系统已连接
            {
                robot?.Dispose();
                robot = null;
                jogHandler = null;
                viewModel.SystemConnected = false;
            }
            else  // 系统未连接
            {
                string portName;
                if (viewModel.SelectedPort.Length > 4)
                {
                    portName = "\\\\.\\" + viewModel.SelectedPort;
                }
                else
                {
                    portName = viewModel.SelectedPort;
                }

                try
                {
                    robot = new D5Robot(portName, natorId, 1, 2);
                    jogHandler = new JogHandler(robot);
                    viewModel.SystemConnected = true;
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                    robot?.Dispose();
                    robot = null;
                    jogHandler = null;
                    viewModel.SystemConnected = false;
                }
            }
        }

        // 预设位姿按键 //

        private void BtnZeroPos_Click(object sender, RoutedEventArgs e)
        {
            viewModel.TargetState.SetFromD5RJoints(ZeroPos);
        }

        private void BtnIdlePos_Click(object sender, RoutedEventArgs e)
        {
            viewModel.TargetState.SetFromD5RJoints(IdlePos);
        }

        private void BtnPreChangeJawPos_Click(object sender, RoutedEventArgs e)
        {
            viewModel.TargetState.SetFromD5RJoints(PreChangeJawPos);
        }

        private void BtnChangeJawPos_Click(object sender, RoutedEventArgs e)
        {
            viewModel.TargetState.SetFromD5RJoints(ChangeJawPos);
        }

        private void BtnAssemblePos1_Click(object sender, RoutedEventArgs e)
        {
            viewModel.TargetState.SetFromD5RJoints(AssemblePos1);
        }

        private void BtnAssemblePos2_Click(object sender, RoutedEventArgs e)
        {
            viewModel.TargetState.SetFromD5RJoints(AssemblePos2);
        }

        private void BtnAssemblePos3_Click(object sender, RoutedEventArgs e)
        {
            viewModel.TargetState.SetFromD5RJoints(AssemblePos3);
        }

        private void BtnPreFetchRingPos_Click(object sender, RoutedEventArgs e)
        {
            viewModel.TargetState.SetFromD5RJoints(PreFetchRingPos);
        }

        private void BtnFetchRingPos_Click(object sender, RoutedEventArgs e)
        {
            viewModel.TargetState.SetFromD5RJoints(FetchRingPos);
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (robot == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            Joints j = viewModel.TargetState.ToD5RJoints();
            D5Robot.ErrorCode err = robot.JointsMoveAbsolute(j);

            if (err != D5Robot.ErrorCode.OK)
            {
                MessageBox.Show($"Error while running: {err}");
                return;
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (robot == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            var err = robot.Stop();
            if (err != D5Robot.ErrorCode.OK)
            {
                MessageBox.Show($"Error while stopping: {err}");
                return;
            }
        }

        private void BtnSetZero_Click(object sender, RoutedEventArgs e)
        {
            if (robot == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            var err = robot.SetZero();
            if (err != D5Robot.ErrorCode.OK)
            {
                MessageBox.Show($"Error while setting zero: {err}");
                return;
            }
        }

        // R1 jogging button callbacks //

        private void BtnR1JogDown_N(object sender, MouseButtonEventArgs e)
        {
            if (jogHandler == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            Joints joints = new(-RMDJogResolution, 0, 0, 0, 0);
            jogHandler?.StartJogging(joints);
        }

        private void BtnR1JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler?.StopJogging();
        }

        private void BtnR1JogDown_P(object sender, MouseButtonEventArgs e)
        {
            Joints joints = new(RMDJogResolution, 0, 0, 0, 0);
            jogHandler?.StartJogging(joints);
        }

        private void BtnR1JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler?.StopJogging();
        }

        // P2 jogging button callbacks //

        private void BtnP2JogDown_N(object sender, MouseButtonEventArgs e)
        {
            Joints joints = new(0, -natorJogResolution, 0, 0, 0);
            jogHandler?.StartJogging(joints);
        }

        private void BtnP2JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler?.StopJogging();
        }

        private void BtnP2JogDown_P(object sender, MouseButtonEventArgs e)
        {
            Joints joints = new(0, natorJogResolution, 0, 0, 0);
            jogHandler?.StartJogging(joints);
        }

        private void BtnP2JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler?.StopJogging();
        }

        // P3 jogging button callbacks //

        private void BtnP3JogDown_N(object sender, MouseButtonEventArgs e)
        {
            Joints joints = new(0, 0, -natorJogResolution, 0, 0);
            jogHandler?.StartJogging(joints);
        }

        private void BtnP3JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler?.StopJogging();
        }

        private void BtnP3JogDown_P(object sender, MouseButtonEventArgs e)
        {
            Joints joints = new(0, 0, natorJogResolution, 0, 0);
            jogHandler?.StartJogging(joints);
        }

        private void BtnP3JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler?.StopJogging();
        }

        // P4 jogging button callbacks //

        private void BtnP4JogDown_N(object sender, MouseButtonEventArgs e)
        {
            Joints joints = new(0, 0, 0, -natorJogResolution, 0);
            jogHandler?.StartJogging(joints);
        }

        private void BtnP4JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler?.StopJogging();
        }

        private void BtnP4JogDown_P(object sender, MouseButtonEventArgs e)
        {
            Joints joints = new(0, 0, 0, natorJogResolution, 0);
            jogHandler?.StartJogging(joints);
        }

        private void BtnP4JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler?.StopJogging();
        }

        // R5 jogging button callbacks //

        private void BtnR5JogDown_N(object sender, MouseButtonEventArgs e)
        {
            Joints joints = new(0, 0, 0, 0, -RMDJogResolution);
            jogHandler?.StartJogging(joints);
        }

        private void BtnR5JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler?.StopJogging();
        }

        private void BtnR5JogDown_P(object sender, MouseButtonEventArgs e)
        {
            Joints joints = new(0, 0, 0, 0, RMDJogResolution);
            jogHandler?.StartJogging(joints);
        }

        private void BtnR5JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler?.StopJogging();
        }

        private void BtnOpenManualControl_Click(object sender, RoutedEventArgs e)
        {
            if (robot == null)
            {
                MessageBox.Show("Robot not connected.");
                return;
            }

            ManualControlWindow window = new(robot);
            window.Show();
        }
    }
}