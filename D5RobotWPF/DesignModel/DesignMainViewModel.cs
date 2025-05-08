using DOF5RobotControl_GUI.Services;
using DOF5RobotControl_GUI.ViewModel;

namespace DOF5RobotControl_GUI.DesignModel
{
    public class DesignMainViewModel : MainViewModel
    {
        public DesignMainViewModel() : base(
            new DRobotControlService(), 
            new PopUpService(), 
            new DCameraControlService(), 
            new OpcService(), 
            new DataRecordService(), 
            new GamepadService(),
            new DProcessImageService()
            )
        {
        }
    }
}
