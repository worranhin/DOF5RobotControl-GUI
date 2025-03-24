using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DOF5RobotControl_GUI.Model;
using System.Diagnostics;

namespace DOF5RobotControl_GUI.ViewModel
{
    partial class MainViewModel
    {
        /***** 点动相关字段/属性 *****/
        public static IEnumerable<JogMode> JogModes => Enum.GetValues(typeof(JogMode)).Cast<JogMode>();
        public static IEnumerable<JogResolution> JogResolutions => Enum.GetValues(typeof(JogResolution)).Cast<JogResolution>();

        public const int natorJogResolution = 30000;
        public const int RMDJogResolution = 20;
        const uint jogPeriod = 20;  // ms

        System.Timers.Timer? jogTimer;
        [ObservableProperty]
        private JogMode _jogModeSelected = JogMode.OneStep;
        [ObservableProperty]
        private JogResolution _jogResolutionSelected = JogResolution.Speed1mm;

        /***** Jog 相关命令 *****/

        [RelayCommand]
        private void Jog(JogParams param)
        {
            if (JogModeSelected == JogMode.OneStep)
            {
                double resolution = 0;
                switch (JogResolutionSelected)
                {
                    case JogResolution.Speed1mm:
                        resolution = 1;
                        break;
                    case JogResolution.Speed100um:
                        resolution = 0.1;
                        break;
                    case JogResolution.Speed10um:
                        resolution = 0.01;
                        break;
                    default:
                        Debug.WriteLine("Invalid JogResolutionSelected");
                        break;
                }

                resolution = param.IsPositive ? resolution : -resolution;

                TargetState.JointSpace.Copy(CurrentState.JointSpace);

                switch (param.Joint)
                {
                    case JointSelect.R1:
                        TargetState.JointSpace.R1 += resolution;
                        break;
                    case JointSelect.P2:
                        TargetState.JointSpace.P2 += resolution;
                        break;
                    case JointSelect.P3:
                        TargetState.JointSpace.P3 += resolution;
                        break;
                    case JointSelect.P4:
                        TargetState.JointSpace.P4 += resolution;
                        break;
                    case JointSelect.R5:
                        TargetState.JointSpace.R5 += resolution;
                        break;
                    case JointSelect.Px:
                        TargetPx += resolution;
                        break;
                    case JointSelect.Py:
                        TargetPy += resolution;
                        break;
                    case JointSelect.Pz:
                        TargetPz += resolution;
                        break;
                    case JointSelect.Ry:
                        TargetState.TaskSpace.Ry += resolution;
                        break;
                    case JointSelect.Rz:
                        TargetState.TaskSpace.Rz += resolution;
                        break;
                    default:
                        Debug.WriteLine("Invalid JointSelect");
                        break;
                }

                if (!TargetState.JointSpace.HasErrors && _robotControlService.RobotIsConnected)
                    _robotControlService.MoveTo(TargetState);
            }
        }

        public void StartJogContinuous(JogParams param)
        {
            if (JogModeSelected != JogMode.Continuous)
            {
                return;
            }

            double resolution = 0;
            switch (JogResolutionSelected)
            {
                case JogResolution.Speed1mm:
                    resolution = 1;
                    break;
                case JogResolution.Speed100um:
                    resolution = 0.1;
                    break;
                case JogResolution.Speed10um:
                    resolution = 0.01;
                    break;
                default:
                    break;
            }

            if (!param.IsPositive)
                resolution = -resolution;  // 每秒步进量

            jogTimer = new(jogPeriod);
            resolution = resolution * jogPeriod / 1000;  // 每次控制的步进量

            Action updateJointAction = () => { };
            switch (param.Joint)
            {
                case JointSelect.R1:
                    updateJointAction = () => { TargetState.JointSpace.R1 += resolution; };
                    break;
                case JointSelect.P2:
                    updateJointAction = () => { TargetState.JointSpace.P2 += resolution; };
                    break;
                case JointSelect.P3:
                    updateJointAction = () => { TargetState.JointSpace.P3 += resolution; };
                    break;
                case JointSelect.P4:
                    updateJointAction = () => { TargetState.JointSpace.P4 += resolution; };
                    break;
                case JointSelect.R5:
                    updateJointAction = () => { TargetState.JointSpace.R5 += resolution; };
                    break;
                case JointSelect.Px:
                    updateJointAction = () => { TargetPx += resolution; };
                    break;
                case JointSelect.Py:
                    updateJointAction = () => { TargetPy += resolution; };
                    break;
                case JointSelect.Pz:
                    updateJointAction = () => { TargetPz += resolution; };
                    break;
                case JointSelect.Ry:
                    updateJointAction = () => { TargetState.TaskSpace.Ry += resolution; };
                    break;
                case JointSelect.Rz:
                    updateJointAction = () => { TargetState.TaskSpace.Rz += resolution; };
                    break;
                default:
                    break;
            }

            jogTimer.Elapsed += (source, e) =>
            {
                try
                {
                    updateJointAction();
                    _robotControlService.MoveTo(TargetState);
                }
                catch (ArgumentException exc)
                {
                    _popUpService.Show(exc.Message);
                    jogTimer?.Stop();
                }
            };

            jogTimer.Start();
        }

        public void StopJogContinuous()
        {
            jogTimer?.Stop();
            jogTimer = null;
        }
    }
}
