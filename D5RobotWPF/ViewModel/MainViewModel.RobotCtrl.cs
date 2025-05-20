using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D5R;
using DOF5RobotControl_GUI.Model;
using System.Diagnostics;
using System.Windows;

namespace DOF5RobotControl_GUI.ViewModel
{
    public struct JogParams
    {
        public JogParams() { }

        public JointSelect Joint { get; set; }
        public bool IsPositive { get; set; }
        public bool IsJogTaskSpace { get; set; } = false;
    };

    partial class MainViewModel
    {
        /***** 机器人系统相关 *****/
        [ObservableProperty]
        private bool _systemConnected = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TargetPx))]
        [NotifyPropertyChangedFor(nameof(TargetPy))]
        [NotifyPropertyChangedFor(nameof(TargetPz))]
        private RoboticState _targetState = new(0, 0, 0, 0, 0);

        [ObservableProperty]
        private RoboticState _currentState = new(0, 0, 0, 0, 0);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TargetPx))]
        [NotifyPropertyChangedFor(nameof(TargetPy))]
        [NotifyPropertyChangedFor(nameof(TargetPz))]
        private bool _isPoseRelative = true;

        public double TargetPx
        {
            get
            {
                if (IsPoseRelative)
                    return TargetState.TaskSpace.Px - 72.90;
                else
                    return TargetState.TaskSpace.Px;
            }
            set
            {
                if (IsPoseRelative)
                    TargetState.TaskSpace.Px = value + 72.90;
                else
                    TargetState.TaskSpace.Px = value;

                OnPropertyChanged();
            }
        }

        public double TargetPy
        {
            get
            {
                if (IsPoseRelative)
                    return TargetState.TaskSpace.Py + 88.75;
                else
                    return TargetState.TaskSpace.Py;
            }
            set
            {
                if (IsPoseRelative)
                    TargetState.TaskSpace.Py = value - 88.75;
                else
                    TargetState.TaskSpace.Py = value;

                OnPropertyChanged();
            }
        }

        public double TargetPz
        {
            get
            {
                if (IsPoseRelative)
                    return TargetState.TaskSpace.Pz + 88.46;
                else
                    return TargetState.TaskSpace.Pz;
            }
            set
            {
                if (IsPoseRelative)
                    TargetState.TaskSpace.Pz = value - 88.46;
                else
                    TargetState.TaskSpace.Pz = value;

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 更新机器人关节状态的异步任务
        /// </summary>
        Task? updateTask;

        /// <summary>
        /// // 更新机器人关节状态的取消源（CTS)
        /// </summary>
        CancellationTokenSource? updateCancelSource;

        /***** 机器人控制命令 *****/

        [RelayCommand]
        private void SetTargetJoints(Joints joints)
        {
            TargetState.SetFromD5RJoints(joints);
        }

        [RelayCommand]
        private void SetTargetJointsFromCurrent()
        {
            var joints = CurrentState.ToD5RJoints();
            TargetState.SetFromD5RJoints(joints);
        }

        [RelayCommand]
        private void RobotRun()
        {
            try
            {
                _robotControlService.MoveAbsolute(TargetState);
            }
            catch (InvalidOperationException ex)
            {
                _popUpService.Show(ex.Message, "Error when running");
            }
            catch (ArgumentException ex)
            {
                _popUpService.Show(ex.Message, "Error when running");
            }
            catch (RobotException ex)
            {
                _popUpService.Show($"Error code: {ex.Code}\nError Message: {ex.Message}", "Robot error occurs while running");
            }
        }

        [RelayCommand]
        private void RobotStop()
        {
            // 取消异步任务
            insertCancelSource?.Cancel();
            attachCancelSource?.Cancel();

            foreach (var cancelSource in cancelSourceList)
            {
                cancelSource?.Cancel();
            }

            _robotControlService.Stop();
        }

        [RelayCommand]
        private void RobotSetZero()
        {
            try
            {
                _robotControlService.SetZero();
            } catch (InvalidOperationException ex)
            {
                _popUpService.Show(ex.ToString());
            }
        }

        /***** Jog 点动相关代码 *****/

        public static IEnumerable<JogMode> JogModes => Enum.GetValues(typeof(JogMode)).Cast<JogMode>();
        public static IEnumerable<JogResolution> JogResolutions => Enum.GetValues(typeof(JogResolution)).Cast<JogResolution>();

        public const int natorJogResolution = 30000;
        public const int RMDJogResolution = 20;
        const uint jogPeriod = 20;  // ms

        [ObservableProperty]
        private JogMode _jogModeSelected = JogMode.OneStep;
        [ObservableProperty]
        private JogResolution _jogResolutionSelected = JogResolution.Speed1mm;

        System.Timers.Timer? jogTimer;        

        [RelayCommand]
        private void Jog(JogParams param)
        {
            if (JogModeSelected == JogMode.OneStep)
            {
                double resolution;
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
                        _popUpService.Show("Invalid JogResolutionSelected", "Error in Jog");
                        return;
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

                if (!TargetState.JointSpace.HasErrors && _robotControlService.IsConnected)
                {
                    try
                    {
                        _robotControlService.MoveAbsolute(TargetState);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _popUpService.Show(ex.Message, "Error when jogging");
                    }
                }
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
                    _robotControlService.MoveAbsolute(TargetState);
                }
                catch(InvalidOperationException ex)
                {
                    AddLog(ex.Message);
                }
                catch (ArgumentException ex)
                {
                    _popUpService.Show(ex.ToString());
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

        /***** 更新状态任务 *****/

        private void StartUpdateState()
        {
            updateCancelSource = new();
            var token = updateCancelSource.Token;

            updateTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    UpdateCurrentState();
                    await Task.Delay(1000, token);
                }
            });
        }

        private void StopUpdateState()
        {
            updateCancelSource?.Cancel();
            updateCancelSource?.Dispose();

            try
            {

            }
            catch (AggregateException ex)
            {
                updateTask?.Wait();
                if (ex.InnerException is TaskCanceledException)
                    Debug.WriteLine("Update state task canceled");
                else
                    throw;
            }
        }
    }
}
