namespace DOF5RobotControl_GUI.Model
{
    public readonly struct CamFrame(byte[] buf, int width, int height)
    {
        public byte[] Buffer { get; } = (byte[])buf.Clone();
        public int Width { get; } = width;
        public int Height { get; } = height;
        public readonly int Size => (Width * Height);

        /// <summary>
        /// Bytes per row
        /// </summary>
        public readonly int Stride => (Width * 8 + 7) / 8;
    }
}
