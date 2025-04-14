using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using DOF5RobotControl_GUI.ViewModel;
using DOF5RobotControl_GUI.WebAPI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        private Task? gxLibInitTask;

        public App()
        {
            var builder = WebApplication.CreateBuilder();

            // Add services to the container.
            ConfigureServices(builder.Services);

            builder.WebHost.UseUrls("http://localhost:5162");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            app.UseSwagger();
            app.UseSwaggerUI();
            //}

            app.UseAuthorization();
            app.MapControllers();

            app.RunAsync();

            // 引用 ServiceProvider 以便于使用
            Services = app.Services;

            InitializeComponent();
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 在后台线程中调用 GxLibInit
#if !USE_DUMMY
            gxLibInitTask = Task.Run(GxCamera.GxLibInit);
#endif

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            MainWindow = mainWindow;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 调用 GxLibUninit
            gxLibInitTask?.Wait();
            GxCamera.GxLibUninit();

            base.OnExit(e);
        }

        private IServiceCollection ConfigureServices(IServiceCollection services)
        {
            //var services = new ServiceCollection();

            // 注册服务
            services.AddSingleton<IRobotControlService, RobotControlService>();
            services.AddSingleton<IPopUpService, PopUpService>();
            services.AddSingleton<ICamMotorControlService, CamMotorControlService>();
            services.AddSingleton<ICameraControlService, CameraControlService>();
            services.AddSingleton<IOpcService, OpcService>();
            services.AddSingleton<IDataRecordService, DataRecordService>();
            services.AddSingleton<IYoloDetectionService, YoloDetectionService>();
            services.AddSingleton<IGamepadService, GamepadService>();
#if USE_DUMMY // 使用虚假服务，用于测试代码逻辑
            services.AddSingleton<IRobotControlService, DummyRobotControlService>(); // 虚假类，仅用于测试代码逻辑
            services.AddSingleton<ICameraControlService, DummyCameraControlService>(); // 虚假类，仅用于测试代码逻辑
#endif

            // 注册 ViewModel
            services.AddSingleton<MainViewModel>();
            services.AddSingleton(sp => new MainWindow(sp.GetRequiredService<MainViewModel>()));
            services.AddTransient<CameraViewModel>();
            services.AddTransient(sp => new CameraWindow() { DataContext = sp.GetRequiredService<CameraViewModel>() });

            // 注册 Web API 相关服务
            services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            return services;
        }
    }
}
