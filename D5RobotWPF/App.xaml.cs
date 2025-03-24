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

            // 注册服务
            services.AddSingleton<IRobotControlService, RobotControlService>();
            services.AddSingleton<IPopUpService, PopUpService>();
            services.AddSingleton<ICamMotorControlService, CamMotorControlService>();
            services.AddSingleton<ICameraControlService, CameraControlService>(); 
            services.AddSingleton<IOpcService, OpcService>();
            services.AddSingleton<IDataRecordService, DataRecordService>();
            services.AddSingleton<IYoloDetectionService, YoloDetectionService>();
#if USE_DUMMY // 使用虚假服务，用于测试代码逻辑
            services.AddSingleton<IRobotControlService, DummyRobotControlService>(); // 虚假类，仅用于测试代码逻辑
            services.AddSingleton<ICameraControlService, DummyCameraControlService>(); // 虚假类，仅用于测试代码逻辑
#endif

            // 注册 ViewModel
            services.AddSingleton<MainViewModel>();
            services.AddSingleton(sp => new MainWindow(sp.GetRequiredService<MainViewModel>()));

            services.AddTransient<CameraViewModel>();
            services.AddTransient(sp => new CameraWindow() { DataContext = sp.GetRequiredService<CameraViewModel>() });

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
