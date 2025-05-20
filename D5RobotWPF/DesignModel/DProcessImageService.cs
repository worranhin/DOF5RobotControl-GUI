using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;

namespace DOF5RobotControl_GUI.DesignModel
{
    class DProcessImageService : IProcessImageService
    {
        public Task<(double x, double y, double rz)> GetEntranceErrorAsync(CamFrame topImg)
        {
            throw new NotImplementedException();
        }

        public void Init(CamFrame topFrame, CamFrame bottomFrame)
        {
            throw new NotImplementedException();
        }

        public Task<double> ProcessBottomImageAsync(CamFrame frame)
        {
            throw new NotImplementedException();
        }

        public Task<(double x, double y, double rz)> GetJawErrorAsync(CamFrame topImg)
        {
            throw new NotImplementedException();
        }
    }
}
