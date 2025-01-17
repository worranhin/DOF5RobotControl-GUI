using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.ViewModel;
using MahApps.Metro.Controls;
using Opc.UaFx;
using Opc.UaFx.Server;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DOF5RobotControl_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : MetroWindow
    {
        internal readonly MainViewModel viewModel;

        Thread serverThread;
        CancellationTokenSource opcTaskCancelSource;
        CancellationToken opcTaskCancelToken;

        public MainWindow()
        {
            InitializeComponent();

            // 初始化 ViewModel
            viewModel = new(this.Dispatcher);
            DataContext = viewModel;

            // 初始化 OPC
            opcTaskCancelSource = new();
            opcTaskCancelToken = opcTaskCancelSource.Token;
            serverThread = new(ServerRunTask);

            // 注册窗口关闭回调函数
            this.Closing += (sender, e) => {
                Properties.Settings.Default.Port = viewModel.SelectedPort;
                Properties.Settings.Default.Save();
            };

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
                //Random rd = new Random(); 意义不明的操作，先注释掉，没问题再删
                while (!opcTaskCancelToken.IsCancellationRequested)
                {
                    //int i = rd.Next();

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

        private void BtnJogUp(object sender, MouseButtonEventArgs e)
        {
            viewModel.StopJogContinuous();
        }

        // R1 jogging button callbacks //

        private void BtnR1JogDown_N(object sender, MouseButtonEventArgs e)
        {

            JogParams param = new()
            {
                Joint = JointSelect.R1,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnR1JogDown_P(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.R1,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        // P2 jogging button callbacks //

        private void BtnP2JogDown_N(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.P2,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnP2JogDown_P(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.P2,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        // P3 jogging button callbacks //

        private void BtnP3JogDown_N(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.P3,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnP3JogDown_P(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.P3,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        // P4 jogging button callbacks //

        private void BtnP4JogDown_N(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.P4,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnP4JogDown_P(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.P4,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        // R5 jogging button callbacks //

        private void BtnR5JogDown_N(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.R5,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnR5JogDown_P(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.R5,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        private void TextBoxSelectAll(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textbox)
                textbox.SelectAll();
            else
                Debug.WriteLine("no textbox");
        }

        /// <summary>
        /// 处理输入文本框按下 Enter 的事件，使得按下 Enter 后更新 Binding 的属性
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JointTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
                textBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }
    }
}