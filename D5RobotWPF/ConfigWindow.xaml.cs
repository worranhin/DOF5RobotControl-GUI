using DOF5RobotControl_GUI.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DOF5RobotControl_GUI
{
    /// <summary>
    /// ConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public ConfigWindow()
        {
            InitializeComponent();
            DataContext = viewModel;

            Closing += ConfigWindow_Closing;
        }

        private void ConfigWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (viewModel.PropertiesNotSaved == true)
            {
                string text = "配置还没有保存，是否保存？";
                string caption = "配置未保存";
                MessageBoxButton button = MessageBoxButton.YesNoCancel;
                MessageBoxResult result;

                result = MessageBox.Show(text, caption, button);
                Debug.WriteLine(result);

                if (result == MessageBoxResult.Cancel)
                    e.Cancel = true;
                else if (result == MessageBoxResult.Yes)
                    Properties.Settings.Default.Save();
            }
        }

        private readonly ConfigViewModel viewModel = new();
    }
}
