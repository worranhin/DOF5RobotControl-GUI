using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Forms;
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
using System.Threading;
using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Server;
using Opc.UaFx.Services;
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
        readonly Joints IdlePos = new(0, 0, -10000000, 0, 0);
        readonly Joints ChangeJawPos = new(0, -1500000, 8000000, 5000000, 0); // 0, -1.5, 8, 5, 0
        readonly Joints PreChangeJawPos = new(0, -1500000, 0, 0, 0);
        readonly Joints FetchRingPos = new(0, 10000000, 10000000, 0, 0); // 0, 10, 10, 0, 0
        readonly Joints PreFetchRingPos = new(0, 8673000, -15000000, -10000000, 0);
        readonly Joints AssemblePos1 = new(0, -600000, 900000, 9000000, 0); // 0, -0.6, 0.9, 9, 0
        readonly Joints PreAssemblePos2 = new(9000, 0, 0, 0, 0); // 90, 0, 0, 0, 0 -> 90, 14, -12, 5, 0 
        readonly Joints AssemblePos2 = new(9000, 14000000, -12000000, 5000000, 0); // 90, 0, 0, 0, 0 -> 90, 14, -12, 5, 0 
        readonly Joints AssemblePos3 = new(0, -2500000, 4000000, 7000000, 0); // 0, -2.5, 4, 7, 0

        //private readonly JointsPosition ZeroPos = new(0, 0, 0, 0, 0);
        //private readonly JointsPosition IdlePos = new(0, 0, -15000000, -10000000, 0);
        //private readonly JointsPosition ChangeJawPos = new(0, -72195, 5174842, -6912012, 0);
        //private readonly JointsPosition PreChangeJawPos = new(0, -72195, -15000000, -6912012, 0);
        //private readonly JointsPosition FetchRingPos = new(0, 8673000, 4000000, -10000000, 0);
        //private readonly JointsPosition PreFetchRingPos = new(0, 8673000, -15000000, -10000000, 0);
        //private readonly JointsPosition AssemblePos1 = new(9000, 15686500, -16819200, -5759600, -10);
        //private readonly JointsPosition AssemblePos2 = new(6000, -8027000, -15911400, 1783100, 0);
        //private readonly JointsPosition AssemblePos3 = new(0, 0, 7004200, 15275000, 0);

        readonly int natorJogResolution = 30000;
        readonly int RMDJogResolution = 20;
        //readonly string natorId = "usb:id:7547982319";
        readonly string natorId = "usb:id:2250716012";
        internal readonly MainViewModel viewModel = new();
        private D5Robot? robot;
        private JogHandler? jogHandler;
        Thread serverThread;
        CancellationTokenSource opcTaskCancelSource;
        CancellationToken opcTaskCancelToken;
        //static public double[] joints100;

        public MainWindow()
        {
            InitializeComponent();

            // 初始化 Serial
            viewModel.PortsAvailable = SerialPort.GetPortNames();
            if (viewModel.PortsAvailable.Length > 0)
                viewModel.SelectedPort = viewModel.PortsAvailable[0];
            
            // 初始化 ViewModel
            DataContext = viewModel;

            // 初始化 OPC
            opcTaskCancelSource = new();
            opcTaskCancelToken = opcTaskCancelSource.Token;
            serverThread = new(ServerRunTask);

            // 注册窗口关闭回调函数
            this.Closed += Window_Closed;

            //joints100 = new double[5] { viewModel.TargetState.JointSpace.R1, viewModel.TargetState.JointSpace.P2, viewModel.TargetState.JointSpace.P3, viewModel.TargetState.JointSpace.P4, viewModel.TargetState.JointSpace.R5 };
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            Debug.WriteLine("Window closed");
            opcTaskCancelSource.Cancel();
        }

        private void ServerRunTask()
        {
            var dof5robotInstance = new D5RobotOpcNodeManager(this);

            //var test = new MyNodeManager();
            using (var server = new OpcServer("opc.tcp://localhost:4840", dof5robotInstance))//server以nodeManager初始化
            {
                //服务器配置
                server.Configuration = OpcApplicationConfiguration.LoadServerConfig("Opc.UaFx.Server");
                server.ApplicationName = "DOF5ROBOT";//应用名称
                server.Start();
                Random rd = new Random();
                while (!opcTaskCancelToken.IsCancellationRequested)
                {
                    int i = rd.Next();

                    Thread.Sleep(1000);
                }
                server.Stop();
            }
        }

        public void SomeOtherMethod(int method)
        {
            // 创建一个 EventArgs 实例来传递给 ButtonClicked 方法
            RoutedEventArgs args = new();
            switch (method)
            {
                case 1: PortRefresh_Click(this, args); break;
                case 2: BtnConnect_Click(this, args); break;
                case 3: BtnZeroPos_Click(this, args); break;
                case 4: BtnIdlePos_Click(this, args); break;
                case 5: BtnPreChangeJawPos_Click(this, args); break;
                case 6: BtnChangeJawPos_Click(this, args); break;
                case 7: BtnAssemblePos1_Click(this, args); break;
                case 8: BtnAssemblePos2_Click(this, args); break;
                case 9: BtnAssemblePos3_Click(this, args); break;
                case 10: BtnPreFetchRingPos_Click(this, args); break;
                case 11: BtnFetchRingPos_Click(this, args); break;
                case 12: BtnRun_Click(this, args); break;
                case 13: BtnStop_Click(this, args); break;
                case 14: BtnSetZero_Click(this, args); break;
            }
        }

        private void BtnDisconnectServer_Click(object sender, RoutedEventArgs e)
        {
            opcTaskCancelSource.Cancel();
        }

        private void BtnConnectServer_Click(object sender, RoutedEventArgs e)
        {
            serverThread = new Thread(ServerRunTask);
            opcTaskCancelSource = new();
            opcTaskCancelToken = opcTaskCancelSource.Token;
            serverThread.Start();
        }

        /***** UI 事件 *****/

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

        private void BtnPreAssemblePos2_Click(object sender, RoutedEventArgs e)
        {
            viewModel.TargetState.SetFromD5RJoints(PreAssemblePos2);
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
        //public void run2()
        //{
        //    if (robot == null)
        //    {
        //        MessageBox.Show("Robot not connected.");
        //        return;
        //    }

        //    Joints j = viewModel.TargetState.ToD5RJoints();
        //    D5Robot.ErrorCode err = robot.JointsMoveAbsolute(j);

        //    if (err != D5Robot.ErrorCode.OK)
        //    {
        //        MessageBox.Show($"Error while running: {err}");

        //        return;
        //    }
        //}

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
            Console.WriteLine("Button was clicked!");
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

        private void JointValueR1_TextChanged(object sender, TextChangedEventArgs e)
        {
            //joints100[0] = viewModel.TargetState.JointSpace.R1;
        }

        private void JointValueP2_TextChanged(object sender, TextChangedEventArgs e)
        {
            //joints100[1] = viewModel.TargetState.JointSpace.P2;
        }

        private void JointValueP3_TextChanged(object sender, TextChangedEventArgs e)
        {
            //joints100[2] = viewModel.TargetState.JointSpace.P3;
        }

        private void JointValueP4_TextChanged(object sender, TextChangedEventArgs e)
        {
            //joints100[3] = viewModel.TargetState.JointSpace.P4;
        }

        private void JointValueR5_TextChanged(object sender, TextChangedEventArgs e)
        {
            //joints100[4] = viewModel.TargetState.JointSpace.R5;
        }
    }
}