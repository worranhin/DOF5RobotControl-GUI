## 硬件配置

4. [大恒相机水星一代](https://www.daheng-imaging.com/downloads/)
5. Nartor 电机
6. RMD 脉塔电机

## 项目依赖

本解决方案的依赖如下：
- DOF5RobotControl_GUI (C#)
	- D5RobotController_CLR (C++ / CLR)
		- Nators SDK lib
		- RMD 电机驱动
	- VisionLibrary (C++)
		- Halcon SDK lib 
		- OpenCV lib
	- Daheng 相机 SDK
	- （其它依赖由 Nuget 包管理）

因存在上述依赖，故运行本项目需要作一些配置，如下文所示。

## 环境配置

> 本项目所需使用的库文件可以在 [Release](https://github.com/drawal001/D5RC_VS/releases/tag/v0.1.0) 中找到，需要的可自行下载，并按下文内容配置。

**需要注意的是：**

2. 大恒相机需下载其驱动软件，[链接在此](https://www.daheng-imaging.com/downloads/)，且需要工业级以太网视觉采集卡，若出现相机可识别，但无法打开的问题，可使用大恒相机 SDK 中的`GxGigEIPConfig.exe`重新配置 IP
3. halcon 需每月获取许可文件并更名为 **license.dat**，放置在 D5RC_VS.sln 所在的文件目录下

### Nators 电机配置

1. 在项目目录下找到 `D5RobotController_CLR\lib\Nators\SDK1.4.12` 按提示操作。

### RMD 电机配置

RMD 电机通过 USB 串口转 RS485 控制，无需 SDK，但可能需要相应的驱动，并自行查看对应的 COM 口。

### Halcon 配置

1. 去[官网](https://www.mvtec.com/cn/downloads)安装 halcon 24.11
1. 确认拥有 C++ 11 以上版本的工具链
1. 配置项目环境（本项目已配置完成，如遇到问题可参考解决）
	1. 确认添加包含目录  `$(HALCONROOT)\include` `$(HALCONROOT)\include\halconcpp`
	1. 确认 C++ 的链接库包含 `$(HALCONROOT)\lib\$(HALCONARCH)\halcon.lib` 和 `$(HALCONROOT)\lib\$(HALCONARCH)\halconcpp.lib`
	1. 确认 dll 路径已包含在环境变量 PATH 中 `$(HALCONROOT)\bin\%HALCONARCH%\` （正常情况下安装 Halcon 时它会自动完成的，但如果程序跑不了的话可以检查一下）

> `$(HALCONROOT)` 表示环境变量，可能在不同操作系统或 IDE 会有不同表示方式

> 更多信息参考官方文档 [Programer's Guide](https://www.mvtec.com/fileadmin/Redaktion/mvtec.com/products/halcon/documentation/halcon/programmers_guide.pdf)（7.5节）

### OpenCV 配置

1. 安装 OpenCV
1. 设置环境变量： `setx OpenCV_DIR D:\path\to\opencv-4.10.0\opencv\build\x64\vc16` （自行修改为 OpenCV 所在目录）
1. 添加 OpenCV 的 DLL 文件位置 `%OpenCV_DIR%\bin` 到系统环境变量 PATH 中  
1. 配置项目环境（本项目已配置完成，如遇到问题可参考解决）
	1. 添加包含目录 `$(OpenCV_DIR)\..\..\include`
	1. 添加库目录 `$(OPENCV_DIR)\lib`
	1. 添加导入库 `opencv_world4100d.lib` **或** `opencv_world4100.lib` （后缀 `d` 表示这是 debug 需要的库，如 `imshow` 这类函数需要它，它和无 `d` 后缀的库一般不同时存在）

> 更多信息参考官方文档 [Installation in Windows](https://docs.opencv.org/4.x/d3/d52/tutorial_windows_install.html#tutorial_windows_install_path) 以及 [How to build applications with OpenCV inside the "Microsoft Visual Studio"](https://docs.opencv.org/4.x/dd/d6e/tutorial_windows_visual_studio_opencv.html)

### 配置大恒相机 DLL

右键项目依赖项 -> 添加项目引用 -> 浏览 -> 找到大恒安装目录下的 APIDLL\Win64\.NET6.0\GxIAPINET.dll -> 打上勾

重装 SDK 后可能会遇到相机传入的画面有条纹且很卡的问题，这时候可以试试卸载网卡后重新启动电脑，将网卡设置回到初始值，然后用SDK软件重新配置 IP，除此外不要再碰网络设置，这样就有可能能跑了。

## TODO

- [x] 现在存在一个问题，在加入 OPCUA 模块后，当关闭窗口的时候，进程并不会结束，内存依然在占用，急需解决！
- [x] 添加两个控制相机电机移动的按钮
- [x] 目前实时获取关节状态有个问题，可能因为多线程的问题，导致正在获取关节时若发送了运行的指令，会导致 RMD 电机的获取关节出错，可以考虑添加一个互斥锁解决。
- [x] 处理相机获取的图像会在视觉处理时产生异常的问题
- [ ] 添加自动放回钳口的功能
- [ ] 添加使用 HTTP 服务器的控制 API
