using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Model
{
    internal class NewD5Robot : D5R.D5Robot
    {
        NewD5Robot(string port) : base(port, natorId, 1, 2) { }

        const string natorId = "usb:id:2250716012";

        public static NewD5Robot? Instance { get; private set; }
        private static readonly object instanceLock = new();

        public static NewD5Robot Create(string port)
        {
            lock (instanceLock)
            {
                if (Instance != null)
                    throw new InvalidOperationException("机器人实例已创建，请访问 Instance 属性获取");

                Instance ??= new NewD5Robot(port);
                return Instance;
            }
        }

        public void Destroy()
        {
            lock (instanceLock)
            {
                Dispose();
                Instance = null;
            }
        }
    }
}
