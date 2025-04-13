using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DOF5RobotControl_GUI.WebAPI
{
    [ApiController]
    [Route("[controller]")]
    public class RobotController : ControllerBase
    {
        IRobotControlService _robotCtrlService;

        public RobotController(IRobotControlService robotControlService)
        {
            _robotCtrlService = robotControlService;
        }

        [HttpGet("current")]
        public ActionResult<RoboticState> GetCurrentState()
        {
            try
            {
                var currentState = _robotCtrlService.CurrentState;
                return currentState;
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpGet("target")]
        public ActionResult<RoboticState> GetTargetState()
        {
            var targetState = _robotCtrlService.TargetState;

            if (targetState == null)
                return BadRequest();

            return targetState;
        }

        [HttpPost("connect/{portName}")]
        public ActionResult Connect(string portName)
        {
            try
            {
                _robotCtrlService.Connect(portName);
                return Ok(new { robotConnected = _robotCtrlService.RobotIsConnected });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("connect")]
        public ActionResult Connect()
        {
            try
            {
                _robotCtrlService.Connect(Properties.Settings.Default.RmdPort);
                return Ok(new { robotConnected = _robotCtrlService.RobotIsConnected });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("joint")]
        public ActionResult SetTargetState(JointSpace jointSpace)
        {
            if (jointSpace.HasErrors)
                return BadRequest();

            _robotCtrlService.TargetState.JointSpace.Copy(jointSpace);
            return NoContent();
        }

        [HttpPut("task")]
        public ActionResult SetTargetState(TaskSpace taskSpace)
        {
            _robotCtrlService.TargetState.TaskSpace.Copy(taskSpace);
            return NoContent();
        }
    }
}
