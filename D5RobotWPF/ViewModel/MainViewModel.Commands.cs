using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlzEx.Theming;
using D5R;
using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using Microsoft.Extensions.DependencyInjection;
using OnnxInferenceLibrary;
using OpenCvSharp;
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

        /// <summary>
        /// 切换数据记录状态
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task ToggleRecord()
        {
            if (!IsRecording)
            {
                StartRecord(10);
            }
            else
            {
                await StopRecordAsync();
            }
        }

        /// <summary>
        /// Start record data periodically
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private void StartRecord(int period = 100, bool recordImage = true)
        {
            if (IsRecording)
                throw new InvalidOperationException("Data has already been recording.");

            IsRecording = true;

            recordCancelSource = new();
            CancellationToken token = recordCancelSource.Token;

            _dataRecordService.Start();
            Debug.WriteLine("Start recording");

            recordTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {

                    Stopwatch sw = Stopwatch.StartNew();
                    var delayTask = Task.Delay(period, token);

                    var currentJoints = _robotControlService.CurrentState.JointSpace.Clone();
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

                    await delayTask;
                }
            });
        }

        /// <summary>
        /// 停止数据记录
        /// Stop record data
        /// </summary>
        /// <returns></returns>
        private async Task StopRecordAsync()
        {
            // 取消记录任务
            recordCancelSource?.Cancel();
            try
            {
                recordTask?.Wait();
            }
            catch (AggregateException ex)
            {
                Debug.WriteLine(ex.Message);
            }
            recordCancelSource?.Dispose();
            recordCancelSource = null;

            // 停止 record 服务
            //_dataRecordService.Stop();
            await _dataRecordService.StopAsync();

            IsRecording = false;
        }

        CancellationTokenSource? collectRlDataCancelSource = null;

        /// <summary>
        /// 采集强化学习数据
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task CollectRLDataAsync()
        {
            const int period = 50; // ms
            const int maxTime = 12000; // ms

            collectRlDataCancelSource = new();
            var token = collectRlDataCancelSource.Token;

            var policy = App.Current.Services.GetRequiredService<ActorPolicy>();
            var recorder = new RlDataCollecter();

            // 初始化图像处理模块
            var topImg = _cameraCtrlService.GetTopFrame();
            var bottomImg = _cameraCtrlService.GetBottomFrame();
            _imageService.Init(topImg, bottomImg);

            recorder.Start();  // 初始化记录器

            var sw = Stopwatch.StartNew();
            while(!token.IsCancellationRequested)
            {
                // 获取当前位姿状态误差
                topImg = _cameraCtrlService.GetTopFrame();
                bottomImg = _cameraCtrlService.GetBottomFrame();

                var topTask = _imageService.ProcessTopImgAsync(topImg);
                var bottomTask = _imageService.ProcessBottomImageAsync(bottomImg);

                var (x, y, rz) = await topTask; // 单位为 mm 和 rad
                var z = await bottomTask;

                double halfTheta = rz / 2.0;
                double w = Math.Cos(halfTheta);  // 计算四元数
                double qz = Math.Sin(halfTheta);
                //float[] state = [((float)x), ((float)y), ((float)z), (float)w, 0, 0, (float)qz];  // 转为神经网络的输入形式（位移+四元数误差）
                float[] state = [((float)x / 1000.0F), ((float)y / 1000.0F), ((float)z / 1000.0F), (float)w, 0, 0, (float)qz]; // 转为神经网络的输入形式（位移+四元数误差） 单位为 m

                // 将误差作为模型的输入获取动作
                var action = policy.Step(state).ToArray();

                // 拷贝当前状态
                TargetState.Copy(CurrentState);
                var joints = TargetState.JointSpace.Clone();

                // 目标关节位置加上网络输出的相对位移量 + 随机高斯噪声
                const double mean = 0;
                const double std = 0.01;

                joints.R1rad += action[0] + GenerateGaussianNoise(mean, std);
                joints.P2 += action[1] * 1000.0 + GenerateGaussianNoise(mean, std);  // 策略网络的输出单位为 m，控制时转换为 mm
                joints.P3 += action[2] * 1000.0 + GenerateGaussianNoise(mean, std);
                joints.P4 += action[3] * 1000.0 + GenerateGaussianNoise(mean, std);
                joints.R5rad += action[4] + GenerateGaussianNoise(mean, std);

                // 执行动作
                TargetState.JointSpace = joints;
                _robotControlService.MoveTo(TargetState);

                // 记录 state, action
                recorder.Record(state, action);

                // 若达到最大时间，则停止
                var t = sw.ElapsedMilliseconds;
                if (t > maxTime) break;

                // 等待一个控制周期时间
                try
                {
                    await Task.Delay(period, token);
                }
                catch (TaskCanceledException)
                {
                    Debug.WriteLine("Collect RL data is canceled.");
                }
            }

            recorder.Stop();
        }

        [RelayCommand]
        private void StopCollectRlData()
        {
            collectRlDataCancelSource?.Cancel();
            collectRlDataCancelSource?.Dispose();
            collectRlDataCancelSource = null;
        }

        /// <summary>
        /// 生成高斯噪声
        /// </summary>
        /// <param name="mean"></param>
        /// <param name="stdDev"></param>
        /// <returns></returns>
        private static double GenerateGaussianNoise(double mean = 0, double stdDev = 1)
        {
            Random random = new();
            // 使用 Box-Muller 变换生成高斯分布的随机数
            double u1 = 1.0 - random.NextDouble(); // [0,1) -> (0,1]
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // 标准正态分布
            return mean + stdDev * randStdNormal; // 调整为指定均值和标准差
        }

        /// <summary>
        /// 获取机器人当前状态并更新 ViewModel 的 CurrentState
        /// </summary>
        private void UpdateCurrentState()
        {
            var currentState = _robotControlService.CurrentState;

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
                Func<double, double> trackX;
                Func<double, double> trackY;
                Func<double, double> trackZ;

                UpdateCurrentState();
                positionBeforeFeed = CurrentState.TaskSpace.Clone();
                RoboticState target = CurrentState.Clone();
                target.TaskSpace.Px += FeedDistance;
                double x0 = CurrentState.TaskSpace.Px;
                double xf = target.TaskSpace.Px;
                double tf = FeedDistance / FeedVelocity; // seconds, 速度为 0.5 mm/s

                if (IsVibrateFeed)
                    trackX = (t) => x0 + t * FeedVelocity + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);
                else
                    trackX = (t) => x0 + t * FeedVelocity;

                double y0 = CurrentState.TaskSpace.Py;
                if (IsVibrateHorizontal)
                    trackY = (t) => y0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);
                else
                    trackY = (t) => y0;

                double z0 = CurrentState.TaskSpace.Pz;
                if (IsVibrateVertical)
                    trackZ = (t) => z0 + VibrateAmplitude * Math.Sin(2 * Math.PI * VibrateFrequency * t);
                else
                    trackZ = (t) => z0;

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
                            TargetState.TaskSpace.Py = trackY(t);
                            TargetState.TaskSpace.Pz = trackZ(t);

                            if (token.IsCancellationRequested)
                                break;

                            _robotControlService.MoveAbsolute(TargetState);
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

            _robotControlService.MoveAbsolute(TargetState);
        }
    }
}
