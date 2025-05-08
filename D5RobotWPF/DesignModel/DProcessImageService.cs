using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.DesignModel
{
    class DProcessImageService : IProcessImageService
    {
        public void Init(CamFrame topFrame, CamFrame bottomFrame)
        {
            throw new NotImplementedException();
        }

        public Task<double> ProcessBottomImageAsync(CamFrame frame)
        {
            throw new NotImplementedException();
        }

        public Task<(double px, double py, double rz)> ProcessTopImgAsync(CamFrame frame)
        {
            throw new NotImplementedException();
        }
    }
}
