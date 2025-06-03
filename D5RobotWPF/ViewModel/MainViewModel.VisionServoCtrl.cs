using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using Org.BouncyCastle.X509;
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


        //private async Task StartPreAlignAsync()

        /// <summary>
        /// 移动到插入初始位置
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [RelayCommand]
        private async Task PreAlignJawAsync()
        {
            const double tolerance = 0.1;
            const double offset_y = 0.1;
            const double offset_z = 0.3;
            //const double EntrancePointX = 5.7;

            preAlignCancelSource = new();
            var token = preAlignCancelSource.Token;

            try
            {
                // 为了安全，先前往便于视觉检测的位置
                TargetState.SetFromD5RJoints(PreChangeJawPos);
                await _robotControlService.MoveAbsoluteAsync(TargetState, token);
                token.ThrowIfCancellationRequested();

                // 初始化图像处理器
                var topFrame = _cameraCtrlService.GetTopFrame();
                var bottomFrame = _cameraCtrlService.GetBottomFrame();
                _imageService.Init(topFrame, bottomFrame);

                //// 处理图像
                //var TopProcessTask = _imageService.GetEntranceErrorAsync(topFrame);
                //var BottomProcessTask = _imageService.ProcessBottomImageAsync(bottomFrame);
                //token.ThrowIfCancellationRequested();

                //var (x, y, rz) = await TopProcessTask; // 获取处理结果
                //var pz = await BottomProcessTask;

                //// 计算目标位置
                //targetPose.Px += x;
                //targetPose.Py += y;
                //targetPose.Pz += pz;

                //var targetJoint = KineHelper.Inverse(targetPose); // 求解逆运动学

                //if (targetJoint.P4 > 12) // 安全检查
                //    throw new InvalidOperationException($"前往初始位置时，关节移动量超出安全范围，请检查！\nP4: {targetJoint.P4}");

                //TargetState.JointSpace = targetJoint;
                //await _robotControlService.MoveAbsoluteAsync(targetJoint, token);  // 控制机器人前往目标位置

                double x, y, z;
                JointSpace currentJoint, targetJoint;
                TaskSpace targetPose;

                // 1. 先前往入口上方
                currentJoint = _robotControlService.CurrentJoint;
                topFrame = _cameraCtrlService.GetTopFrame();
                (x, y, _) = await _imageService.GetEntranceErrorAsync(topFrame);

                targetPose = KineHelper.Forward(currentJoint);
                targetPose.Px += x - 1;
                targetPose.Py += y;
                targetJoint = KineHelper.Inverse(targetPose);
                TargetState.JointSpace = targetJoint;
                await _robotControlService.MoveAbsoluteAsync(targetJoint, token);
                await Task.Delay(500);

                // 2. 再往下降
                currentJoint = _robotControlService.CurrentJoint;
                bottomFrame = _cameraCtrlService.GetBottomFrame();
                z = await _imageService.ProcessBottomImageAsync(bottomFrame);
                if (double.IsNaN(z))
                    throw new ArgumentOutOfRangeException(nameof(z), z, "Error in z is NaN");

                targetPose = KineHelper.Forward(currentJoint);
                targetPose.Pz += z + offset_z;
                targetJoint = KineHelper.Inverse(targetPose);
                TargetState.JointSpace = targetJoint;
                await _robotControlService.MoveAbsoluteAsync(targetJoint, token);
                await Task.Delay(500);

                // 3. 预接触配合
                currentJoint = _robotControlService.CurrentJoint;
                topFrame = _cameraCtrlService.GetTopFrame();
                (x, y, _) = await _imageService.GetEntranceErrorAsync(topFrame);

                targetPose = KineHelper.Forward(currentJoint);
                targetPose.Px += x + 0.2;

                targetJoint = KineHelper.Inverse(targetPose);
                TargetState.JointSpace = targetJoint;
                await _robotControlService.MoveAbsoluteAsync(targetJoint, token);

                // 4. 最后进行重复预对准直到精度达到要求
                const double Kp = 0.5;
                while (!token.IsCancellationRequested)
                {
                    currentJoint = _robotControlService.CurrentJoint;
                    topFrame = _cameraCtrlService.GetTopFrame();

                    (_, y, _) = await _imageService.GetJawErrorAsync(topFrame);
                    y += offset_y;

                    if (Math.Abs(y) < tolerance * 0.2) // 判断预对准是否完成
                        break;

                    targetPose = KineHelper.Forward(currentJoint);
                    targetPose.Py += (y) * Kp;
                    //targetPose.Rz += rz * 180.0 / Math.PI;

                    targetJoint = KineHelper.Inverse(targetPose);
                    TargetState.JointSpace = targetJoint;
                    await _robotControlService.MoveAbsoluteAsync(targetJoint, token);
                    await Task.Delay(500);
                }
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine("Pre-align canceled.\n" + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _popUpService.Show(ex.Message, "Error when Pre-align");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _popUpService.Show(ex.ToString(), "Error when pre algin");
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
            const double tolerance = 0.1;
            const double Kp = 0.5;

            IsInserting = true;
            insertCancelSource = new();
            var token = insertCancelSource.Token;

            var topImg = _cameraCtrlService.GetTopFrame();
            var bottomImg = _cameraCtrlService.GetBottomFrame();
            _imageService.Init(topImg, bottomImg);

            try
            {
                _robotControlService.StartVibrate(IsVibrateHorizontal, IsVibrateVertical, VibrateAmplitude, VibrateFrequency);
                //// 开始振动配合 ////
                while (!token.IsCancellationRequested)
                {
                    // 更新状态
                    var currentJoint = _robotControlService.CurrentJoint;
                    var topFrame = _cameraCtrlService.GetTopFrame();
                    var (dx, dy, _) = await _imageService.GetJawErrorAsync(topFrame);

                    if (dx < tolerance) // 若误差小于一定值则退出循环
                        break;

                    // 1. y 方向先对齐
                    var targetPose = KineHelper.Forward(currentJoint);
                    targetPose.Py += dy * Kp;
                    var targetJoint = KineHelper.Inverse(targetPose);
                    _robotControlService.JointMoveAbsolute(2, targetJoint.P2);
                    _robotControlService.JointMoveAbsolute(3, targetJoint.P3);
                    await _robotControlService.WaitForTargetedAsync(token);

                    // 2.1. 规划进给轨迹
                    currentJoint = _robotControlService.CurrentJoint;
                    var currentPose = KineHelper.Forward(currentJoint);
                    double x0 = currentPose.Px;
                    double xf = Math.Min(dx, 2);
                    double tf = xf / FeedVelocity; // seconds, depends on FeedVelocity
                    double trackX(double t) => x0 + t * FeedVelocity;

                    // 2.2. 规划振动轨迹
                    //double y0 = currentJoint.P2;
                    //double z0 = currentJoint.P4;
                    //Func<double, double> trackY;
                    //Func<double, double> trackZ;

                    //if (IsVibrateHorizontal)
                    //    trackY = t => y0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);
                    //else
                    //    trackY = t => y0;

                    //if (IsVibrateVertical)
                    //    trackZ = t => z0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);
                    //else
                    //    trackZ = t => z0;

                    // 3. 振动进给
                    double t = 0;
                    Stopwatch sw = Stopwatch.StartNew();
                    
                    //await Task.Delay(500);

                    while (t < tf && !token.IsCancellationRequested)
                    {
                        t = sw.ElapsedMilliseconds / 1000.0;
                        if (t > tf)
                            break;

                        //currentJoint = _robotControlService.CurrentJoint;
                        //if (Math.Abs(currentJoint.R1 - targetJoint.R1) > 0.2)
                        //{
                        //    await Task.Delay(500);
                        //    targetPose.Px = KineHelper.Forward(currentJoint).Px - 1;
                        //    targetJoint = KineHelper.Inverse(targetPose);
                        //    await _robotControlService.MoveAbsoluteAsync(targetJoint, token);
                        //    break;
                        //}

                        targetPose.Px = trackX(t);
                        //targetPose.Py = trackY(t);
                        //targetPose.Pz = trackZ(t);

                        targetJoint = KineHelper.Inverse(targetPose);
                        //_robotControlService.MoveAbsolute(targetJoint);
                        _robotControlService.JointMoveAbsolute(2, targetJoint.P2);
                        _robotControlService.JointMoveAbsolute(3, targetJoint.P3);
                        //_robotControlService.JointMoveAbsolute(3, trackX(t));
                        //_robotControlService.JointMoveAbsolute(2, trackY(t));
                        //_robotControlService.JointMoveAbsolute(4, trackZ(t));
                        //await _robotControlService.WaitForTargetedAsync(token, 10);
                        await Task.Delay(100);
                    }

                    sw.Stop();
                }
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine("Insertion is canceled: " + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _popUpService.Show(ex.ToString(), "Error when Insert Jaw");
            }
            finally
            {
                _robotControlService.StopVibrate();

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

            StartRecord(100, true);
            try
            {
                var sw = Stopwatch.StartNew();
                await PreAlignJawAsync(); // 前往装钳口初始位置
                cancelToken.ThrowIfCancellationRequested();

                var t1 = sw.ElapsedMilliseconds;

                await InsertJawAsync(); // 插装钳口
                //await Task.Delay(500, cancelToken); // 插入完成，先停一会
                cancelToken.ThrowIfCancellationRequested();

                var t2 = sw.ElapsedMilliseconds;

                AddLog($"预对准用时：{t1}, 插装用时：{t2 - t1}, 总用时：{t2}");
                return;


                // 此时应处于插入的状态，接下来将夹钳抬起来
                var target = _robotControlService.CurrentState;
                target.TaskSpace.Rz = 0; // 将 Rz 转正用于检测是否接触
                target.TaskSpace.Pz += 1; // 抬起一点距离，使其与底座脱离接触

                TargetState.Copy(target);
                await _robotControlService.MoveAbsoluteAsync(target, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                do
                {
                    target = _robotControlService.CurrentState;
                    target.TaskSpace.Px -= 1; // 向后退 1mm，避免与台子前方有挤压
                    await _robotControlService.MoveAbsoluteAsync(target, cancelToken);
                } while (Math.Abs(_robotControlService.CurrentState.TaskSpace.Rz) > 0.02
                         && !cancelToken.IsCancellationRequested); // 若与目标值0相差太多，则说明仍有接触，需继续后退
                cancelToken.ThrowIfCancellationRequested();

                target = _robotControlService.CurrentState;
                target.TaskSpace.Pz += 10;  // 向上抬一段距离，避免发生碰撞
                await _robotControlService.MoveAbsoluteAsync(target, cancelToken);
                cancelToken.ThrowIfCancellationRequested();

                target.SetFromD5RJoints(ZeroPos); // 返回零点
                await _robotControlService.MoveAbsoluteAsync(target, cancelToken);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _popUpService.Show(ex.ToString(), "前往初始位置时出现错误");
                attachCancelSource.Cancel();
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Attach jaw task is canceled.");
            }
            catch (InvalidOperationException ex)
            {
                _popUpService.Show(ex.ToString(), "Error When Attach Jaw");
                if (ex.InnerException != null)
                    throw;
            }
            finally
            {
                await StopRecordAsync();

                IsAttachingJaw = false;
                attachCancelSource.Dispose();
                attachCancelSource = null;
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
    }
}
