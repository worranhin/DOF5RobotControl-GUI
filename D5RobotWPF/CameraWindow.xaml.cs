using DOF5RobotControl_GUI.Services;
using DOF5RobotControl_GUI.ViewModel;
using Microsoft.Extensions.DependencyInjection;

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
        }
    }
}
