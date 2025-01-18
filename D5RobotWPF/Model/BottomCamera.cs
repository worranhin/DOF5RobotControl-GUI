namespace DOF5RobotControl_GUI.Model
{
    internal sealed class BottomCamera : GxCamera
    {
        BottomCamera() : base(BottomCameraMac) { }

        const string BottomCameraMac = "00-21-49-03-4D-94";
        private static readonly Lazy<BottomCamera> _instance = new(() => new BottomCamera());
        public static BottomCamera Instance => _instance.Value;
    }
}
