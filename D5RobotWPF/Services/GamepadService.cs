using SharpDX.XInput;
using System.Diagnostics;

namespace DOF5RobotControl_GUI.Services
{
    public class MoveRequestedEventArgs : EventArgs
    {
        /// <summary>移动的关节</summary>
        /// <value>可能的取值为 +x, -x, +y, -y, +z, -z, +ry, -ry, +rz, -rz</value>
        public string Direction { get; set; } = string.Empty;
    }

    public class ErrorOcurredEventArgs : EventArgs
    {
        /// <summary>错误信息</summary>
        public string Error { get; set; } = string.Empty;
    }

    public class GamepadService() : IGamepadService
    {
        public bool GamepadConnected { get; private set; }

        public event EventHandler? SpeedUpRequested;
        public event EventHandler? SpeedDownRequested;
        public event EventHandler<MoveRequestedEventArgs>? MoveRequested;
        public event EventHandler<ErrorOcurredEventArgs>? ErrorOcurred;

        private CancellationTokenSource? inputCancelSource;

        public void Start()
        {
            if (!GamepadConnected)
            {
                GamepadConnected = true;
                inputCancelSource = new();
                Thread thread = new(() => GamepadInputTask(inputCancelSource.Token));
                thread.Start();
            }
        }

        public void Stop()
        {
            inputCancelSource?.Cancel();
            inputCancelSource?.Dispose();
            inputCancelSource = null;
        }

        protected virtual void OnMoveRequested(MoveRequestedEventArgs e)
        {
            EventHandler<MoveRequestedEventArgs>? handler = MoveRequested;
            handler?.Invoke(this, e);
        }

        protected virtual void OnErrorOcurred(ErrorOcurredEventArgs e)
        {
            EventHandler<ErrorOcurredEventArgs>? handler = ErrorOcurred;
            handler?.Invoke(this, e);
        }

        private void GamepadInputTask(CancellationToken token)
        {
            const int ThumbsThreshold = 15000;
            const int TrigerThreshold = 120;
            const int ControlPeriod = 100;

            try
            {
                Debug.WriteLine("Starting gamepad connection...");
                var controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };
                // Get 1st controller available
                Controller? controller = null;
                foreach (var selectControler in controllers)
                {
                    if (selectControler.IsConnected)
                    {
                        controller = selectControler;
                        break;
                    }
                }

                if (controller == null)
                {
                    GamepadConnected = false;
                    throw new InvalidOperationException("No XInput controller installed.");
                }

                Debug.WriteLine("Found a XInput controller available");
                GamepadConnected = true;

                // Poll events from joystick
                while (controller.IsConnected && !token.IsCancellationRequested)
                {
                    var state = controller.GetState();

                    // 判断是否切换速度
                    var res = controller.GetKeystroke(DeviceQueryType.Gamepad, out Keystroke ks);
                    if (res == 0 && ks.Flags == KeyStrokeFlags.KeyDown)
                    {
                        if (ks.VirtualKey == GamepadKeyCode.DPadUp)
                        {
                            SpeedUpRequested?.Invoke(this, EventArgs.Empty);
                            continue;
                        }
                        else if (ks.VirtualKey == GamepadKeyCode.DPadDown)
                        {
                            SpeedDownRequested?.Invoke(this, EventArgs.Empty);
                            continue;
                        }
                    }

                    // 控制移动的判断
                    MoveRequestedEventArgs args = new();

                    if (state.Gamepad.RightThumbX <= -ThumbsThreshold)
                        args.Direction = "+rz";
                    else if (state.Gamepad.RightThumbX >= ThumbsThreshold)
                        args.Direction = "-rz";
                    if (state.Gamepad.LeftThumbX <= -ThumbsThreshold)
                        args.Direction = "+y";
                    else if (state.Gamepad.LeftThumbX >= ThumbsThreshold)
                        args.Direction = "-y";
                    if (state.Gamepad.LeftThumbY <= -ThumbsThreshold)
                        args.Direction = "-x";
                    else if (state.Gamepad.LeftThumbY >= ThumbsThreshold)
                        args.Direction = "+x";
                    if (state.Gamepad.LeftTrigger >= TrigerThreshold)
                        args.Direction = "-z";
                    else if (state.Gamepad.RightTrigger >= TrigerThreshold)
                        args.Direction = "+z";
                    if (state.Gamepad.RightThumbY <= -ThumbsThreshold)
                        args.Direction = "-ry";
                    else if (state.Gamepad.RightThumbY >= ThumbsThreshold)
                        args.Direction = "+ry";

                    if (args.Direction != string.Empty)
                        OnMoveRequested(args);


                    Thread.Sleep(ControlPeriod);  // 控制周期
                }

                // 线程清理
                if (!controller.IsConnected && !token.IsCancellationRequested)
                {
                    throw new InvalidOperationException("Gamepad is disconnected for unknown reason.");
                }
                GamepadConnected = false;
                Debug.WriteLine("Teleop thread exit");
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(ex.ToString());
                ErrorOcurredEventArgs args = new() { Error = ex.ToString() };
                OnErrorOcurred(args);
                GamepadConnected = false;
            }
        }
    }
}
