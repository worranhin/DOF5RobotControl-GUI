using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using DOF5RobotControl_GUI.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DOF5RobotControl_GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //[STAThread]

        public static new App Current => (App)Application.Current;

        public IServiceProvider Services { get; }

        public App()
        {
            Services = ConfigureServices();

            InitializeComponent();
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IRobotControlService, RobotControlService>();
            services.AddTransient<IPopUpService, PopUpService>();
            services.AddTransient<MainViewModel>();
            services.AddTransient(sp => new MainWindow(sp.GetRequiredService<MainViewModel>()));

            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 在后台线程中调用 GxLibInit
            Task.Run(GxCamera.GxLibInit);

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            this.MainWindow = mainWindow;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 调用 GxLibUninit
            GxCamera.GxLibUninit();

            base.OnExit(e);
        }
    }
}
