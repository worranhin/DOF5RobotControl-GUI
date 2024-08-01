using System.IO.Ports;
using System.Runtime.InteropServices;
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

namespace DOF5RobotControl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public class JointsPositon
    {
        public int R1 { get; set; }
        public int P2 { get; set; }
        public int P3 { get; set; }
        public int P4 { get; set; }
        public int R5 { get; set; }

        public JointsPositon(int r1, int p2, int p3, int p4, int r5)
        {
            R1 = r1;
            P2 = p2;
            P3 = p3;
            P4 = p4;
            R5 = r5;
        }
    };

    internal partial class TestDllMethods
    {
        //[DllImport("TestDll.dll", CharSet = CharSet.Auto)]
        [LibraryImport("TestDll.dll")]
        internal static partial int TestDll();
    }

    internal static partial class D5RControl
    {
        internal struct Joints
        {
            public int r1;
            public int p2;
            public int p3;
            public int p4;
            public int r5;
        };

        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_Init", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int Init(string RMDSerialPort);
        [LibraryImport ("libDOF5RobotControl.dll", EntryPoint = "D5R_DeInit")]
        internal static partial int DeInit();
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_Stop")]
        internal static partial int Stop();
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_SetZero")]
        internal static partial int SetZero(int r1 = 0, int p2 = 0, int p3 = 0, int p4 = 0, int r5 = 0);
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_JointsControl")]
        internal static partial int JointsControl(Joints j);
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

        private bool isConnected = false;

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
        }

        private void UpdateJointControlTextBox(JointsPositon j)
        {
            JointValueR1.Text = j.R1.ToString();
            JointValueP2.Text = j.P2.ToString();
            JointValueP3.Text = j.P3.ToString();
            JointValueP4.Text = j.P4.ToString();
            JointValueR5.Text = j.R5.ToString();
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
                catch (AggregateException except)
                {
                    //DebugMessage($"{nameof(AggregateException)} thrown with message: {except.Message}\n");
                }
                catch (OperationCanceledException except)
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
            UpdateJointControlTextBox(IdlePos);
        }

        private void BtnPreChangeJawPos_Click(object sender, RoutedEventArgs e)
        {
            UpdateJointControlTextBox(PreChangeJawPos);
        }

        private void BtnChangeJawPos_Click(object sender, RoutedEventArgs e)
        {
            UpdateJointControlTextBox(ChangeJawPos);
        }

        private void BtnAssemblePos1_Click(object sender, RoutedEventArgs e)
        {
            UpdateJointControlTextBox(AssemblePos1);
        }

        private void BtnAssemblePos2_Click(object sender, RoutedEventArgs e)
        {
            UpdateJointControlTextBox(AssemblePos2);
        }

        private void BtnAssemblePos3_Click(object sender, RoutedEventArgs e)
        {
            UpdateJointControlTextBox(AssemblePos3);
        }

        private void BtnPreFetchRingPos_Click(object sender, RoutedEventArgs e)
        {
            UpdateJointControlTextBox(PreFetchRingPos);
        }

        private void BtnFetchRingPos_Click(object sender, RoutedEventArgs e)
        {
            UpdateJointControlTextBox(FetchRingPos);
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            D5RControl.Joints j;
            j.r1 = int.Parse(JointValueR1.Text);
            j.p2 = int.Parse(JointValueP2.Text);
            j.p3 = int.Parse(JointValueP3.Text);
            j.p4 = int.Parse(JointValueP4.Text);
            j.r5 = int.Parse(JointValueR5.Text);
            int result = D5RControl.JointsControl(j);
            if (result != 0)
            {
                throw new Exception("Joints control error.");
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            int result = D5RControl.Stop();
            if (result != 0)
            {
                throw new Exception("Robot stop error.");
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
    }
}