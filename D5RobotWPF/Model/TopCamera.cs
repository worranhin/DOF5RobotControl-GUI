namespace DOF5RobotControl_GUI.Model
{
    internal sealed class TopCamera : GxCamera
    {
        private TopCamera() : base(TopCameraMac) { }

        const string TopCameraMac = "00-21-49-03-4D-95";
        private static readonly Lazy<TopCamera> _instance = new(() => new TopCamera());
        public static TopCamera Instance => _instance.Value;
    }
}
