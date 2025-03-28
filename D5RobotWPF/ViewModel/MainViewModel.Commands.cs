using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D5R;
using System.Diagnostics;
using System.Windows;

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

        [RelayCommand]
        private void ToggleRecord()
        {
            if (!IsRecording)
            {
                _ = StartRecord();
            }
            else
                StopRecord();
        }


        private async Task StartRecord()
        {
            const int RecordPeriod = 100;

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
                    var topFrame = _cameraCtrlService.GetTopFrame();
                    var bottomFrame = _cameraCtrlService.GetBottomFrame();

                    _dataRecordService.Record(currentJoints, targetJoints, topFrame, bottomFrame); // 记录当前状态和对应的动作
                    Debug.WriteLine("Do one record.");

                    await Task.Delay(RecordPeriod, token);
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
    }
}
