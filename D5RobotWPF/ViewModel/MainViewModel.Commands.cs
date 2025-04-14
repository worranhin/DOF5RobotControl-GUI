using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D5R;
using DOF5RobotControl_GUI.Model;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using VisionLibrary;

namespace DOF5RobotControl_GUI.ViewModel
{
    partial class MainViewModel
    {
        Timer? updateStateTimer;

        [ObservableProperty]
        bool _opcServerIsOn = false;

        /***** 机器人控制命令 *****/

        [RelayCommand]
        private static void OpenConfigWindow()
        {
            ConfigWindow window = new()
            {
                Owner = Application.Current.MainWindow
            };
            window.Show();
        }

        [RelayCommand]
        private void ToggleConnect()
        {
            if (SystemConnected)  // 如果目前系统已连接，则断开连接
            {
                _robotControlService.Disconnect();
                _cameraCtrlService.DisconnectCamMotor();
                updateStateTimer?.Dispose();
                SystemConnected = false;
            }
            else  // 系统未连接，则建立连接
            {
                try
                {
                    _robotControlService.Connect(Properties.Settings.Default.RmdPort);
                    _cameraCtrlService.ConnectCamMotor(Properties.Settings.Default.CamMotorPort);
                    SystemConnected = true;
                    updateStateTimer = new(state => UpdateCurrentState(), null, 500, 1000);
                }
                catch (InvalidOperationException exc)
                {
                    _robotControlService.Disconnect();
                    _cameraCtrlService.DisconnectCamMotor();
                    _popUpService.Show(exc.Message);
                }
            }
        }

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
                _robotControlService.MoveTo(TargetState);
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
            _robotControlService.SetZero();
        }

        [RelayCommand]
        private void ToggleOpcServer()
        {
            if (!OpcServerIsOn)
            {
                OpcConnect();
            }
            else
            {
                OpcDisconnect();
            }
        }

        [RelayCommand]
        private void OpcConnect()
        {
            _opcService.Connect();
            OpcServerIsOn = true;
        }

        [RelayCommand]
        private void OpcDisconnect()
        {
            _opcService.Disconnect();
            OpcServerIsOn = false;
        }

        /***** 数据记录 *****/

        [ObservableProperty]
        bool _isRecording = false;

        CancellationTokenSource? recordCancelSource;
        Task? recordTask;

        [RelayCommand]
        private async Task ToggleRecord()
        {
            if (!IsRecording)
            {
                recordTask = StartRecordAsync();
            }
            else
            {
                StopRecord();

                if (recordTask != null)
                {
                    await recordTask;
                    recordTask = null;
                }
            }
        }

        /// <summary>
        /// Start record data periodically
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task StartRecordAsync(int period = 100, bool recordImage = true)
        {
            if (IsRecording)
                throw new InvalidOperationException("Data has already been recording.");

            IsRecording = true;
            recordCancelSource = new();
            CancellationToken token = recordCancelSource.Token;

            try
            {
                _dataRecordService.Start();
                Debug.WriteLine("Start recording");

                while (!token.IsCancellationRequested)
                {
                    var currentJoints = _robotControlService.GetCurrentState().JointSpace.Clone();
                    var targetJoints = _robotControlService.TargetState.JointSpace.Clone();

                    if (recordImage)
                    {
                        var topFrame = _cameraCtrlService.GetTopFrame();
                        var bottomFrame = _cameraCtrlService.GetBottomFrame();

                        _dataRecordService.Record(currentJoints, targetJoints, topFrame, bottomFrame); // 记录当前状态和对应的动作
                    }
                    else
                    {
                        _dataRecordService.Record(currentJoints, targetJoints);
                    }

                    await Task.Delay(period, token);
                }
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                _dataRecordService.Stop();
                Debug.WriteLine("Stop recording.");

                recordCancelSource.Dispose();
                recordCancelSource = null;

                IsRecording = false;
            }
        }

        private void StopRecord()
        {
            recordCancelSource?.Cancel();
        }

        /// <summary>
        /// 获取机器人当前状态并更新 ViewModel 的 CurrentState
        /// </summary>
        private void UpdateCurrentState()
        {
            var currentState = _robotControlService.GetCurrentState();

            Dispatcher.Invoke(() =>
            {
                try
                {
                    CurrentState = currentState;
                }
                catch (ArgumentException exc)
                {
                    if (exc.ParamName == "joint")
                        Debug.WriteLine(exc.Message);
                }
            });
        }

        /***** 振动进给实验 *****/
        [ObservableProperty]
        private double _feedVelocity = 0.5; // mm/s
        [ObservableProperty]
        private double _feedDistance = 5.0; // mm
        [ObservableProperty]
        private bool _isFeeding = false;

        private CancellationTokenSource? feedCancelSource;
        private Task? feedTask;
        private TaskSpace? positionBeforeFeed;

        [RelayCommand]
        private async Task ToggleFeed()
        {
            if (!IsFeeding)
                feedTask = StartFeedAsync();
            else
            {
                StopFeed();
                if (feedTask != null)
                    await feedTask;
            }
        }

        private async Task StartFeedAsync()
        {
            IsFeeding = true;
            feedCancelSource = new();
            var token = feedCancelSource.Token;

            try
            {
                // 定义轨迹
                UpdateCurrentState();
                positionBeforeFeed = CurrentState.TaskSpace.Clone();
                RoboticState target = CurrentState.Clone();
                target.TaskSpace.Px += FeedDistance;
                double x0 = CurrentState.TaskSpace.Px;
                double xf = target.TaskSpace.Px;
                double tf = FeedDistance / FeedVelocity; // seconds, 速度为 0.5 mm/s

                Func<double, double> trackX;
                if (IsVibrateFeed)
                {
                    trackX = (double t) => x0 + t * FeedVelocity + +VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);
                }
                else
                {
                    //double trackX(double t) => x0 + t * FeedVelocity;
                    trackX = (double t) => x0 + t * FeedVelocity;
                }

                double y0 = CurrentState.TaskSpace.Py;
                double trackY(double t) => y0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);

                double z0 = CurrentState.TaskSpace.Pz;
                double trackZ(double t) => z0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);

                double t = 0;
                TargetState.Copy(CurrentState);
                Stopwatch sw = Stopwatch.StartNew();

                try
                {
                    // 开启一个进给任务
                    await Task.Run(() =>
                    {
                        do
                        {
                            t = sw.ElapsedMilliseconds / 1000.0;
                            TargetState.TaskSpace.Px = trackX(t);
                            if (IsVibrateHorizontal)
                                TargetState.TaskSpace.Py = trackY(t);
                            if (IsVibrateVertical)
                                TargetState.TaskSpace.Pz = trackZ(t);

                            if (token.IsCancellationRequested)
                                break;

                            _robotControlService.MoveTo(TargetState);
                        } while (t < tf);
                    });
                }
                catch (OperationCanceledException ex)
                {
                    Debug.WriteLine("Insertion is canceled: " + ex.Message);
                }
                finally
                {
                    sw.Stop();
                }
            }
            finally
            {
                feedCancelSource?.Dispose();
                feedCancelSource = null;
                IsFeeding = false;
            }
        }

        private void StopFeed()
        {
            feedCancelSource?.Cancel();
        }

        [RelayCommand]
        private void Retreat()
        {
            if (positionBeforeFeed != null)
            {
                TargetState.TaskSpace.Copy(positionBeforeFeed);
            }
            else
            {
                UpdateCurrentState();
                TargetState.Copy(CurrentState);
                TargetState.TaskSpace.Px -= FeedDistance;
            }

            _robotControlService.MoveTo(TargetState);
        }
    }
}
