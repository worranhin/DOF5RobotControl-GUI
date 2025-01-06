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
using Opc.UaFx;
using Opc.UaFx.Server;
using Window = System.Windows.Window;
using System.Timers;
using MahApps.Metro.Controls;

namespace DOF5RobotControl_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : MetroWindow
    {     
        //readonly string natorId = "usb:id:7547982319";
        
        internal readonly MainViewModel viewModel;
        
        
        //private RoboticState targetState = new(0, 0, 0, 0, 0);
        //private RoboticState currentState = new(0, 0, 0, 0, 0);
        Thread serverThread;
        CancellationTokenSource opcTaskCancelSource;
        CancellationToken opcTaskCancelToken;
        const uint jogPeriod = 20;  // ms

        //static public double[] joints100;

        public MainWindow()
        {
            InitializeComponent();

            // 初始化 ViewModel
            viewModel = new(this);
            DataContext = viewModel;

            // 初始化 Serial
            viewModel.PortsAvailable = SerialPort.GetPortNames();
            if (viewModel.PortsAvailable.Length > 0)
                viewModel.SelectedPort = viewModel.PortsAvailable[0];            

            // 初始化 OPC
            opcTaskCancelSource = new();
            opcTaskCancelToken = opcTaskCancelSource.Token;
            serverThread = new(ServerRunTask);

            // 注册窗口关闭回调函数
            this.Closed += Window_Closed;
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            Debug.WriteLine("Window closed");
            opcTaskCancelSource.Cancel();
            
        }     

        private void ServerRunTask()
        {
            var dof5robotInstance = new D5RobotOpcNodeManager(viewModel);

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

        // R1 jogging button callbacks //

        private void BtnJogUp(object sender, MouseButtonEventArgs e)
        {
            //jogHandler?.StopJogging();
            //jogTimer?.Stop();
            //jogTimer = null;
            viewModel.StopJogContinuous();
        }

        private void BtnR1JogDown_N(object sender, MouseButtonEventArgs e)
        {

            JogParams param = new()
            {
                Joint = JointSelect.R1,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
            //Console.WriteLine("Button was clicked!");
            //if (jogHandler == null)
            //{
            //    MessageBox.Show("Robot not connected.");
            //    return;
            //}
            //Joints joints = new(-viewModel.RMDJogResolution, 0, 0, 0, 0);
            //viewModel.jogHandler?.StartJogging(joints);


            //if (viewModel.JogModeSelected == JogMode.Continuous)
            //{
            //JogParams param = new()
            //{
            //    Joint = JointSelect.R1,
            //    IsPositive = false
            //};

            //double resolution = 0;
            //switch (viewModel.JogResolutionSelected)
            //{
            //    case JogResolution.Speed1mm:
            //        resolution = 1;
            //        break;
            //    case JogResolution.Speed100um:
            //        resolution = 0.1;
            //        break;
            //    case JogResolution.Speed10um:
            //        resolution = 0.01;
            //        break;
            //    default:
            //        break;
            //}

            //if (!param.IsPositive)
            //    resolution = -resolution;  // 每秒步进量

            //jogTimer = new(jogPeriod);
            //resolution = resolution * jogPeriod / 1000;  // 每次控制的步进量
            //jogTimer.Elapsed += (source, e) =>
            //{
            //    viewModel.TargetState.JointSpace.R1 += resolution;
            //    //viewModel.RobotRunCommand.Execute(null);
            //};

            //jogTimer.Start();

            //}
            //viewModel.JogCommand.Execute(param);
        }

        private void BtnR1JogUp_N(object sender, MouseButtonEventArgs e)
        {
            //jogHandler?.StopJogging();
            //jogTimer?.Stop();
            //jogTimer = null;
            viewModel.StopJogContinuous();
        }

        private void BtnR1JogDown_P(object sender, MouseButtonEventArgs e)
        {
            //Joints joints = new(RMDJogResolution, 0, 0, 0, 0);
            //jogHandler?.StartJogging(joints);
            JogParams param = new()
            {
                Joint = JointSelect.R1,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnR1JogUp_P(object sender, MouseButtonEventArgs e)
        {
            //jogHandler?.StopJogging();
        }

        // P2 jogging button callbacks //

        private void BtnP2JogDown_N(object sender, MouseButtonEventArgs e)
        {
            //Joints joints = new(0, -natorJogResolution, 0, 0, 0);
            //jogHandler?.StartJogging(joints);
            JogParams param = new()
            {
                Joint = JointSelect.P2,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnP2JogUp_N(object sender, MouseButtonEventArgs e)
        {
            //jogHandler?.StopJogging();
        }

        private void BtnP2JogDown_P(object sender, MouseButtonEventArgs e)
        {
            //Joints joints = new(0, natorJogResolution, 0, 0, 0);
            //jogHandler?.StartJogging(joints);
            JogParams param = new()
            {
                Joint = JointSelect.P2,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnP2JogUp_P(object sender, MouseButtonEventArgs e)
        {
            //jogHandler?.StopJogging();
        }

        // P3 jogging button callbacks //

        private void BtnP3JogDown_N(object sender, MouseButtonEventArgs e)
        {
            //Joints joints = new(0, 0, -natorJogResolution, 0, 0);
            //jogHandler?.StartJogging(joints);
            JogParams param = new()
            {
                Joint = JointSelect.P3,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnP3JogUp_N(object sender, MouseButtonEventArgs e)
        {
            //jogHandler?.StopJogging();
        }

        private void BtnP3JogDown_P(object sender, MouseButtonEventArgs e)
        {
            //Joints joints = new(0, 0, natorJogResolution, 0, 0);
            //jogHandler?.StartJogging(joints);
            JogParams param = new()
            {
                Joint = JointSelect.P3,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnP3JogUp_P(object sender, MouseButtonEventArgs e)
        {
            //jogHandler?.StopJogging();
        }

        // P4 jogging button callbacks //

        private void BtnP4JogDown_N(object sender, MouseButtonEventArgs e)
        {
            //Joints joints = new(0, 0, 0, -natorJogResolution, 0);
            //jogHandler?.StartJogging(joints);
            JogParams param = new()
            {
                Joint = JointSelect.P4,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnP4JogUp_N(object sender, MouseButtonEventArgs e)
        {
            //jogHandler?.StopJogging();
        }

        private void BtnP4JogDown_P(object sender, MouseButtonEventArgs e)
        {
            //Joints joints = new(0, 0, 0, natorJogResolution, 0);
            //jogHandler?.StartJogging(joints);
            JogParams param = new()
            {
                Joint = JointSelect.P4,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnP4JogUp_P(object sender, MouseButtonEventArgs e)
        {
            //jogHandler?.StopJogging();
        }

        // R5 jogging button callbacks //

        private void BtnR5JogDown_N(object sender, MouseButtonEventArgs e)
        {
            //Joints joints = new(0, 0, 0, 0, -RMDJogResolution);
            //jogHandler?.StartJogging(joints);
            JogParams param = new()
            {
                Joint = JointSelect.R5,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnR5JogUp_N(object sender, MouseButtonEventArgs e)
        {
            //jogHandler?.StopJogging();
        }

        private void BtnR5JogDown_P(object sender, MouseButtonEventArgs e)
        {
            //Joints joints = new(0, 0, 0, 0, RMDJogResolution);
            //jogHandler?.StartJogging(joints);
            JogParams param = new()
            {
                Joint = JointSelect.R5,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnR5JogUp_P(object sender, MouseButtonEventArgs e)
        {
            //jogHandler?.StopJogging();
        }
    }
}