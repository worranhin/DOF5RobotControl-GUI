using DOF5RobotControl_GUI.Model;

namespace DOF5RobotControl_GUI.Services
{
    public interface IProcessImageService
    {

        /// <summary>
        /// 对图像作预处理，必须在每次相机移动后调用
        /// </summary>
        /// <param name="topFrame">顶部相机图像</param>
        /// <param name="bottomFrame">底部相机图像</param>
        void Init(CamFrame topFrame, CamFrame bottomFrame);

        /// <summary>
        /// 获取夹钳末端到钳口入口处的误差
        /// </summary>
        /// <param name="topImg">顶部相机图像</param>
        /// <returns>误差，单位为 mm / rad</returns>
        Task<(double x, double y, double rz)> GetEntranceErrorAsync(CamFrame topImg);

        /// <summary>
        /// 异步获取夹钳与钳口完成配合处的误差
        /// </summary>
        /// <param name="topImg">顶部相机图像</param>
        /// <returns>误差，单位为 mm / rad</returns>
        Task<(double x, double y, double rz)> GetJawErrorAsync(CamFrame topImg);

        /// <summary>
        /// 异步地处理底部相机的图像，若移动过相机必须先调用 Init()
        /// </summary>
        /// <param name="frame">底部相机图像</param>
        /// <returns>夹钳到钳口库的竖直方向上的距离，单位 mm</returns>
        /// <exception cref="InvalidOperationException"></exception>
        Task<double> ProcessBottomImageAsync(CamFrame frame);
    }
}