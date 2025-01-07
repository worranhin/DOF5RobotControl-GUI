using DOF5RobotControl_GUI.ViewModel;
using System;
using System.Collections.Generic;
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
    /// CameraWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CameraWindow : MahApps.Metro.Controls.MetroWindow
    {
        private readonly CameraViewModel viewModel = new();
        public CameraWindow()
        {
            InitializeComponent();

            this.DataContext = viewModel;

            this.Closed += (sender, e) =>
            {
                viewModel.Dispose();
            };
        }
    }
}
