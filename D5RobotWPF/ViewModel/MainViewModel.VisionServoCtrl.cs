using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
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

        private CancellationTokenSource? preAlignCancelSource;
        private CancellationTokenSource? insertCancelSource;
        private CancellationTokenSource? attachCancelSource;


        /// <summary>
        /// 移动到插入初始位置
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [RelayCommand]
        private async Task PreAlignJawAsync()
        {
            const double EntrancePointX = 5.7;

            preAlignCancelSource = new();
            var token = preAlignCancelSource.Token;

            try
            {
                // 为了安全，先前往便于视觉检测的位置
                TargetState.SetFromD5RJoints(PreChangeJawPos);
                _robotControlService.MoveAbsolute(TargetState);
                await _robotControlService.WaitForTargetedAsync(token);
                token.ThrowIfCancellationRequested();

                // 更新关节状态与图像
                var current1 = CurrentState;
                var current = _robotControlService.CurrentState;
                var cj = _robotControlService.CurrentJoint;
                var ct = KineHelper.Forward(cj);

                var target = _robotControlService.CurrentState.TaskSpace.Clone();
                var topFrame = _cameraCtrlService.GetTopFrame();
                var bottomFrame = _cameraCtrlService.GetBottomFrame();

                // 处理图像
                _imageService.Init(topFrame, bottomFrame);

                // 处理图像
                var TopProcessTask = _imageService.GetEntranceErrorAsync(topFrame);
                var BottomProcessTask = _imageService.ProcessBottomImageAsync(bottomFrame);
                token.ThrowIfCancellationRequested();

                var (x, y, rz) = await TopProcessTask; // 获取处理结果
                var pz = await BottomProcessTask;

                // 计算目标位置
                target.Px += x - EntrancePointX;
                target.Py += y;
                target.Pz += pz;

                var targetJoints = KineHelper.Inverse(target); // 求解逆运动学
                var ts = KineHelper.Forward(targetJoints);
                if (targetJoints.P4 > 12) // 安全检查
                    throw new InvalidOperationException($"前往初始位置时，关节移动量超出安全范围，请检查！\nP4: {targetJoints.P4}");
                TargetState.JointSpace = targetJoints;
                _robotControlService.MoveAbsolute(TargetState); // 控制机器人前往目标位置
                await _robotControlService.WaitForTargetedAsync(token);

                // 进行重复预对准
                TargetState.Copy(CurrentState);

                while (!token.IsCancellationRequested)
                {

                    UpdateCurrentState();
                    topFrame = _cameraCtrlService.GetTopFrame();

                    //(x, y, rz) = await _imageService.ProcessTopImgAsync(topFrame);
                    (x, y, rz) = await _imageService.GetEntranceErrorAsync(topFrame);
                    if (token.IsCancellationRequested) break;

                    //Debug.WriteLine($"Fine  x:{x}, y:{y}, z:{rz}");

                    if (Math.Abs(y) < 0.05) // 已完成预对准
                        break;

                    TargetState.TaskSpace.Px += x - EntrancePointX;
                    TargetState.TaskSpace.Py += y;
                    await _robotControlService.MoveAbsoluteAsync(TargetState, token);
                    //_robotControlService.MoveAbsolute(TargetState);
                    //await _robotControlService.WaitForTargetedAsync(token, 100, 0.01);
                }
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine("Pre-align canceled.\n" + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _popUpService.Show(ex.Message, "Error in Pre-align");
            }
            finally
            {
                preAlignCancelSource?.Dispose();
                preAlignCancelSource = null;
            }
        }

        /// <summary>
        /// 异步地完成振动配合过程
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task InsertJawAsync()
        {
            IsInserting = true;
            insertCancelSource = new();
            var token = insertCancelSource.Token;

            try
            {
                //// 开始振动配合 ////
                while (!token.IsCancellationRequested)
                {
                    // 更新状态
                    RoboticState target = _robotControlService.CurrentState;
                    CurrentState = target.Clone();
                    var topFrame = _cameraCtrlService.GetTopFrame();
                    var (dx, _, _) = await _imageService.GetJawErrorAsync(topFrame);
                    
                    if (token.IsCancellationRequested) break;

                    if (dx < 0.05) // 若误差小于一定值则退出循环
                        break;

                    // 规划进给轨迹
                    target.TaskSpace.Px += dx;
                    double x0 = CurrentState.TaskSpace.Px;
                    double xf = target.TaskSpace.Px;
                    double tf = dx / FeedVelocity; // seconds, depends on FeedVelocity
                    double trackX(double t) => x0 + t * FeedVelocity;

                    // 振动轨迹
                    double y0 = CurrentState.TaskSpace.Py;
                    double z0 = CurrentState.TaskSpace.Pz;
                    Func<double, double> trackY;
                    Func<double, double> trackZ;

                    if (IsVibrateHorizontal)
                        trackY = (double t) => y0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);
                    else
                        trackY = (t) => y0;

                    if (IsVibrateVertical)
                        trackZ = (double t) => z0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);
                    else
                        trackZ = (t) => z0;

                    double t = 0;
                    TargetState.Copy(CurrentState);
                    Stopwatch sw = Stopwatch.StartNew();

                    await Task.Run(() =>
                    {
                        do
                        {
                            t = sw.ElapsedMilliseconds / 1000.0;

                            var current = _robotControlService.CurrentState.TaskSpace;
                            var target = current.Clone();
                            target.Px = trackX(t);
                            target.Py = trackY(t);
                            target.Pz = trackZ(t);
                            TargetState.TaskSpace = target;
                            _robotControlService.MoveAbsolute(TargetState);

                            // 记录数据
                            if (_dataRecordService.IsStarted)
                                _dataRecordService.Record(
                                    KineHelper.Inverse(current), KineHelper.Inverse(target),
                                    _cameraCtrlService.GetTopFrame(), _cameraCtrlService.GetBottomFrame());
                            //JointSpace Joint = _robotControlService.GetCurrentState().JointSpace.Clone();
                            //JointSpace targetJoint = _robotControlService.TargetState.JointSpace.Clone();
                            //_dataRecordService.Record(`Joint, targetJoint);
                            //JointSpace deltaJoint = TargetState.JointSpace.Clone().Minus(`Joint);
                            //_dataRecordService.Record(`Joint, deltaJoint, _cameraCtrlService.GetTopFrame(), _cameraCtrlService.GetBottomFrame()); // TODO: 这里的记录过程会影响正常运行，急需解决
                        } while (t < tf && !token.IsCancellationRequested);
                    });

                    _robotControlService.TargetState.TaskSpace.Pz = z0;
                    sw.Stop();
                }
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine("Insertion is canceled: " + ex.Message);
            }
            finally
            {
                //_dataRecordService.Stop();
                //IsRecording = false;

                insertCancelSource?.Dispose();
                insertCancelSource = null;
                IsInserting = false;
            }
        }

        /// <summary>
        /// 自动从零点开始装上钳口，再返回零点
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task AttachJawAsync()
        {
            // 初始化状态量与任务取消源
            IsAttachingJaw = true;
            attachCancelSource = new();
            var cancelToken = attachCancelSource.Token;

            // 开始记录状态动作数据
            StartRecord(10, false);

            try
            {
                try
                {
                    cancelToken.ThrowIfCancellationRequested();
                    await PreAlignJawAsync(); // 前往装钳口初始位置
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    _popUpService.Show(ex.Message, "前往初始位置时出现错误");
                    attachCancelSource.Cancel();
                }

                cancelToken.ThrowIfCancellationRequested();
                await InsertJawAsync(); // 插装钳口
                await Task.Delay(500); // 插入完成，先停一会

                // 此时应处于插入的状态，接下来将夹钳抬起来
                cancelToken.ThrowIfCancellationRequested();
                TargetState.Copy(CurrentState);
                TargetState.TaskSpace.Rz = 0; // 将 Rz 转正用于检测是否接触
                TargetState.TaskSpace.Pz += 1; // 抬起一点距离，使其与底座脱离接触
                _robotControlService.MoveAbsolute(TargetState);
                await Task.Delay(500);

                do
                {
                    cancelToken.ThrowIfCancellationRequested();
                    TargetState.TaskSpace.Px = CurrentState.TaskSpace.Px - 1; // 向后退 1mm，避免与台子前方有挤压
                    _robotControlService.MoveAbsolute(TargetState);
                    await _robotControlService.WaitForTargetedAsync();
                } while (Math.Abs(CurrentState.TaskSpace.Rz) > 0.02); // 若与目标值0相差太多，则说明仍有接触，需继续后退

                cancelToken.ThrowIfCancellationRequested();
                TargetState.TaskSpace.Pz += 10;  // 向上抬一段距离，避免发生碰撞
                _robotControlService.MoveAbsolute(TargetState);
                await _robotControlService.WaitForTargetedAsync();

                cancelToken.ThrowIfCancellationRequested();
                TargetState.SetFromD5RJoints(ZeroPos); // 返回零点
                _robotControlService.MoveAbsolute(TargetState);
                await _robotControlService.WaitForTargetedAsync();
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

                await StopRecordAsync();
                //await recordTask;
                recordTask = null;
            }
        }

        /// <summary>
        /// 自动将钳口放回钳口库中
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [RelayCommand]
        private async Task DetachJawAsync()
        {
            await Task.Delay(100);
            throw new NotImplementedException();
        }

        /// <summary>
        /// 取消当前正在进行的任务
        /// </summary>
        [RelayCommand]
        private void CancelTask()
        {
            preAlignCancelSource?.Cancel();
            insertCancelSource?.Cancel();
            attachCancelSource?.Cancel();
        }

        /// <summary>
        /// 等待直到 CurrentState 与 TargetState 的距离小于一定值
        /// </summary>
        /// <param name="CheckDistance">检查距离，小于该值则返回，单位mm</param>
        /// <param name="CheckPeriod">检查周期，单位ms</param>
        /// <returns></returns>
        [Obsolete("Use RobotControlService.WaitForTargetedAsync() instead.")]
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
