using GxIAPINET;
using OpenCvSharp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DOF5RobotControl_GUI.Model
{
    public class GxCamera
    {
        enum CameraSelect
        {
            TopCamera,
            BottomCamera
        };

        public bool IsOpened { get; private set; } = false;
        public CamFrame LastFrame { get; private set; }

        public event EventHandler<CamFrame>? FrameReceived;

        const string TopCameraMac = "00-21-49-03-4D-95";
        const string BottomCameraMac = "00-21-49-03-4D-94";

        private static readonly ManualResetEvent libInitializedEvent = new(false);
        private readonly string mac;
        private readonly object camOpLock = new();
        private IGXDevice? camera;
        private IGXStream? stream;
        private IGXFeatureControl? featControl;
        private Mat? lastMat;
        private bool usingCallback = false;

        private static readonly ConcurrentDictionary<string, GxCamera> camDict = [];

        private GxCamera(string mac)
        {
            this.mac = mac;
        }

        ~GxCamera()
        {
            Close();
        }

        /// <summary>
        /// Create a new GxCamera instance or an already created one of the given mac. (this function is thread-safe)
        /// 创建或获取一个给定 mac 的 GxCamera 实例（线程安全）
        /// </summary>
        /// <param name="mac">The mac of the camera to be created</param>
        /// <returns>the GxCamera instance of the given mac</returns>
        public static GxCamera Create(string mac)
        {
            return camDict.GetOrAdd(mac, (mac) => new GxCamera(mac));
        }

        public static async Task GxLibInit()
        {
            try
            {
                // 初始化大恒相机库
                IGXFactory.GetInstance().Init();

                IGXFactory.GetInstance().GigEResetDevice(TopCameraMac, GX_RESET_DEVICE_MODE.GX_MANUFACTURER_SPECIFIC_RECONNECT);
                IGXFactory.GetInstance().GigEResetDevice(BottomCameraMac, GX_RESET_DEVICE_MODE.GX_MANUFACTURER_SPECIFIC_RECONNECT);

                await Task.Delay(2000); // 等待相机重连

                // 枚举设备
                List<IGXDeviceInfo> deviceInfos = [];
                IGXFactory.GetInstance().UpdateAllDeviceList(1000, deviceInfos);  // 枚举相机，文档建议在打开相机前先枚举
                if (deviceInfos.Count == 0)
                {
                    throw new CGalaxyException(-3, "No device found.");
                }

                foreach (IGXDeviceInfo info in deviceInfos)
                {
                    Debug.WriteLine(info.GetModelName());
                    Debug.WriteLine(info.GetVendorName());
                }

                // 获取 Interface 信息
                List<IGXInterfaceInfo> gxInterfaceList = new();
                IGXFactory.GetInstance().GetAllInterfaceInfo(gxInterfaceList);
                foreach (IGXInterfaceInfo info in gxInterfaceList)
                {
                    Debug.WriteLine(info.GetModelName());
                    Debug.WriteLine(info.GetVendorName());
                }

                libInitializedEvent.Set();
            }
            catch (CGalaxyException ex)
            {
                if (ex.GetErrorCode() == -3)
                {
                    Debug.WriteLine("初始化相机库时发生错误：" + ex.Message);
                    throw new InvalidOperationException("初始化相机库时发生错误。", ex);
                }
                else
                {
                    Debug.WriteLine("Error code: " + ex.GetErrorCode().ToString());
                    Debug.WriteLine("Error message: " + ex.Message);
                    throw;
                }
            }
        }

        public static void GxLibUninit()
        {
            IGXFactory.GetInstance().Uninit();
            libInitializedEvent.Reset();
        }

        private static void EnsureInitialized()
        {
            if (libInitializedEvent.WaitOne(1000) == false)
                throw new InvalidOperationException("相机库未成功打开，请尝试重启应用。");
        }

        public void Open(bool useCallback = true)
        {
            EnsureInitialized();

            lock (camOpLock)
            {

                if (IsOpened) throw new InvalidOperationException("相机已打开。\nThe camera has opened.");

                try
                {
                    // 打开相机
                    camera = IGXFactory.GetInstance().OpenDeviceByMAC(mac, GX_ACCESS_MODE.GX_ACCESS_EXCLUSIVE);

                    // 采集图像
                    if (camera != null)
                    {
                        UInt32 streamCount = camera.GetStreamCount();
                        if (streamCount < 1)
                        {
                            throw new InvalidOperationException("Stream count of the camera < 1");
                        }

                        stream = camera.OpenStream(0);
                        try
                        {
                            featControl = camera.GetRemoteFeatureControl();
                            GX_DEVICE_CLASS_LIST deviceClass = camera.GetDeviceInfo().GetDeviceClass();

                            // 设置最优包长
                            if (GX_DEVICE_CLASS_LIST.GX_DEVICE_CLASS_GEV == deviceClass)
                            {
                                if (true == featControl.IsImplemented("GevSCPSPacketSize"))
                                {
                                    UInt32 packetSize = stream.GetOptimalPacketSize();
                                    featControl.GetIntFeature("GevSCPSPacketSize").SetValue(packetSize);
                                }
                            }

                            stream.SetAcqusitionBufferNumber(10); // 设置缓存数量，在开采前设置

                            /***** 下面是一些相机配置 *****/

                            {
                                // 设置 buffer 行为（好像这个无效）
                                //if (featControl.IsImplemented("StreamBufferHandlingMode"))
                                //{
                                //    featControl.GetEnumFeature("StreamBufferHandlingMode").SetValue("NewestOnly");
                                //    string s = featControl.GetEnumFeature("StreamBufferHandlingMode").GetValue();
                                //    Debug.Assert(s == "NewestOnly");
                                //}
                                //else
                                //{
                                //    Debug.WriteLine("StreamBufferHandlingMode not supported");
                                //}

                                // 设置采集模式
                                if (featControl.IsImplemented("AcquisitionMode"))
                                {
                                    featControl.GetEnumFeature("AcquisitionMode").SetValue("Continuous");
                                    string s = featControl.GetEnumFeature("AcquisitionMode").GetValue();
                                    Debug.Assert(s == "Continuous");
                                }
                                else
                                {
                                    Debug.WriteLine("AcquisitionMode not supported");
                                }

                                if (featControl.IsImplemented("TriggerSelector") && featControl.IsImplemented("TriggerMode"))
                                {
                                    featControl.GetEnumFeature("TriggerSelector").SetValue("FrameStart"); // 这个是调试软件提供的，不清楚是否必要

                                    // 设置触发模式
                                    if (featControl.IsImplemented("TriggerMode"))
                                    {
                                        featControl.GetEnumFeature("TriggerMode").SetValue("Off");
                                        string s = featControl.GetEnumFeature("TriggerMode").GetValue();
                                        Debug.Assert(s == "Off");
                                    }
                                    else
                                    {
                                        Debug.WriteLine("TriggerMode not supported");
                                    }

                                    // 设置触发源
                                    if (featControl.IsImplemented("TriggerSource"))
                                    {
                                        featControl.GetEnumFeature("TriggerSource").SetValue("Software");
                                        string s = featControl.GetEnumFeature("TriggerSource").GetValue();
                                        Debug.Assert(s == "Software");
                                    }
                                    else
                                    {
                                        Debug.WriteLine("TriggerSource not supported");
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("TriggerSelector not supported");
                                }

                                // 设置采集帧率调节模式：控制采集帧率是否激活
                                //if (featControl.IsImplemented("AcquisitionFrameRateMode"))
                                //{
                                //    featControl.GetEnumFeature("AcquisitionFrameRateMode").SetValue("On");
                                //    string s = featControl.GetEnumFeature("AcquisitionFrameRateMode").GetValue();
                                //    Debug.Assert(s == "On");
                                //}
                                //else
                                //{
                                //    Debug.WriteLine("AcquisitionFrameRateMode not supported");
                                //}

                                // 设置采集帧率值，当采集帧率调节模式为 On 时有效
                                //if (featControl.IsImplemented("AcquisitionFrameRate"))
                                //{
                                //    featControl.GetFloatFeature("AcquisitionFrameRate").SetValue(5.0000);
                                //    double d = featControl.GetFloatFeature("AcquisitionFrameRate").GetValue();
                                //    Debug.Assert(d == 5.0000);
                                //}
                                //else
                                //{
                                //    Debug.WriteLine("AcquisitionFrameRate not supported");
                                //}
                            }

                            /***** 相机配置结束 *****/

                            if (useCallback)
                            {
                                stream.RegisterCaptureCallback(camera, OnFrameCallback); // 这个 API 貌似有 bug，启用了 回调模式后，camera.close() 会使程序卡住！
                                usingCallback = true;
                            }

                            stream.StartGrab();  // 开启流通道
                            featControl.GetCommandFeature("AcquisitionStart").Execute();  // 发送开采命令，必须先开启流通道

                            IsOpened = true;
                        }
                        catch (CGalaxyException ex)
                        {
                            Debug.WriteLine("Error in GxCamera.Open" + ex.Message);
                            throw;
                        }
                    }
                }
                catch (CGalaxyException ex)
                {
                    if (ex.GetErrorCode() == -8)
                    {
                        Debug.WriteLine(ex.Message);
                        throw new InvalidOperationException("打开相机失败，请确认是否被占用（可尝试重新拔出）", ex);
                    }
                    else if (ex.GetErrorCode() == -14)
                    {
                        Debug.WriteLine("Camera timeout: " + ex.Message);
                    }
                    else
                    {
                        Debug.WriteLine("Error code: " + ex.GetErrorCode().ToString());
                        Debug.WriteLine("Error message: " + ex.Message);
                        throw;
                    }
                }
            }
        }

        public void OnFrameCallback(object objPara, IFrameData objIFrameData)
        {
            IGXDevice? objIGXDevice = objPara as IGXDevice;
            if (GX_FRAME_STATUS_LIST.GX_FRAME_STATUS_SUCCESS == objIFrameData.GetStatus())
            {
                // 获取图像数据
                int width = (int)objIFrameData.GetWidth();
                int height = (int)objIFrameData.GetHeight();
                if (width != 2592 || height != 2048)
                {
                    Debug.WriteLine("The size of image is not correct!");
                    return;
                }

                GX_PIXEL_FORMAT_ENTRY emPixelFormat = objIFrameData.GetPixelFormat();
                if (emPixelFormat != GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_MONO8)
                {
                    Debug.WriteLine("Error in OnFrameCallback: 图像格式不对！需要 MONO8");
                    return;
                }

                IntPtr rawBufferPtr = objIFrameData.GetBuffer();
                byte[] buffer = new byte[width * height];
                Marshal.Copy(rawBufferPtr, buffer, 0, width * height);
                LastFrame = new(buffer, width, height);
                FrameReceived?.Invoke(this, LastFrame);
            }
        }

        public void Close()
        {
            lock (camOpLock)
            {
                featControl?.GetCommandFeature("AcquisitionStop").Execute(); // 发送停采命令
                featControl = null;

                stream?.StopGrab();
                stream?.UnregisterCaptureCallback();
                stream?.Close(); // 关闭流通道
                stream = null;

                //camera?.Close();
                //camera = null;

                IsOpened = false;
            }
        }

        public Mat? GetMatFrame()
        {
            const int timeout = 1000; // TODO: 测试并改小这个值

            lock (camOpLock)
            {
                if (IsOpened == false)
                    throw new InvalidOperationException("相机未打开，请先调用 open()");

                if (stream == null)
                    throw new InvalidOperationException("Stream is null, open camera first.");

                if (featControl == null)
                    throw new InvalidOperationException("FeatControl is null, open camera first.");

                if (usingCallback == true)
                    throw new InvalidOperationException("相机使用了回调模式，无法使用 GetMatFrame() 方法。请访问 LastFrame 以获取当前图像");

                featControl.GetCommandFeature("TriggerSoftware").Execute();

                try
                {
                    var frameData = stream.DQBuf(timeout);  // 零拷贝采单帧，超时 timeout ms
                    if (frameData.GetStatus() != GX_FRAME_STATUS_LIST.GX_FRAME_STATUS_SUCCESS)
                        throw new InvalidOperationException("图像帧不完整");

                    try
                    {
                        ulong width = frameData.GetWidth();
                        ulong height = frameData.GetHeight();

                        if (width != 2592 || height != 2048)
                            throw new InvalidOperationException("The size of image is not correct!");
                        var pixelFormat = frameData.GetPixelFormat();
                        if (pixelFormat == GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_MONO8)
                        {

                            var pRaw8Buffer = frameData.ConvertToRaw8(GX_VALID_BIT_LIST.GX_BIT_0_7);
                            lastMat = Mat.FromPixelData((int)height, (int)width, MatType.CV_8U, pRaw8Buffer);
                        }
                        else
                        {
                            Debug.WriteLine("Format error!");
                        }

                        return lastMat;
                    }
                    finally
                    {
                        stream.QBuf(frameData);
                    }
                }
                catch (CGalaxyException ex)
                {
                    if (ex.GetErrorCode() == -14)
                    {
                        Debug.WriteLine("Camera timeout: " + ex.Message);
                        throw new InvalidOperationException("相机采集超时.", ex);
                    }
                    else throw;
                }
            }
        }

        public BitmapSource GetBitmapFrame()
        {
            const int timeout = 1000; // TODO: 测试并改小这个值

            lock (camOpLock)
            {
                if (IsOpened == false)
                    throw new InvalidOperationException("相机未打开，请先调用 open()");

                if (stream == null)
                    throw new InvalidOperationException("Stream is null, open camera first.");

                if (featControl == null)
                    throw new InvalidOperationException("FeatControl is null, open camera first.");

                if (usingCallback == true)
                    throw new InvalidOperationException("相机使用了回调模式，无法使用 GetBitmapFrame() 方法。请访问 LastFrame 以获取当前图像");

                featControl.GetCommandFeature("TriggerSoftware").Execute();

                try
                {
                    var frameData = stream.DQBuf(timeout);  // 零拷贝采单帧，超时 timeout ms
                    if (frameData.GetStatus() != GX_FRAME_STATUS_LIST.GX_FRAME_STATUS_SUCCESS)
                        throw new InvalidOperationException("图像帧不完整");

                    try
                    {
                        int width = (int)frameData.GetWidth();
                        int height = (int)frameData.GetHeight();
                        if (width != 2592 || height != 2048)
                            throw new InvalidOperationException("The size of image is not correct!");

                        var pixelFormat = frameData.GetPixelFormat();
                        if (pixelFormat != GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_MONO8)
                            throw new InvalidOperationException("图像格式错误，要求 MONO8");

                        //var pRaw8Buffer = frameData.ConvertToRaw8(GX_VALID_BIT_LIST.GX_BIT_0_7);
                        // 下面转成 bitmap 格式
                        var rawBufferPtr = frameData.GetBuffer();
                        int size = width * height;
                        System.Windows.Media.PixelFormat pf = PixelFormats.Gray8; // 下面转成 bitmap 格式
                        int rawStride = (width * pf.BitsPerPixel + 7) / 8;
                        BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, pf, null, rawBufferPtr, size, rawStride);

                        return bitmap;
                    }
                    finally
                    {
                        stream.QBuf(frameData);
                    }
                }
                catch (CGalaxyException ex)
                {
                    if (ex.GetErrorCode() == -14)
                    {
                        Debug.WriteLine("Camera timeout: " + ex.Message);
                        throw new InvalidOperationException("相机采集超时.", ex);
                    }
                    else throw;
                }
            }
        }

        public bool TryGetBitmapFrame(out BitmapSource? bitmap)
        {
            try
            {
                bitmap = GetBitmapFrame();
                return true;
            }
            catch (InvalidOperationException)
            {
                bitmap = null;
                return false;
            }
        }

        public static async Task<BitmapSource> GetBitmapFrameAsync()
        {
            await Task.Delay(1000);
            throw new NotImplementedException();
        }

        public CamFrame GetRawFrame()
        {
            const int timeout = 1000; // TODO: 测试并改小这个值

            lock (camOpLock)
            {
                if (IsOpened == false)
                    throw new InvalidOperationException("相机未打开，请先调用 open()");

                if (stream == null)
                    throw new InvalidOperationException("Stream is null, open camera first.");

                if (featControl == null)
                    throw new InvalidOperationException("FeatControl is null, open camera first.");

                if (usingCallback == true)
                    throw new InvalidOperationException("相机使用了回调模式，无法使用 GetRawFrame() 方法。请访问 LastFrame 以获取当前图像");

                featControl.GetCommandFeature("TriggerSoftware").Execute();

                try
                {
                    var frameData = stream.DQBuf(timeout);  // 零拷贝采单帧，超时 timeout ms
                    if (frameData.GetStatus() != GX_FRAME_STATUS_LIST.GX_FRAME_STATUS_SUCCESS)
                        throw new InvalidOperationException("图像帧不完整");

                    try
                    {
                        int width = (int)frameData.GetWidth();
                        int height = (int)frameData.GetHeight();
                        if (width != 2592 || height != 2048)
                            throw new InvalidOperationException("The size of image is not correct!");

                        var pixelFormat = frameData.GetPixelFormat();
                        if (pixelFormat != GX_PIXEL_FORMAT_ENTRY.GX_PIXEL_FORMAT_MONO8)
                            throw new InvalidOperationException("图像格式错误，要求 MONO8");

                        IntPtr rawBufferPtr = frameData.GetBuffer();
                        byte[] buffer = new byte[width * height];
                        Marshal.Copy(rawBufferPtr, buffer, 0, width * height);
                        CamFrame frame = new(buffer, width, height);
                        LastFrame = frame;
                        return frame;
                    }
                    finally
                    {
                        stream.QBuf(frameData);
                    }
                }
                catch (CGalaxyException ex)
                {
                    if (ex.GetErrorCode() == -14)
                    {
                        Debug.WriteLine("Camera timeout: " + ex.Message);
                        throw new InvalidOperationException("相机采集超时.", ex);
                    }
                    else throw;
                }
            }
        }

        public bool TryGetRawFrame(out CamFrame frame)
        {
            try
            {
                frame = GetRawFrame();
                return true;
            }
            catch (InvalidOperationException)
            {
                //frame = null;
                frame = new CamFrame([], 0, 0);
                return false;
            }
        }
    }
}
