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
using DOF5RobotControl_GUI.ViewModel;

namespace DOF5RobotControl_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        readonly D5RControl.Joints ZeroPos = new(0, 0, 0, 0, 0);
        readonly D5RControl.Joints IdlePos = new(0, 0, -14000000, -10000000, 0);
        readonly D5RControl.Joints ChangeJawPos = new(0, -72195, 5174842, -6912012, 0);
        readonly D5RControl.Joints PreChangeJawPos = new(0, 5000000, -5000000, -15184980, 0);
        readonly D5RControl.Joints FetchRingPos = new(0, 8673000, 4000000, -10000000, 0);
        readonly D5RControl.Joints PreFetchRingPos = new(0, 8673000, -15000000, -10000000, 0);
        readonly D5RControl.Joints AssemblePos1 = new(9000, 15686500, -16819200, -5759600, -10);
        readonly D5RControl.Joints AssemblePos2 = new(6000, -8027000, -15911400, 1783100, 0);
        readonly D5RControl.Joints AssemblePos3 = new(0, 0, 7004200, 15275000, 0);



        //private readonly JointsPosition ZeroPos = new(0, 0, 0, 0, 0);
        //private readonly JointsPosition IdlePos = new(0, 0, -15000000, -10000000, 0);
        //private readonly JointsPosition ChangeJawPos = new(0, -72195, 5174842, -6912012, 0);
        //private readonly JointsPosition PreChangeJawPos = new(0, -72195, -15000000, -6912012, 0);
        //private readonly JointsPosition FetchRingPos = new(0, 8673000, 4000000, -10000000, 0);
        //private readonly JointsPosition PreFetchRingPos = new(0, 8673000, -15000000, -10000000, 0);
        //private readonly JointsPosition AssemblePos1 = new(9000, 15686500, -16819200, -5759600, -10);
        //private readonly JointsPosition AssemblePos2 = new(6000, -8027000, -15911400, 1783100, 0);
        //private readonly JointsPosition AssemblePos3 = new(0, 0, 7004200, 15275000, 0);
        private JointsPosition targetJointPos = new(0, 0, 0, 0, 0);
        //private bool isConnected = false;
        private readonly JogHandler jogHandler = new();
        readonly int natorJogResolution = 100000;
        readonly int RMDJogResolution = 20;
        private readonly MainViewModel mainViewModel = new();

        public MainWindow()
        {
            InitializeComponent();

            // 初始化 Serial
            mainViewModel.PortsAvailable = SerialPort.GetPortNames();
            if (mainViewModel.PortsAvailable.Length > 0)
                mainViewModel.SelectedPort = mainViewModel.PortsAvailable[0];

            DataContext = mainViewModel;
        }

        private void PortRefresh_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.PortsAvailable = SerialPort.GetPortNames();
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (mainViewModel.SystemConnected)  // 如果目前系统已连接
            {
                int result = D5RControl.DeInit();
                if (result != 0)
                {
                    MessageBox.Show($"DeInitialize error: {result}");
                    return;
                }
                mainViewModel.SystemConnected = false;
            }
            else
            {
                string newStr;
                if(mainViewModel.SelectedPort.Length > 4)
                {
                    newStr = "\\\\.\\" + mainViewModel.SelectedPort;
                } else
                {
                    newStr = mainViewModel.SelectedPort;
                }
                int result = D5RControl.Init(newStr);
                if (result != 0)
                {
                    MessageBox.Show($"Initialize error: {result}");
                    return;
                }
                mainViewModel.SystemConnected = true;
            }
        }

        private void BtnZeroPos_Click(object sender, RoutedEventArgs e)
        {
            //targetJointPos = ZeroPos;
            //this.DataContext = targetJointPos;
            mainViewModel.TargetPosition.SetFromJoints(ZeroPos);
        }

        private void BtnIdlePos_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.TargetPosition.SetFromJoints(IdlePos);
        }

        private void BtnPreChangeJawPos_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.TargetPosition.SetFromJoints(PreChangeJawPos);
        }

        private void BtnChangeJawPos_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.TargetPosition.SetFromJoints(ChangeJawPos);
        }

        private void BtnAssemblePos1_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.TargetPosition.SetFromJoints(AssemblePos1);
        }

        private void BtnAssemblePos2_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.TargetPosition.SetFromJoints(AssemblePos2);
        }

        private void BtnAssemblePos3_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.TargetPosition.SetFromJoints(AssemblePos3);
        }

        private void BtnPreFetchRingPos_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.TargetPosition.SetFromJoints(PreFetchRingPos);
        }

        private void BtnFetchRingPos_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.TargetPosition.SetFromJoints(FetchRingPos);
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            D5RControl.Joints j = mainViewModel.TargetPosition.ToD5RJoints();

            int result = D5RControl.JointsMoveAbsolute(j);
            if (result != 0)
            {
                //throw new Exception("Joints control error.");
                MessageBox.Show($"Joints control error: {result}.");
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            int result = D5RControl.Stop();
            if (result != 0)
            {
                //throw new Exception("Robot stop error.");
                MessageBox.Show("Robot stop error.");
            }
        }

        private void BtnSetZero_Click(object sender, RoutedEventArgs e)
        {
            int result = D5RControl.SetZero();
            if (result != 0)
            {
                MessageBox.Show($"Set zero error: {result}.");
            }
        }

        // R1 jogging button callbacks //

        private void BtnR1JogDown_N(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("button down");
            //jogHandler.TestStartJogging();
            D5RControl.Joints joints = new(-RMDJogResolution, 0, 0, 0, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnR1JogUp_N(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("button up.");
            jogHandler.StopJogging();
        }

        private void BtnR1JogDown_P(object sender, MouseButtonEventArgs e)
        {
            D5RControl.Joints joints = new(RMDJogResolution, 0, 0, 0, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnR1JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        // P2 jogging button callbacks //

        private void BtnP2JogDown_N(object sender, MouseButtonEventArgs e)
        {
            D5RControl.Joints joints = new(0, -natorJogResolution, 0, 0, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnP2JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        private void BtnP2JogDown_P(object sender, MouseButtonEventArgs e)
        {
            D5RControl.Joints joints = new(0, natorJogResolution, 0, 0, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnP2JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        // P3 jogging button callbacks //

        private void BtnP3JogDown_N(object sender, MouseButtonEventArgs e)
        {
            D5RControl.Joints joints = new(0, 0, -natorJogResolution, 0, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnP3JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        private void BtnP3JogDown_P(object sender, MouseButtonEventArgs e)
        {
            D5RControl.Joints joints = new(0, 0, natorJogResolution, 0, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnP3JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        // P4 jogging button callbacks //

        private void BtnP4JogDown_N(object sender, MouseButtonEventArgs e)
        {
            D5RControl.Joints joints = new(0, 0, 0, -natorJogResolution, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnP4JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        private void BtnP4JogDown_P(object sender, MouseButtonEventArgs e)
        {
            D5RControl.Joints joints = new(0, 0, 0, natorJogResolution, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnP4JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        // R5 jogging button callbacks //

        private void BtnR5JogDown_N(object sender, MouseButtonEventArgs e)
        {
            D5RControl.Joints joints = new(0, 0, 0, 0, -RMDJogResolution);
            jogHandler.StartJogging(joints);
        }

        private void BtnR5JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        private void BtnR5JogDown_P(object sender, MouseButtonEventArgs e)
        {
            D5RControl.Joints joints = new(0, 0, 0, 0, RMDJogResolution);
            jogHandler.StartJogging(joints);
        }

        private void BtnR5JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        private void BtnOpenManualControl_Click(object sender, RoutedEventArgs e)
        {
            ManualControlWindow window = new ManualControlWindow();
            window.Show();
        }
    }
}