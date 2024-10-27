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

    public class MyData : INotifyPropertyChanged
    {
        public MyData()
        {
            _colorName = "Blue";
            ColorName = "Blue";
        }

        private string _colorName;
        public string ColorName
        {
            get { return _colorName; }
            set
            {
                if (_colorName != value)
                {
                    _colorName = value;
                    OnpropertyChanged();
                }
            }
        }



        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnpropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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

        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_Init")]
        internal static partial int Init();
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_DeInit")]
        internal static partial int DeInit();
        [LibraryImport("libDOF5RobotControl.dll", EntryPoint = "D5R_Stop")]
        internal static partial int Stop();
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
                try
                {
                    int result = D5RControl.Init();

                }
                catch (AggregateException except)
                {
                    MessageBox.Show(("Init Error:" + except.Message));
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
            j.R1 = int.Parse(JointValueR1.Text);
            j.P2 = int.Parse(JointValueP2.Text);
            j.P3 = int.Parse(JointValueP3.Text);
            j.P4 = int.Parse(JointValueP4.Text);
            j.R5 = int.Parse(JointValueR5.Text);
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

        // P5 jogging button callbacks //

        private void BtnP5JogDown_N(object sender, MouseButtonEventArgs e)
        {
            const int deltaP5 = 100;
            D5RControl.Joints joints = new(0, 0, 0, 0, -deltaP5);
            jogHandler.StartJogging(joints);
        }

        private void BtnP5JogUp_N(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }

        private void BtnP5JogDown_P(object sender, MouseButtonEventArgs e)
        {
            const int deltaP5 = 100;
            D5RControl.Joints joints = new(0, 0, 0, 0, deltaP5);
            jogHandler.StartJogging(joints);
        }

        private void BtnP5JogUp_P(object sender, MouseButtonEventArgs e)
        {
            jogHandler.StopJogging();
        }
    }
}