using DOF5RobotControl_GUI.Model;
using System.Configuration;
using System.Data;
using System.Windows;

namespace DOF5RobotControl_GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //public static MainWindow mainWin;
        ////[STAThread]

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 在后台线程中调用 GxLibInit
            Task.Run(GxCamera.GxLibInit);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 调用 GxLibUninit
            GxCamera.GxLibUninit();

            base.OnExit(e);
        }
    }
}
