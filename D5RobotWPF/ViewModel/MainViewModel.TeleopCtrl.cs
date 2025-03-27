using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DOF5RobotControl_GUI.Model;
using DOF5RobotControl_GUI.Services;
using System.Diagnostics;

namespace DOF5RobotControl_GUI.ViewModel
{
    partial class MainViewModel
    {
        DateTime lastTeleMoveTime = DateTime.Now;

        [ObservableProperty]
        private bool _enableGamepad = false;

        [RelayCommand]
        private void ToggleTeleop()
        {
            if (EnableGamepad)
                EnableTeleop();
            else
                DisableTeleop();
        }

        private void EnableTeleop()
        {
            Debug.WriteLine("Enable gamepad.");
            _gamepadService.ErrorOcurred += HandleErrorOccured;
            _gamepadService.MoveRequested += HandleTeleopMove;
            _gamepadService.SpeedUpRequested += HandleSpeedUpRequest;
            _gamepadService.SpeedDownRequested += HandleSpeedDownRequest;
            _gamepadService.Start();
        }



        private void DisableTeleop()
        {
            Debug.WriteLine("Disable gamepad");
            _gamepadService.Stop();
            _gamepadService.ErrorOcurred -= HandleErrorOccured;
            _gamepadService.MoveRequested -= HandleTeleopMove;
        }

        private void HandleSpeedUpRequest(object? sender, EventArgs e)
        {
            Debug.WriteLine("Speed up requested");
            if (JogResolutionSelected > JogResolution.Speed1mm)
            {
                JogResolutionSelected -= 1;
            }
        }

        private void HandleSpeedDownRequest(object? sender, EventArgs e)
        {
            Debug.WriteLine("Speed down requested");
            if (JogResolutionSelected < JogResolution.Speed10um)
            {
                JogResolutionSelected += 1;
            }
        }

        private void HandleTeleopMove(object? sender, MoveRequestedEventArgs e)
        {
            TimeSpan timeFromLastMove = DateTime.Now - lastTeleMoveTime;
            if (timeFromLastMove.TotalSeconds < 1)
            {
                return;
            }

            Debug.WriteLine(e.Direction);
            string dir = e.Direction;
            JogParams jogParams = new() { IsJogTaskSpace = true };
            switch (dir)
            {
                case "+x":
                    jogParams.Joint = JointSelect.Px;
                    jogParams.IsPositive = true;
                    break;
                case "-x":
                    jogParams.Joint = JointSelect.Px;
                    jogParams.IsPositive = false;
                    break;
                case "+y":
                    jogParams.Joint = JointSelect.Py;
                    jogParams.IsPositive = true;
                    break;
                case "-y":
                    jogParams.Joint = JointSelect.Py;
                    jogParams.IsPositive = false;
                    break;
                case "+z":
                    jogParams.Joint = JointSelect.Pz;
                    jogParams.IsPositive = true;
                    break;
                case "-z":
                    jogParams.Joint = JointSelect.Pz;
                    jogParams.IsPositive = false;
                    break;
                case "+ry":
                    jogParams.Joint = JointSelect.Ry;
                    jogParams.IsPositive = true;
                    break;
                case "-ry":
                    jogParams.Joint = JointSelect.Ry;
                    jogParams.IsPositive = false;
                    break;
                case "+rz":
                    jogParams.Joint = JointSelect.Rz;
                    jogParams.IsPositive = true;
                    break;
                case "-rz":
                    jogParams.Joint = JointSelect.Rz;
                    jogParams.IsPositive = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e), $"Direction \'{dir}\' not supported");
            }

            Jog(jogParams);
            lastTeleMoveTime = DateTime.Now;
        }

        private void HandleErrorOccured(object? sender, ErrorOcurredEventArgs e)
        {
            _popUpService.Show(e.Error);
            EnableGamepad = false;
            DisableTeleop();
        }
    }
}
