using DOF5RobotControl_GUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.ViewModel
{
    public class DesignMainViewModel : MainViewModel
    {
        public DesignMainViewModel() 
            : base(new RobotControlService(), new PopUpService(), new CameraControlService(new CamMotorControlService()), new OpcService())
        {
        }
    }
}
