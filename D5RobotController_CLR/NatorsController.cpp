#include "pch.h"
#include "NatorsController.h"
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>

namespace D5R {


	NatorsController::NatorsController(String^ id)
	{
		using namespace msclr::interop;

		auto res = NT_OK;
		_id = id;

		if (String::IsNullOrEmpty(_id))
			throw gcnew InvalidOperationException("Id should not be null or empty");

		// Convert System::String^ to const char*
		marshal_context^ context = gcnew marshal_context();
		const char* id_cstr = context->marshal_as<const char*>(_id);

		pin_ptr<NT_INDEX> pHandle = &_handle;
		res = NT_OpenSystem(pHandle, id_cstr, "sync");  // 开启系统
		CheckResult(res, "Failed to init device");

		res = NT_SetHCMEnabled(_handle, NT_HCM_ENABLED);  // 使能手动控制模块
		CheckResult(res, "Failed to set HCM enabled");

		res = NT_SetSensorEnabled_S(_handle, NT_SENSOR_ENABLED);  // 使能传感器
		CheckResult(res, "Failed to set sensor enabled");

		// 设置相对运动不累加 Set Accumulate relative positions
		res = NT_SetAccumulateRelativePositions_S(_handle, NTU_AXIS_X, NT_NO_ACCUMULATE_RELATIVE_POSITIONS);
		CheckResult(res, "Failed to set relative move accumulation");

		res = NT_SetAccumulateRelativePositions_S(_handle, NTU_AXIS_Y, NT_NO_ACCUMULATE_RELATIVE_POSITIONS);
		CheckResult(res, "Failed to set relative move accumulation");

		res = NT_SetAccumulateRelativePositions_S(_handle, NTU_AXIS_Z, NT_NO_ACCUMULATE_RELATIVE_POSITIONS);
		CheckResult(res, "Failed to set relative move accumulation");
	}

	NatorsController::~NatorsController()
	{
		NT_CloseSystem(_handle);
	}

	/// <summary>
	/// 查询 NanoMotor 的位置
	/// </summary>
	/// <param name="axis">轴，从基座到末端为 1-3</param>
	/// <returns>位置值，单位为纳米</returns>
	int NatorsController::GetPosition(int axis)
	{
		auto channel = MapAxisToChannel(axis);
		int position = 0;
		auto result = NT_GetPosition_S(_handle, channel, &position);

		if (result != NT_OK)
			throw gcnew InvalidOperationException(
				String::Format("Failed to get axis position, error code: {}", result));

		return position;
	}

	/// <summary>
	/// 步进运动
	/// </summary>
	/// <param name="axis">运动的轴 1-3</param>
	/// <param name="steps">运动步数</param>
	/// <param name="amplitude">幅值</param>
	/// <param name="frequency">频率</param>
	void NatorsController::StepMove(int axis, signed int steps, unsigned int amplitude, unsigned int frequency)
	{
		NT_INDEX channel = MapAxisToChannel(axis);

		auto status = NT_StepMove_S(_handle, channel, steps, amplitude, frequency);

		if (status != NT_OK)
		{
			String^ message = String::Format("Failed to step move, error status: {}", status);
			throw gcnew InvalidOperationException(message);
		}
	}

	/// <summary>
	/// 停止电机运动
	/// </summary>
	/// <param name="axis">选择电机轴，范围 1-3</param>
	void NatorsController::Stop(int axis)
	{
		auto channel = MapAxisToChannel(axis);
		auto result = NT_Stop_S(_handle, channel);
		CheckResult(result, "Fail to stop motor");
	}

	/// <summary>
	/// 获取运动状态
	/// </summary>
	/// <param name="axis"></param>
	/// <returns></returns>
	unsigned int NatorsController::GetStatus(int axis)
	{
		NT_INDEX channel = MapAxisToChannel(axis);

		unsigned int status = 0;
		auto result = NT_GetStatus_S(_handle, channel, &status);

		if (result != NT_OK)
		{
			if (result != NT_OK)
				throw gcnew InvalidOperationException(
					String::Format("Failed to get status, error code: {}", result));
		}

		return status;
	}

	unsigned int NatorsController::GetChannelCount()
	{
		unsigned int channels;
		auto result = NT_GetNumberOfChannels(_handle, &channels);  // 获取通道数
		CheckResult(result, "Failed to get number of channels");
		return channels;
	}

	/// <summary>
	/// 设置当前位置
	/// </summary>
	/// <param name="axis">选择设置的轴，范围 1-3</param>
	/// <param name="position">定义当前位置为该值</param>
	void NatorsController::SetPosition(int axis, signed int position)
	{
		auto channel = MapAxisToChannel(axis);
		auto result = NT_SetPosition_S(_handle, channel, position);
		CheckResult(result, "Fail to set position");
	}

	/// <summary>
	/// 移动电机到指定位置
	/// </summary>
	/// <param name="axis">移动的电机，范围 1-3</param>
	/// <param name="position">移动的绝对位置，单位纳米</param>
	void NatorsController::MoveAbsolute(int axis, int position)
	{
		auto channel = MapAxisToChannel(axis);
		auto result = NT_GotoPositionAbsolute_S(_handle, channel, position, 0);  // 文档说明最后一个参数不可调
		CheckResult(result, "Failed to move to absolute position");
	}

	/// <summary>
	/// 相对移动
	/// </summary>
	/// <param name="axis">要移动的电机轴，范围 1-3</param>
	/// <param name="diff">相对运动的距离，单位纳米</param>
	void NatorsController::MoveRelative(int axis, int diff)
	{
		auto channel = MapAxisToChannel(axis);
		auto result = NT_GotoPositionRelative_S(_handle, channel, diff, 0);  // 文档说明最后一个参数不可调
		CheckResult(result, "Failed to move relatively");
	}

	/// <summary>
	/// 扫描模式移动到绝对位置
	/// </summary>
	/// <param name="axis">选择运动的轴，范围 1-3</param>
	/// <param name="target">绝对目标位置，范围 0...4095，0 对应 0V，4095 对应 100V</param>
	/// <param name="scanSpeed">扫描速度，范围 0...4,095,000，表示每秒执行 target 的个数（以 0...4095 为单位）</param>
	void NatorsController::ScanMoveAbsolute(int axis, unsigned int target, unsigned int scanSpeed)
	{
		auto channel = MapAxisToChannel(axis);
		auto result = NT_ScanMoveAbsolute_S(_handle, channel, target, scanSpeed);
		CheckResult(result, "Fail to scan move absolute");
	}

	/// <summary>
	/// 扫描模式移动到相对目标位置
	/// </summary>
	/// <param name="axis">选择运动的轴，范围 1-3</param>
	/// <param name="diff">相对目标位置，范围 0...4095，0 对应 0V，4095 对应 100V</param>
	/// <param name="scanSpeed">扫描速度，范围 0...4,095,000，表示每秒执行 target 的个数（以 0...4095 为单位）</param>
	void NatorsController::ScanMoveRelative(int axis, signed int diff, unsigned int scanSpeed)
	{
		auto channel = MapAxisToChannel(axis);
		auto result = NT_ScanMoveRelative_S(_handle, channel, diff, scanSpeed);
		CheckResult(result, "Fail to scan move relative");
	}

	/// <summary>
	/// 将轴映射到 SDK 的 Channel 值
	/// </summary>
	/// <param name="axis">轴，范围 1-3</param>
	/// <returns>Channel</returns>
	NT_INDEX NatorsController::MapAxisToChannel(int axis)
	{
		NT_INDEX channel;

		switch (axis)
		{
		case 1:
			channel = NTU_AXIS_X;
			break;
		case 2:
			channel = NTU_AXIS_Y;
			break;
		case 3:
			channel = NTU_AXIS_Z;
			break;
		default:
			throw gcnew ArgumentOutOfRangeException("axis", "axis should be 1-3");
		}

		return channel;
	}

	/// <summary>
	/// 检查 SDK 返回的 NT_STATUS
	/// </summary>
	/// <param name="result">NATORS SDK 函数运行结果（NT_STATUS）</param>
	/// <param name="message">错误信息</param>
	void NatorsController::CheckResult(NT_STATUS result, String^ message)
	{
		if (result != NT_OK) {
			throw gcnew InvalidOperationException(
				String::Format(message + ", error code: {}", result));
		}
	}
}
