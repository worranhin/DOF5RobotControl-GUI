using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using DOF5RobotControl_GUI.ViewModel;
using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;
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

        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();

            // 初始化 ViewModel
            viewModel = vm;
            DataContext = viewModel;

            // 注册窗口关闭回调函数
            this.Closed += Window_Closed;
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            var opc = App.Current.Services.GetService<IOpcService>();
            opc?.Disconnect();

            var teleopService = App.Current.Services.GetService<IGamepadService>();
            teleopService?.Stop();
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

        // Px jogging button callbacks //

        private void BtnPxJogDown_N(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.Px,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnPxJogDown_P(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.Px,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        // Py jogging button callbacks //

        private void BtnPyJogDown_N(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.Py,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnPyJogDown_P(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.Py,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        // Pz jogging button callbacks //

        private void BtnPzJogDown_N(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.Pz,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnPzJogDown_P(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.Pz,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        // Ry jogging button callbacks //

        private void BtnRyJogDown_N(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.Ry,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnRyJogDown_P(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.Ry,
                IsPositive = true
            };

            viewModel.StartJogContinuous(param);
        }

        // Rz jogging button callbacks //

        private void BtnRzJogDown_N(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.Rz,
                IsPositive = false
            };

            viewModel.StartJogContinuous(param);
        }

        private void BtnRzJogDown_P(object sender, MouseButtonEventArgs e)
        {
            JogParams param = new()
            {
                Joint = JointSelect.Rz,
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