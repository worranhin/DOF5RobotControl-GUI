using CommunityToolkit.Mvvm.Input;
using D5R;
using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using System.Diagnostics;
using System.Windows;

namespace DOF5RobotControl_GUI.ViewModel
{
    partial class MainViewModel
    {
        Timer? updateStateTimer;

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
        private async Task MoveToInitialPosition()
        {
            try
            {
                await MoveToInitialPositionAsync();
            }
            catch (InvalidOperationException ex)
            {
                _popUpService.Show(ex.Message, "Error when go to insertion initial position");
            }
        }

        [RelayCommand]
        private async Task ToggleInsertion()
        {
            if (!IsInserting)
            {
                try
                {
                    await InsertTaskAsync();
                }
                catch (InvalidOperationException ex)
                {
                    _popUpService.Show(ex.Message, "Error When Inserting");
                }
            }
            else  // if inserting, then cancel it
            {
                insertCancelSource?.Cancel();
            }
        }

        /// <summary>
        /// 自动从零点开始装上钳口，再返回零点
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task AttachJawAsync()
        {
            IsAttachingJaw = true;
            attachCancelSource = new();
            var cancelToken = attachCancelSource.Token;

            try
            {
                try
                {
                    cancelToken.ThrowIfCancellationRequested();
                    await MoveToInitialPositionAsync(); // 前往装钳口初始位置
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    _popUpService.Show(ex.Message, "前往初始位置时出现错误");
                    attachCancelSource.Cancel();
                }

                cancelToken.ThrowIfCancellationRequested();
                await InsertTaskAsync(); // 插装钳口
                await Task.Delay(500); // 插入完成，先停一会

                // 此时应处于插入的状态，接下来将夹钳抬起来
                cancelToken.ThrowIfCancellationRequested();
                TargetState.Copy(CurrentState);
                TargetState.TaskSpace.Rz = 0; // 将 Rz 转正用于检测是否接触
                TargetState.TaskSpace.Pz += 1; // 抬起一点距离，使其与底座脱离接触
                _robotControlService.MoveTo(TargetState);
                await Task.Delay(500);

                do
                {
                    cancelToken.ThrowIfCancellationRequested();
                    TargetState.TaskSpace.Px = CurrentState.TaskSpace.Px - 1; // 向后退 1mm，避免与台子前方有挤压
                    _robotControlService.MoveTo(TargetState);
                    await WaitForTargetedAsync();
                } while (Math.Abs(CurrentState.TaskSpace.Rz) > 0.02); // 若与目标值0相差太多，则说明仍有接触，需继续后退

                cancelToken.ThrowIfCancellationRequested();
                TargetState.TaskSpace.Pz += 10;  // 向上抬一段距离，避免发生碰撞
                _robotControlService.MoveTo(TargetState);
                await WaitForTargetedAsync();

                cancelToken.ThrowIfCancellationRequested();
                TargetState.SetFromD5RJoints(ZeroPos); // 返回零点
                _robotControlService.MoveTo(TargetState);
                await WaitForTargetedAsync();
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Attach jaw task is canceled.");
            }
            catch (InvalidOperationException ex)
            {
                _popUpService.Show(ex.Message, "Error When Attach Jaw");
                if (ex.InnerException != null)
                    throw;
            }
            finally
            {
                IsAttachingJaw = false;
                attachCancelSource.Dispose();
                attachCancelSource = null;
            }
        }

        [RelayCommand]
        private async Task DetachJawAsync()
        {
            await Task.Delay(100);
            throw new NotImplementedException();
        }

        [RelayCommand]
        private void OpcConnect()
        {
            _opcService.Connect();
        }

        [RelayCommand]
        private void OpcDisconnect()
        {
            _opcService.Disconnect();
        }

        [RelayCommand]
        private async Task DoRecord()
        {
            _dataRecordService.Start();
            Debug.WriteLine("Start recording");

            for (int i = 0; i < 10; i++)
            {
                var joints = _robotControlService.GetCurrentState().JointSpace;
                var topFrame = _cameraCtrlService.GetTopFrame();
                var bottomFrame = _cameraCtrlService.GetBottomFrame();
                // 根据状态决定行动
                JointSpace deltaJoints = new() { P2 = 1 }; // 模拟决策

                _dataRecordService.Record(joints, deltaJoints, topFrame, bottomFrame); // 记录当前状态和对应的动作
                Debug.WriteLine("Do one record.");
                
                TargetState.JointSpace.Add(deltaJoints);
                _robotControlService.MoveTo(TargetState);
                await Task.Delay(1000);
            }

            _dataRecordService.Stop();
            Debug.WriteLine("Stop recording.");
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
