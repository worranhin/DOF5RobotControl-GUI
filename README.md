## 依赖项

- Nator 电机控制 Dll
- Galaxy GxIAPINET.dll

### 配置大恒相机 DLL

右键项目依赖项 -> 添加项目引用 -> 浏览 -> 找到大恒安装目录下的 APIDLL\Win64\.NET6.0\GxIAPINET.dll -> 打上勾

## TODO

- [ ] 现在存在一个问题，在加入 OPCUA 模块后，当关闭窗口的时候，进程并不会结束，内存依然在占用，急需解决！
- [ ] 添加两个控制相机电机移动的按钮