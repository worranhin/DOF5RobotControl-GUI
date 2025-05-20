using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DOF5RobotControl_GUI.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOF5RobotControl_GUI.ViewModel
{
    partial class MainViewModel
    {
        /***** 振动相关字段/属性 *****/
        [ObservableProperty]
        private bool _isVibrating = false;
        [ObservableProperty]
        private bool _isVibrateHorizontal = true;
        [ObservableProperty]
        private bool _isVibrateVertical = false;
        [ObservableProperty]
        private bool _isVibrateFeed = false;
        [ObservableProperty]
        private double _vibrateAmplitude = 0.02;
        [ObservableProperty]
        private double _vibrateFrequency = 50.0;
        [ObservableProperty]
        private double _feedVelocity = 0.5; // mm/s
        [ObservableProperty]
        private double _feedDistance = 5.0; // mm
        [ObservableProperty]
        private bool _isFeeding = false;

        private CancellationTokenSource? feedCancelSource;
        private Task? feedTask;
        private TaskSpace? positionBeforeFeed;

        /***** 处理振动相关 UI 逻辑 *****/

        [RelayCommand]
        private void ToggleVibrate()
        {
            if (!IsVibrating)
            {
                try
                {
                    _robotControlService.StartVibrate(IsVibrateHorizontal, IsVibrateVertical, VibrateAmplitude, VibrateFrequency);
                    IsVibrating = true;
                }
                catch (ArgumentException exc)
                {
                    if (exc.ParamName == "vibrateVertical")
                        _popUpService.Show(exc.Message, "Error while toggle vibration");
                    else
                        throw;
                }
            }
            else
            {
                _robotControlService.StopVibrate();
                IsVibrating = false;
            }
        }

        /***** 振动进给实验 *****/

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
