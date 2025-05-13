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
        /// OPC 相关代码

        [ObservableProperty]
        bool _opcServerIsOn = false;

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

        private void OpcConnect()
        {
            _opcService.Connect();
            OpcServerIsOn = true;
        }

        private void OpcDisconnect()
        {
            _opcService.Disconnect();
            OpcServerIsOn = false;
        }

        public void OpcMapMethod(int method)
        {
            // 创建一个 EventArgs 实例来传递给 ButtonClicked 方法
            switch (method)
            {
                case 1: break;
                case 2: ToggleConnectCommand.Execute(null); break;
                case 3: SetTargetJointsCommand.Execute(ZeroPos); break;
                case 4: SetTargetJointsCommand.Execute(IdlePos); break;
                case 5: SetTargetJointsCommand.Execute(PreChangeJawPos); break;
                case 6: SetTargetJointsCommand.Execute(ChangeJawPos); break;
                case 7: SetTargetJointsCommand.Execute(AssemblePos1); break;
                case 8: SetTargetJointsCommand.Execute(AssemblePos2); break;
                case 9: SetTargetJointsCommand.Execute(AssemblePos3); break;
                case 10: SetTargetJointsCommand.Execute(PreFetchRingPos); break;
                case 11: SetTargetJointsCommand.Execute(FetchRingPos); break;
                case 12: RobotRunCommand.Execute(null); break;
                case 13: RobotStopCommand.Execute(null); break;
                case 14: RobotSetZeroCommand.Execute(null); break;
            }
        }

        /***** 数据采集相关代码 *****/

        [ObservableProperty]
        bool _isRecording = false;

        Task? recordTask;
        CancellationTokenSource? recordCancelSource;

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
                // 获取当前状态
                float[] state = await GetState();

                // 将误差作为模型的输入获取动作
                const float kp = 0.3f;
                var action = policy.Step(state);

                for (int i = 0; i < action.Length; i++)
                {
                    action[i] = action[i] * kp; // 将动作缩放到合理范围
                }

                // 设置相对移动量 + 随机高斯噪声
                const double mean = 0;
                const double std = 0.02 * 1000; // 转为 mm

                JointSpace error = new()
                {
                    R1rad = action[0] + GenerateGaussianNoise(mean, Math.Abs(action[0] * std)),
                    P2 = action[1] * 1000.0 + GenerateGaussianNoise(mean, Math.Abs(action[1] * std)),  // 策略网络的输出单位为 m，控制时转换为 mm
                    P3 = action[2] * 1000.0 + GenerateGaussianNoise(mean, Math.Abs(action[2] * std)),
                    P4 = action[3] * 1000.0 + GenerateGaussianNoise(mean, Math.Abs(action[3] * std)),
                    R5rad = action[4] + GenerateGaussianNoise(mean, Math.Abs(action[4] * std)),
                };

                Debug.WriteLine(error);

                // 执行动作
                await _robotControlService.MoveRelativeAsync(error, token);

                // 记录 state, action
                recorder.Record(state, action);

                // 若达到最大时间，则停止
                var t = sw.ElapsedMilliseconds;
                if (t > maxTime) break;
            }

            recorder.Stop();
            
            // 本地方法定义
            async Task<float[]> GetState()
            {
                CamFrame topImg, bottomImg;
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
                return state;
            }
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
    }
}
