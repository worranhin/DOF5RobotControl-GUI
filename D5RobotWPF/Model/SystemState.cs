using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.Model
{
    internal class SystemState
    {
        private static readonly Lazy<SystemState> _instance = new(() => new SystemState());
        public static SystemState Instance => _instance.Value;

        public bool SystemConnected { get; set; } = false;
    }
}
