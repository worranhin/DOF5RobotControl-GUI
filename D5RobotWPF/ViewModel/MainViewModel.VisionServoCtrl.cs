using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DOF5RobotControl_GUI.Model;
using System.Diagnostics;
using VisionLibrary;

namespace DOF5RobotControl_GUI.ViewModel
{
    partial class MainViewModel
    {
        [ObservableProperty]
        private bool _isInserting = false;
        [ObservableProperty]
        private bool _isAttachingJaw = false;

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

        /// <summary>
        /// 移动到插入初始位置
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task MoveToInitialPositionAsync()
        {
            // 为了安全，先前往便于视觉检测的位置
            TargetState.SetFromD5RJoints(PreChangeJawPos);
            _robotControlService.MoveTo(TargetState);
            using (CancellationTokenSource cancelSource = new())
            {
                cancelSourceList.Add(cancelSource);
                var token = cancelSource.Token;
                while (!token.IsCancellationRequested && TaskSpace.Distance(TargetState.TaskSpace, CurrentState.TaskSpace) > 1) // 确保已到位
                {
                    Debug.WriteLine(TaskSpace.Distance(TargetState.TaskSpace, CurrentState.TaskSpace));

                    await Task.Delay(1000);
                    UpdateCurrentState();
                }
                cancelSourceList.Remove(cancelSource);
            }

            // 下面获取图像信息
            try
            {
                UpdateCurrentState();
                var topFrame = TopCamera.Instance.LastFrame;
                var bottomFrame = BottomCamera.Instance.LastFrame;

                var (x, y, rz) = await ImageProcessor.ProcessTopImgAsync(topFrame.Buffer, topFrame.Width, topFrame.Height, topFrame.Stride, MatchingMode.ROUGH);
                double pz = await ImageProcessor.ProcessBottomImgAsync(bottomFrame.Buffer, bottomFrame.Width, bottomFrame.Height, bottomFrame.Stride);

                TaskSpace error = new() { Px = x, Py = y, Pz = pz, Ry = 0, Rz = 0 };
                Debug.WriteLine(error);

                JointSpace deltaJoint = KineHelper.InverseDifferential(error, CurrentState.TaskSpace);

                if (deltaJoint.P4 + CurrentState.JointSpace.P4 > 11)  // 对关节移动量进行安全检查
                {
                    Debug.WriteLine(deltaJoint);
                    throw new InvalidOperationException("前往初始位置时，关节移动量超出安全范围，请检查！");
                }

                TargetState.JointSpace.Copy(CurrentState.JointSpace).Add(deltaJoint); // 设置目标位置
                Debug.WriteLine(TargetState.JointSpace);
                _robotControlService.MoveTo(TargetState); // 前往目标位置
                await WaitForTargetedAsync();

                //// 前往振动开始点 ////
                using CancellationTokenSource cancelSource = new();
                cancelSourceList.Add(cancelSource);
                TargetState.Copy(CurrentState);

                while (!cancelSource.Token.IsCancellationRequested)
                {
                    const double VibratePointX = 4;
                    UpdateCurrentState();
                    topFrame = TopCamera.Instance.LastFrame;
                    (x, y, rz) = await ImageProcessor.ProcessTopImgAsync(topFrame.Buffer, topFrame.Width, topFrame.Height, topFrame.Stride, MatchingMode.FINE);
                    Debug.WriteLine($"Fine  x:{x}, y:{y}, z:{rz}");
                    if (Math.Abs(y) < 0.05)
                        break;

                    TargetState.TaskSpace.Px += x - VibratePointX;
                    TargetState.TaskSpace.Py += y;
                    TargetState.TaskSpace.Rz = 0; // 将夹钳带动钳口转正
                    _robotControlService.MoveTo(TargetState);
                    await WaitForTargetedAsync(0.01);
                }
                cancelSourceList.Remove(cancelSource);
            }
            catch (OverflowException ex)
            {
                Debug.WriteLine("Error in MoveToInitialPosition: " + ex.Message);
                throw;
            }
        }

        private async Task InsertTaskAsync()
        {
            object robotMoveLock = new();

            try
            {
                IsInserting = true;
                insertCancelSource = new();
                insertCancelToken = insertCancelSource.Token;

                _dataRecordService.Start();

                //// 开始振动并插入 ////
                while (!insertCancelToken.IsCancellationRequested)
                {
                    UpdateCurrentState();
                    var topFrame = TopCamera.Instance.LastFrame;
                    var (dx, dy, drz) = await ImageProcessor.ProcessTopImgAsync(topFrame.Buffer, topFrame.Width, topFrame.Height, topFrame.Stride, MatchingMode.FINE);

                    if (dx < 0.05) // 若误差小于一定值则退出循环
                        break;

                    RoboticState target = CurrentState.Clone();
                    target.TaskSpace.Px += dx;
                    double x0 = CurrentState.TaskSpace.Px;
                    double xf = target.TaskSpace.Px;
                    double tf = dx / 0.5; // seconds, 速度为 0.5 mm/s
                    double trackX(double t) => x0 + t * (xf - x0) / tf;

                    double y0 = CurrentState.TaskSpace.Py;
                    double trackY(double t) => y0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);

                    double z0 = CurrentState.TaskSpace.Pz;
                    double trackZ(double t) => z0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);

                    double t = 0;
                    TargetState.Copy(CurrentState);
                    Stopwatch sw = Stopwatch.StartNew();

                    await Task.Run(() =>
                    {
                        do
                        {
                            t = sw.ElapsedMilliseconds / 1000.0;
                            TargetState.TaskSpace.Px = trackX(t);
                            TargetState.TaskSpace.Py = trackY(t);
                            TargetState.TaskSpace.Pz = trackZ(t);
                            insertCancelToken.ThrowIfCancellationRequested();
                            _robotControlService.MoveTo(TargetState);

                            // 记录数据
                            JointSpace currentJoint = _robotControlService.GetCurrentState().JointSpace;
                            JointSpace deltaJoint = TargetState.JointSpace.Clone().Minus(currentJoint);
                            _dataRecordService.Record(currentJoint, deltaJoint, _cameraCtrlService.GetTopFrame(), _cameraCtrlService.GetBottomFrame());
                        } while (t < tf);
                    });
                    sw.Stop();
                }
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine("Insertion is canceled: " + ex.Message);
            }
            finally
            {
                _dataRecordService.Stop();

                insertCancelSource?.Dispose();
                insertCancelSource = null;
                IsInserting = false;
            }
        }

        /// <summary>
        /// 等待直到 CurrentState 与 TargetState 的距离小于一定值
        /// </summary>
        /// <param name="CheckDistance">检查距离，小于该值则返回，单位mm</param>
        /// <param name="CheckPeriod">检查周期，单位ms</param>
        /// <returns></returns>
        private async Task WaitForTargetedAsync(double CheckDistance = 0.1, int CheckPeriod = 100)
        {
            using CancellationTokenSource waitCancelSource = new();
            cancelSourceList.Add(waitCancelSource);
            var token = waitCancelSource.Token;
            while (!token.IsCancellationRequested && TaskSpace.Distance(CurrentState.TaskSpace, TargetState.TaskSpace) > CheckDistance)
            {
                await Task.Delay(CheckPeriod);
                UpdateCurrentState();
            }
            cancelSourceList.Remove(waitCancelSource);
        }
    }
}
