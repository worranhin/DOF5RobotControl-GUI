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
        static private readonly MainViewModel viewModel = new();
        static private D5Robot? robot;
        static private JogHandler? jogHandler;
        static public double[] joints100;

        public MainWindow()
        {
            InitializeComponent();

            // 初始化 Serial
            viewModel.PortsAvailable = SerialPort.GetPortNames();
            if (viewModel.PortsAvailable.Length > 0)
                viewModel.SelectedPort = viewModel.PortsAvailable[0];

            DataContext = viewModel;

            joints100 = new double[5] { viewModel.TargetState.JointSpace.R1, viewModel.TargetState.JointSpace.P2, viewModel.TargetState.JointSpace.P3, viewModel.TargetState.JointSpace.P4, viewModel.TargetState.JointSpace.R5 };
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
        public static void run2()
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


        public void SomeOtherMethod(int method)
        {
            // 创建一个 EventArgs 实例来传递给 ButtonClicked 方法
            EventArgs args = new EventArgs();
            switch (method)
            {
                case 1: PortRefresh_Click(this, null); break;
                case 2: BtnConnect_Click(this, null); break;
                case 3: BtnZeroPos_Click(this, null); break;
                case 4: BtnIdlePos_Click(this, null); break;
                case 5: BtnPreChangeJawPos_Click(this, null); break;
                case 6: BtnChangeJawPos_Click(this, null); break;
                case 7: BtnAssemblePos1_Click(this, null); break;
                case 8: BtnAssemblePos2_Click(this, null); break;
                case 9: BtnAssemblePos3_Click(this, null); break;
                case 10: BtnPreFetchRingPos_Click(this, null); break;
                case 11: BtnFetchRingPos_Click(this, null); break;
                case 12: BtnRun_Click(this, null); break;
                case 13: BtnStop_Click(this, null); break;
                case 14: BtnSetZero_Click(this, null); break;

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
            joints100[0] = viewModel.TargetState.JointSpace.R1;
        }

        private void JointValueP2_TextChanged(object sender, TextChangedEventArgs e)
        {
            joints100[1] = viewModel.TargetState.JointSpace.P2;
        }

        private void JointValueP3_TextChanged(object sender, TextChangedEventArgs e)
        {
            joints100[2] = viewModel.TargetState.JointSpace.P3;
        }

        private void JointValueP4_TextChanged(object sender, TextChangedEventArgs e)
        {
            joints100[3] = viewModel.TargetState.JointSpace.P4;
        }

        private void JointValueR5_TextChanged(object sender, TextChangedEventArgs e)
        {
            joints100[4] = viewModel.TargetState.JointSpace.R5;
        }

        Thread serverThread;
        private void serverRun()
        {
            var dof5robotInstance = new dof5robotNodeManager();

            //var test = new MyNodeManager();
            using (var server = new OpcServer("opc.tcp://localhost:4840", dof5robotInstance))//server以nodeManager初始化
            {
                //服务器配置
                server.Configuration = OpcApplicationConfiguration.LoadServerConfig("Opc.UaFx.Server");
                server.ApplicationName = "DOF5ROBOT";//应用名称
                server.Start();
                Random rd = new Random();
                while (true)
                {
                    int i = rd.Next();

                    Thread.Sleep(1000);
                }
            }
        }

        private void BtnDisconnectServer_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnConnectServer_Click(object sender, RoutedEventArgs e)
        {
            serverThread = new Thread(serverRun);
            serverThread.Start();
        }
    }


    public class dof5robotNodeManager : OpcNodeManager
    {
        private OpcDataVariableNode<double> r1_Node;
        private OpcDataVariableNode<double> p2_Node;
        private OpcDataVariableNode<double> p3_Node;
        private OpcDataVariableNode<double> p4_Node;
        private OpcDataVariableNode<double> r5_Node;
        private OpcMethodNode method;

        [return: OpcArgument("是否成功")]//说明参数
        private bool runMotor()//运行
        {
            try
            {
                App.mainWin.SomeOtherMethod(12);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_1([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(1);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_2([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(2);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_3([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(3);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_4([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(4);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_5([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(5);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_6([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(6);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_7([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(7);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_8([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(8);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_9([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(9);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_10([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(10);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_11([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(11);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        [return: OpcArgument("调用情况")]//此处为输出参数，类型为下函数返回的类型
        private bool opcBtn_12([OpcArgument("运行", Description = "是否运行")] bool change)//内部为输入参数
        {
            try
            {
                App.mainWin.SomeOtherMethod(12);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_13([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(13);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }
        private bool opcBtn_14([OpcArgument("运行", Description = "是否运行")] bool change)//刷新
        {
            try
            {
                App.mainWin.SomeOtherMethod(14);
            }
            catch //异常处理，警告窗口
            {
                MessageBox.Show("CALL FAIL!");
                return false;
            }
            return true;
        }


        protected override IEnumerable<IOpcNode> CreateNodes(OpcNodeReferenceCollection references)
        {
            var rootNode = new OpcObjectNode(new OpcName("dof5", this.DefaultNamespaceIndex));
            references.Add(rootNode, OpcObjectTypes.ObjectsFolder);

            r1_Node = new OpcDataVariableNode<double>(rootNode, "r1");
            r1_Node.Value = MainWindow.joints100[0];
            p2_Node = new OpcDataVariableNode<double>(rootNode, "p2");
            p2_Node.Value = MainWindow.joints100[1];
            p3_Node = new OpcDataVariableNode<double>(rootNode, "p3");
            p3_Node.Value = MainWindow.joints100[2];
            p4_Node = new OpcDataVariableNode<double>(rootNode, "p4");
            p4_Node.Value = MainWindow.joints100[3];
            r5_Node = new OpcDataVariableNode<double>(rootNode, "r5");
            r5_Node.Value = MainWindow.joints100[4];
            r1_Node.WriteVariableValueCallback = handleWriteR1_NodeCallback;
            r1_Node.ReadVariableValueCallback = handleReadR1_NodeCallback;
            p2_Node.WriteVariableValueCallback = handleWriteP2_NodeCallback;
            p2_Node.ReadVariableValueCallback = handleReadP2_NodeCallback;
            p3_Node.WriteVariableValueCallback = handleWriteP3_NodeCallback;
            p3_Node.ReadVariableValueCallback = handleReadP3_NodeCallback;
            p4_Node.WriteVariableValueCallback = handleWriteP4_NodeCallback;
            p4_Node.ReadVariableValueCallback = handleReadP4_NodeCallback;
            r5_Node.WriteVariableValueCallback = handleWriteR5_NodeCallback;
            r5_Node.ReadVariableValueCallback = handleReadR5_NodeCallback;
            var globalControlNode = new OpcFolderNode(rootNode, "全局控制");
            var refreshNode = new OpcMethodNode(globalControlNode, "刷新", new Func<bool, bool>(this.opcBtn_1));
            var connectNode = new OpcMethodNode(globalControlNode, "连接", new Func<bool, bool>(this.opcBtn_2));
            var ZeroPosNode = new OpcMethodNode(globalControlNode, "零点位", new Func<bool, bool>(this.opcBtn_3));
            var IdlePosNode = new OpcMethodNode(globalControlNode, "待机位", new Func<bool, bool>(this.opcBtn_4));
            var PreChangeJawPosNode = new OpcMethodNode(globalControlNode, "换夹钳预备位", new Func<bool, bool>(this.opcBtn_5));
            var ChangeJawPosNode = new OpcMethodNode(globalControlNode, "换夹钳位", new Func<bool, bool>(this.opcBtn_6));
            var AssemblePos1Node = new OpcMethodNode(globalControlNode, "装配位1", new Func<bool, bool>(this.opcBtn_7));
            var AssemblePos2Node = new OpcMethodNode(globalControlNode, "装配位2", new Func<bool, bool>(this.opcBtn_8));
            var AssemblePos3Node = new OpcMethodNode(globalControlNode, "装配位3", new Func<bool, bool>(this.opcBtn_9));
            var PreFetchRingPosNode = new OpcMethodNode(globalControlNode, "取零件预备位", new Func<bool, bool>(this.opcBtn_10));
            var FetchRingPosNode = new OpcMethodNode(globalControlNode, "取零件位", new Func<bool, bool>(this.opcBtn_11));
            var runNode = new OpcMethodNode(globalControlNode, "运行", new Func<bool, bool>(this.opcBtn_12));
            var StopNode = new OpcMethodNode(globalControlNode, "停止", new Func<bool, bool>(this.opcBtn_13));
            var SetZeroNode = new OpcMethodNode(globalControlNode, "置零", new Func<bool, bool>(this.opcBtn_14));


            return new IOpcNode[] { rootNode };

        }


        OpcVariableValue<object> handleWriteR1_NodeCallback(OpcWriteVariableValueContext context, OpcVariableValue<object> value)
        {
            object objectValue = value.Value;
            double nodeValue = (double)objectValue;
            {
                // 安全地更新标签文本
                App.mainWin.Dispatcher.Invoke(() =>
                {
                    App.mainWin.JointValueR1.Text = nodeValue.ToString();
                });
            }
            return value;
        }
        OpcVariableValue<object> handleReadR1_NodeCallback(OpcReadVariableValueContext context, OpcVariableValue<object> value)
        {
            double r1_change = MainWindow.joints100[0];
            OpcVariableValue<object> r = new OpcVariableValue<object>(r1_change);
            return r;
        }
        OpcVariableValue<object> handleWriteP2_NodeCallback(OpcWriteVariableValueContext context, OpcVariableValue<object> value)
        {
            object objectValue = value.Value;
            double nodeValue = (double)objectValue;
            {
                App.mainWin.Dispatcher.Invoke(() =>
                {
                    App.mainWin.JointValueP2.Text = nodeValue.ToString();
                });
            }
            return value;
        }
        OpcVariableValue<object> handleReadP2_NodeCallback(OpcReadVariableValueContext context, OpcVariableValue<object> value)
        {
            double r1_change = MainWindow.joints100[1];
            OpcVariableValue<object> r = new OpcVariableValue<object>(r1_change);
            return r;
        }
        OpcVariableValue<object> handleWriteP3_NodeCallback(OpcWriteVariableValueContext context, OpcVariableValue<object> value)
        {
            object objectValue = value.Value;
            double nodeValue = (double)objectValue;
            {
                App.mainWin.Dispatcher.Invoke(() =>
                {
                    App.mainWin.JointValueP3.Text = nodeValue.ToString();
                });
            }
            return value;
        }
        OpcVariableValue<object> handleReadP3_NodeCallback(OpcReadVariableValueContext context, OpcVariableValue<object> value)
        {
            double r1_change = MainWindow.joints100[2];
            OpcVariableValue<object> r = new OpcVariableValue<object>(r1_change);
            return r;
        }
        OpcVariableValue<object> handleWriteP4_NodeCallback(OpcWriteVariableValueContext context, OpcVariableValue<object> value)
        {
            object objectValue = value.Value;
            double nodeValue = (double)objectValue;
            {
                App.mainWin.Dispatcher.Invoke(() =>
                {
                    App.mainWin.JointValueP4.Text = nodeValue.ToString();
                });
            }
            return value;
        }
        OpcVariableValue<object> handleReadP4_NodeCallback(OpcReadVariableValueContext context, OpcVariableValue<object> value)
        {
            double r1_change = MainWindow.joints100[3];
            OpcVariableValue<object> r = new OpcVariableValue<object>(r1_change);
            return r;
        }
        OpcVariableValue<object> handleWriteR5_NodeCallback(OpcWriteVariableValueContext context, OpcVariableValue<object> value)
        {
            object objectValue = value.Value;
            double nodeValue = (double)objectValue;
            {
                App.mainWin.Dispatcher.Invoke(() =>
                {
                    App.mainWin.JointValueR5.Text = nodeValue.ToString();
                });
            }
            return value;
        }
        OpcVariableValue<object> handleReadR5_NodeCallback(OpcReadVariableValueContext context, OpcVariableValue<object> value)
        {
            double r1_change = MainWindow.joints100[4];
            OpcVariableValue<object> r = new OpcVariableValue<object>(r1_change);
            return r;
        }
        public dof5robotNodeManager()
        : base("dof5")
        {

        }
    }
}