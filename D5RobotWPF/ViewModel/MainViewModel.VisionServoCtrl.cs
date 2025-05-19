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

                double x, y, z, rz;
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

                // 2. 再往下降
                currentJoint = _robotControlService.CurrentJoint;
                bottomFrame = _cameraCtrlService.GetBottomFrame();
                z = await _imageService.ProcessBottomImageAsync(bottomFrame);

                targetPose = KineHelper.Forward(currentJoint);
                targetPose.Pz += z;
                targetJoint = KineHelper.Inverse(targetPose);
                TargetState.JointSpace = targetJoint;
                await _robotControlService.MoveAbsoluteAsync(targetJoint, token);

                // 3. 最后进行重复预对准直到精度达到要求
                while (!token.IsCancellationRequested)
                {
                    currentJoint = _robotControlService.CurrentJoint;
                    topFrame = _cameraCtrlService.GetTopFrame();

                    //(x, y, rz) = await _imageService.ProcessTopImgAsync(topFrame);
                    (x, y, rz) = await _imageService.GetEntranceErrorAsync(topFrame);
                    if (token.IsCancellationRequested) break;

                    if (Math.Abs(y) < tolerance
                        && Math.Abs(x) < tolerance
                        && Math.Abs(rz) < tolerance) // 判断预对准是否完成
                        break;

                    targetPose = KineHelper.Forward(currentJoint);
                    targetPose.Px += x;
                    targetPose.Py += y;
                    targetPose.Rz += rz * 180.0 / Math.PI;

                    targetJoint = KineHelper.Inverse(targetPose);
                    TargetState.JointSpace = targetJoint;
                    await _robotControlService.MoveAbsoluteAsync(targetJoint, token);
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
            const double tolerance = 0.3;

            IsInserting = true;
            insertCancelSource = new();
            var token = insertCancelSource.Token;

            try
            {
                //// 开始振动配合 ////
                while (!token.IsCancellationRequested)
                {
                    // 更新状态
                    var currentJoint = _robotControlService.CurrentJoint;
                    var topFrame = _cameraCtrlService.GetTopFrame();
                    var (dx, _, _) = await _imageService.GetJawErrorAsync(topFrame);
                    
                    if (token.IsCancellationRequested) break;

                    if (dx < tolerance) // 若误差小于一定值则退出循环
                        break;

                    // 规划进给轨迹
                    double x0 = currentJoint.P3;
                    double tf = dx / FeedVelocity; // seconds, depends on FeedVelocity
                    double trackX(double t) => x0 + t * FeedVelocity;

                    // 规划振动轨迹
                    double y0 = currentJoint.P2;
                    double z0 = currentJoint.P4;
                    Func<double, double> trackY;
                    Func<double, double> trackZ;

                    if (IsVibrateHorizontal)
                        trackY = t => y0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);
                    else
                        trackY = t => y0;

                    if (IsVibrateVertical)
                        trackZ = t => z0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);
                    else
                        trackZ = t => z0;

                    // 安排一个振动进给任务并等待执行完毕
                    await Task.Run(async () =>
                    {
                        double t = 0;
                        //var targetPose = KineHelper.Forward(currentJoint);
                        Stopwatch sw = Stopwatch.StartNew();

                        while (t < tf && !token.IsCancellationRequested)
                        {
                            t = sw.ElapsedMilliseconds / 1000.0;
                            if (t > tf)
                                break;

                            //targetPose.Px = trackX(t);
                            //targetPose.Py = trackY(t);
                            //targetPose.Pz = trackZ(t);

                            _robotControlService.JointMoveAbsolute(3, trackX(t));
                            _robotControlService.JointMoveAbsolute(2, trackY(t));
                            _robotControlService.JointMoveAbsolute(4, trackZ(t));
                            await _robotControlService.WaitForTargetedAsync(token, 10);
                        }

                        sw.Stop();
                    });
                }
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine("Insertion is canceled: " + ex.Message);
            }
            finally
            {
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
            //StartRecord(10, false);

            try
            {
                await PreAlignJawAsync(); // 前往装钳口初始位置
                cancelToken.ThrowIfCancellationRequested();

                await InsertJawAsync(); // 插装钳口
                await Task.Delay(500, cancelToken); // 插入完成，先停一会
                cancelToken.ThrowIfCancellationRequested();

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
    }
}
