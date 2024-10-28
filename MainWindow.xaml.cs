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

namespace DOF5RobotControl_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    internal static partial class D5RControl
    {
        internal struct Joints(int r1, int p2, int p3, int p4, int r5)
        {
            public int R1 = r1;
            public int P2 = p2;
            public int P3 = p3;
            public int P4 = p4;
            public int R5 = r5;
        };

        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_Init", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int Init(string RMDSerialPort);
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_DeInit")]
        internal static partial int DeInit();
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_Stop")]
        internal static partial int Stop();
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_SetZero")]
        internal static partial int SetZero(int r1 = 0, int p2 = 0, int p3 = 0, int p4 = 0, int r5 = 0);
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_JointsMoveAbsolute")]
        internal static partial int JointsMoveAbsolute(Joints j);
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_JointsMoveRelative")]
        internal static partial int JointsMoveRelative(Joints j);
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_Test")]
        internal static partial int Test(int x);
    }

    public partial class MainWindow : Window
    {
        //const Joints BeforeChangeJawPos1 = { 0, 5000000, -5000000, -15184980, 0 };
        //const Joints ChangeJawPos1 = { 0, -72195, 5174842, -6912012, 0 };
        //const Joints AfterChangeJawPos1 = { 0, -72195, -15000000, -6912012, 0 };
        //const Joints BeforeFetchRingPos1 = { 0, 8673000, -15000000, -10000000, 0 };
        //const Joints FetchRingPos1 = { 0, 8673000, 4000000, -10000000, 0 };
        //const Joints AfterFetchRingPos1 = { 0, 8673000, -15000000, -10000000, 0 };
        //const Joints AssemblePos1 = { 9000, 15686500, -16819200, -5759600, -10 };
        //const Joints AssemblePos2 = { 6000, -8027000, -15911400, 1783100, 0 };
        //const Joints AssemblePos3 = { 0, 0, 7004200, 15275000, 0 };
        //const Joints IdlePos = { 0, 0, -15000000, -10000000, 0 };

        private readonly JointsPositon IdlePos = new(0, 0, -15000000, -10000000, 0);
        private readonly JointsPositon ChangeJawPos = new(0, -72195, 5174842, -6912012, 0);
        private readonly JointsPositon PreChangeJawPos = new(0, -72195, -15000000, -6912012, 0);
        private readonly JointsPositon FetchRingPos = new(0, 8673000, 4000000, -10000000, 0);
        private readonly JointsPositon PreFetchRingPos = new(0, 8673000, -15000000, -10000000, 0);
        private readonly JointsPositon AssemblePos1 = new(9000, 15686500, -16819200, -5759600, -10);
        private readonly JointsPositon AssemblePos2 = new(6000, -8027000, -15911400, 1783100, 0);
        private readonly JointsPositon AssemblePos3 = new(0, 0, 7004200, 15275000, 0);

        private JointsPositon targetJointPos = new(0, 0, 0, 0, 0);
        private bool isConnected = false;
        private JogHandler jogHandler = new();

        public MainWindow()
        {
            InitializeComponent();

            // 初始化 Serial
            //string[] portArray = SerialPort.GetPortNames();
            //int x = TestDllMethods.TestDll();
            //int x = D5RControl.Test(10);
            //JointValueR1.Text = x.ToString();
            portBox.ItemsSource = SerialPort.GetPortNames();
            portBox.SelectedIndex = 0;
            this.DataContext = targetJointPos;
        }

        private void PortRefresh_Click(object sender, RoutedEventArgs e)
        {
            portBox.ItemsSource = SerialPort.GetPortNames();
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (isConnected)
            {
                //readTaskCancelSource.Cancel();  // 取消任务
                D5RControl.DeInit();

                try
                {
                    //readTask.Wait(2000);
                }
                catch (AggregateException)
                {
                    //DebugMessage($"{nameof(AggregateException)} thrown with message: {except.Message}\n");
                }
                catch (OperationCanceledException)
                {
                    //DebugMessage($"{nameof(OperationCanceledException)} thrown with message: {except.Message}\n");
                }
                finally
                {
                    //readTaskCancelSource.Dispose();
                }

                isConnected = false;

                // UI 处理
                portBox.IsEnabled = true;
                baudBox.IsEnabled = true;
                btnConnect.Content = "连接";
                //DebugMessage("serial closed\n");
            }
            else
            {
                // 创建 Task 实例并运行
                //readTaskCancelSource = new CancellationTokenSource();
                //readTaskCancelToken = readTaskCancelSource.Token;
                //readTask = new Task(() => ReadSerial(), readTaskCancelToken, TaskCreationOptions.LongRunning);
                //readTask.Start();
                int result = D5RControl.Init(portBox.Text);
                if(result != 0)
                {
                    MessageBox.Show($"Initialize error: {result}");
                    return;
                }
                isConnected = true;

                // UI 处理
                portBox.IsEnabled = false;
                baudBox.IsEnabled = false;
                btnConnect.Content = "断开连接";
                //DebugMessage("serial opened\n");
            }
        }

        private void BtnIdlePos_Click(object sender, RoutedEventArgs e)
        {
            targetJointPos = IdlePos;
            this.DataContext = targetJointPos;
        }

        private void BtnPreChangeJawPos_Click(object sender, RoutedEventArgs e)
        {
            targetJointPos = PreChangeJawPos;
            this.DataContext = targetJointPos;
        }

        private void BtnChangeJawPos_Click(object sender, RoutedEventArgs e)
        {
            targetJointPos = ChangeJawPos;
            this.DataContext = targetJointPos;
        }

        private void BtnAssemblePos1_Click(object sender, RoutedEventArgs e)
        {
            targetJointPos = AssemblePos1;
            this.DataContext = targetJointPos;
        }

        private void BtnAssemblePos2_Click(object sender, RoutedEventArgs e)
        {
            targetJointPos = AssemblePos2;
            this.DataContext = targetJointPos;
        }

        private void BtnAssemblePos3_Click(object sender, RoutedEventArgs e)
        {
            targetJointPos = AssemblePos3;
            this.DataContext = targetJointPos;
        }

        private void BtnPreFetchRingPos_Click(object sender, RoutedEventArgs e)
        {
            targetJointPos = PreFetchRingPos;
            this.DataContext = targetJointPos;
        }

        private void BtnFetchRingPos_Click(object sender, RoutedEventArgs e)
        {
            targetJointPos = FetchRingPos;
            this.DataContext = targetJointPos;
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            var j = ((JointsPositon)this.DataContext).ToD5RJoints();

            // TODO: Clear Code
            //Debug.WriteLine($"R1: {j.R1}, P2: {j.P2}, P3: {j.P3}, P4: {j.P4}, R5: {j.R5}");
            //return;
            
            //D5RControl.Joints j;
            //j.R1 = int.Parse(JointValueR1.Text);
            //j.P2 = int.Parse(JointValueP2.Text);
            //j.P3 = int.Parse(JointValueP3.Text);
            //j.P4 = int.Parse(JointValueP4.Text);
            //j.R5 = int.Parse(JointValueR5.Text);

            int result = D5RControl.JointsMoveAbsolute(j);
            if (result != 0)
            {
                //throw new Exception("Joints control error.");
                MessageBox.Show("Joints control error.");
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
            //Debug.WriteLine("button down");
            //jogHandler.TestStartJogging();
            const int deltaR1 = 100;
            D5RControl.Joints joints = new(-deltaR1, 0, 0, 0, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnR1JogUp_N(object sender, MouseButtonEventArgs e)
        {
            //Debug.WriteLine("button up.");
            jogHandler.StopJogging();
        }

        private void BtnR1JogDown_P(object sender, MouseButtonEventArgs e)
        {
            const int deltaR1 = 100;
            D5RControl.Joints joints = new(deltaR1, 0, 0, 0, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnR1JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        // P2 jogging button callbacks //

        private void BtnP2JogDown_N(object sender, MouseButtonEventArgs e)
        {
            const int deltaP2 = 100;
            D5RControl.Joints joints = new(0, -deltaP2, 0, 0, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnP2JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        private void BtnP2JogDown_P(object sender, MouseButtonEventArgs e)
        {
            const int deltaP2 = 100;
            D5RControl.Joints joints = new(0, deltaP2, 0, 0, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnP2JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        // P3 jogging button callbacks //

        private void BtnP3JogDown_N(object sender, MouseButtonEventArgs e)
        {
            const int deltaP3 = 100;
            D5RControl.Joints joints = new(0, 0, -deltaP3, 0, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnP3JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        private void BtnP3JogDown_P(object sender, MouseButtonEventArgs e)
        {
            const int deltaP3 = 100;
            D5RControl.Joints joints = new(0, 0, deltaP3, 0, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnP3JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        // P4 jogging button callbacks //

        private void BtnP4JogDown_N(object sender, MouseButtonEventArgs e)
        {
            const int deltaP4 = 100;
            D5RControl.Joints joints = new(0, 0, 0, -deltaP4, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnP4JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        private void BtnP4JogDown_P(object sender, MouseButtonEventArgs e)
        {
            const int deltaP4 = 100;
            D5RControl.Joints joints = new(0, 0, 0, deltaP4, 0);
            jogHandler.StartJogging(joints);
        }

        private void BtnP4JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        // R5 jogging button callbacks //

        private void BtnR5JogDown_N(object sender, MouseButtonEventArgs e)
        {
            const int deltaR5 = 100;
            D5RControl.Joints joints = new(0, 0, 0, 0, -deltaR5);
            jogHandler.StartJogging(joints);
        }

        private void BtnR5JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        private void BtnR5JogDown_P(object sender, MouseButtonEventArgs e)
        {
            const int deltaR5 = 100;
            D5RControl.Joints joints = new(0, 0, 0, 0, deltaR5);
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