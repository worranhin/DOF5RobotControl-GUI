using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using DOF5RobotControl_GUI.ViewModel;
using DOF5RobotControl_GUI.WebAPI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OnnxInferenceLibrary;
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
            var service = ConfigureServices();
            Services = service.BuildServiceProvider();

            InitializeComponent();
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            MainWindow = mainWindow;
        }

        protected override void OnExit(ExitEventArgs e)
        {

            base.OnExit(e);
        }

        private static IServiceCollection ConfigureServices(IServiceCollection? services = null)
        {
            services ??= new ServiceCollection();

            // 注册服务
            services.AddSingleton<IRobotControlService, RobotControlService>();
            services.AddSingleton<IPopUpService, PopUpService>();
            services.AddSingleton<ICamMotorControlService, CamMotorControlService>();
            services.AddSingleton<ICameraControlService, CameraControlService>();
            services.AddSingleton<IOpcService, OpcService>();
            services.AddSingleton<IDataRecordService, DataRecordService>();
            services.AddSingleton<IYoloDetectionService, YoloDetectionService>();
            services.AddSingleton<IGamepadService, GamepadService>();
            services.AddSingleton<IProcessImageService, ProcessImageService>();
            services.AddSingleton<ActorPolicy>();
#if USE_DUMMY // 使用虚假服务，用于测试代码逻辑
            services.AddSingleton<IRobotControlService, DummyRobotControlService>(); // 虚假类，仅用于测试代码逻辑
            services.AddSingleton<ICameraControlService, DummyCameraControlService>(); // 虚假类，仅用于测试代码逻辑
#endif

            // 注册 ViewModel
            services.AddSingleton<MainViewModel>();
            services.AddSingleton(sp => new MainWindow(sp.GetRequiredService<MainViewModel>()));
            services.AddTransient<CameraViewModel>();
            services.AddTransient(sp => new CameraWindow() { DataContext = sp.GetRequiredService<CameraViewModel>() });

            return services;
        }
    }
}
