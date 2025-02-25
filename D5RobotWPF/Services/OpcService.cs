using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Opc.UaFx;
using Opc.UaFx.Server;
using System.Diagnostics;

namespace DOF5RobotControl_GUI.Services
{
    public class OpcService : IOpcService
    {
        CancellationTokenSource? opcCancelSource;

        ~OpcService()
        {
            Disconnect();
        }

        public void Connect()
        {
            opcCancelSource = new();
            var serverThread = new Thread(() => ServerRunTask(opcCancelSource.Token));
            serverThread.Start();
        }

        public void Disconnect()
        {
            opcCancelSource?.Cancel();
            opcCancelSource?.Dispose();
            opcCancelSource = null;
        }

        private static void ServerRunTask(CancellationToken token)
        {
            var vm = App.Current.Services.GetRequiredService<MainViewModel>();
            var dof5robotInstance = new D5RobotOpcNodeManager(vm);

            using (var server = new OpcServer("opc.tcp://localhost:4840", dof5robotInstance)) //server以nodeManager初始化
            {
                //服务器配置
                server.Configuration = OpcApplicationConfiguration.LoadServerConfig("Opc.UaFx.Server");
                server.ApplicationName = "DOF5ROBOT";//应用名称
                server.Start();
                //Random rd = new Random(); 意义不明的操作，先注释掉，没问题再删
                while (!token.IsCancellationRequested)
                {
                    //int i = rd.Next();
                    Debug.WriteLine("Opc Server running");
                    Thread.Sleep(1000);
                }
                server.Stop();
                Debug.WriteLine("Opc server stopped");
            }
        }
    }
}
