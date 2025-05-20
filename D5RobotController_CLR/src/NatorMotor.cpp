/**
 * @file NatorMotor.cpp
 * @author drawal (2581478521@qq.com)
 * @brief
 * @version 0.1
 * @date 2024-11-05
 *
 * @copyright Copyright (c) 2024
 *
 */
#include "pch.h"
#include "NatorMotor.h"
#include "RobotException.hpp"
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>


namespace D5R {
	// 构造析构---------------------------------------
	NatorMotor::NatorMotor(String^ id) : _id(id) {
		_isInit = Init();
		if (!_isInit)
		{
			std::cerr << "Failed to init NatorMotor" << std::endl;
			throw gcnew RobotException(ErrorCode::NatorInitError);
		}
	}
	NatorMotor::~NatorMotor() { NT_CloseSystem(_handle); }

	// 初始化------------------------------------------
	bool NatorMotor::Init() {
		using namespace msclr::interop;

		auto res = NT_OK;

		if (String::IsNullOrEmpty(_id))
			throw gcnew InvalidOperationException("Id should not be null or empty");

		// Convert System::String^ to const char*
		marshal_context^ context = gcnew marshal_context();
		const char* id_cstr = context->marshal_as<const char*>(_id);

		pin_ptr<NT_INDEX> pHandle = &_handle;
		res = NT_OpenSystem(pHandle, id_cstr, "sync");
		if (res != NT_OK) {
			std::cerr << "Failed to init device, error status: " << res << std::endl;
			return false;
		}
		res = NT_SetHCMEnabled(_handle, NT_HCM_ENABLED);
		if (res != NT_OK) {
			std::cerr << "Failed to set HCM enabled, error status: " << res
				<< std::endl;
			return false;
		}
		res = NT_SetSensorEnabled_S(_handle, NT_SENSOR_ENABLED);
		if (res != NT_OK) {
			std::cerr << "Failed to set sensor enabled, error status: " << res
				<< std::endl;
			return false;
		}
		res = NT_SetAccumulateRelativePositions_S(_handle, NTU_AXIS_X, NT_NO_ACCUMULATE_RELATIVE_POSITIONS);
		if (res != NT_OK) {
			std::cerr << "Failed to set relative move accumulation, error status: " << res
				<< std::endl;
			return false;
		}
		res = NT_SetAccumulateRelativePositions_S(_handle, NTU_AXIS_Y, NT_NO_ACCUMULATE_RELATIVE_POSITIONS);
		if (res != NT_OK) {
			std::cerr << "Failed to set relative move accumulation, error status: " << res
				<< std::endl;
			return false;
		}
		res = NT_SetAccumulateRelativePositions_S(_handle, NTU_AXIS_Z, NT_NO_ACCUMULATE_RELATIVE_POSITIONS);
		if (res != NT_OK) {
			std::cerr << "Failed to set relative move accumulation, error status: " << res
				<< std::endl;
			return false;
		}
		_isInit = true;
		return true;
	}

	// 判断初始化成功------------------------------------
	bool NatorMotor::IsInit() { return _isInit; }

	// 设置零点-----------------------------------------
	bool NatorMotor::SetZero() {
		auto res = NT_OK;
		res = NT_SetPosition_S(_handle, NTU_AXIS_X, 0);
		if (res != NT_OK) {
			std::cerr << "Failed to set axis_x zero, error status: " << res
				<< std::endl;
			return false;
		}
		res = NT_SetPosition_S(_handle, NTU_AXIS_Y, 0);
		if (res != NT_OK) {
			std::cerr << "Failed to set axis_y zero, error status: " << res
				<< std::endl;
			return false;
		}
		res = NT_SetPosition_S(_handle, NTU_AXIS_Z, 0);
		if (res != NT_OK) {
			std::cerr << "Failed to set axis_z zero, error status: " << res
				<< std::endl;
			return false;
		}
		return true;
	}

	// 获取当前位置
	bool NatorMotor::GetAllPosition(NTU_Point* p) {
		auto res = NT_OK;
		res = NT_GetPosition_S(_handle, NTU_AXIS_X, &(p->x));
		if (res != NT_OK) {
			std::cerr << "Failed to get axis_x positon, error status: " << res
				<< std::endl;
			return false;
		}
		res = NT_GetPosition_S(_handle, NTU_AXIS_Y, &(p->y));
		if (res != NT_OK) {
			std::cerr << "Failed to get axis_y positon, error status: " << res
				<< std::endl;
			return false;
		}
		res = NT_GetPosition_S(_handle, NTU_AXIS_Z, &(p->z));
		if (res != NT_OK) {
			std::cerr << "Failed to get axis_z positon, error status: " << res
				<< std::endl;
			return false;
		}
		return true;
	}

	/// <summary>
	/// 查询 NanoMotor 的位置
	/// </summary>
	/// <param name="axis">轴，从基座到末端为 1-3</param>
	/// <returns>位置值，单位为纳米</returns>
	int NatorMotor::GetPosition(int axis)
	{
		auto channel = MapAxisToChannel(axis);
		int position = 0;
		auto result = NT_GetPosition_S(_handle, channel, &position);

		if (result != NT_OK)
			throw gcnew InvalidOperationException(
				String::Format("Failed to get axis position, error code: {}", result));

		return position;
	}

	// 绝对移动------------------------------------------
	bool NatorMotor::GoToPoint_A(NTU_Point p) {
		auto res = NT_OK;
		res = NT_GotoPositionAbsolute_S(_handle, NTU_AXIS_X, p.x, 0);
		if (res != NT_OK) {
			std::cerr << "Failed to move axis_x, error status: " << res << std::endl;
			return false;
		}
		res = NT_GotoPositionAbsolute_S(_handle, NTU_AXIS_Y, p.y, 0);
		if (res != NT_OK) {
			std::cerr << "Failed to move axis_y, error status: " << res << std::endl;
			return false;
		}
		res = NT_GotoPositionAbsolute_S(_handle, NTU_AXIS_Z, p.z, 0);
		if (res != NT_OK) {
			std::cerr << "Failed to move axis_z, error status: " << res << std::endl;
			return false;
		}
		//   WaitUtilPositioned();
		return true;
	}

	// 阻塞-------------------------------------------------
	void NatorMotor::WaitUtilPositioned() {
		unsigned int res = 0;
		NT_GetStatus_S(_handle, NTU_AXIS_X, &res);
		while (res == NT_TARGET_STATUS) {
			Sleep(100);
			NT_GetStatus_S(_handle, NTU_AXIS_X, &res);
		}
		NT_GetStatus_S(_handle, NTU_AXIS_Y, &res);
		while (res == NT_TARGET_STATUS) {
			Sleep(100);
			NT_GetStatus_S(_handle, NTU_AXIS_Y, &res);
		}
		NT_GetStatus_S(_handle, NTU_AXIS_Z, &res);
		while (res == NT_TARGET_STATUS) {
			Sleep(100);
			NT_GetStatus_S(_handle, NTU_AXIS_Z, &res);
		}
	}

	// 相对移动---------------------------------------------
	bool NatorMotor::GoToPoint_R(NTU_Point p) {
		NT_STATUS res = NT_OK;
		if (p.x != 0) {
			res = NT_GotoPositionRelative_S(_handle, NTU_AXIS_X, p.x, 0);
			if (res != NT_OK) {
				std::cerr << "Failed to move axis_x, error status: " << res << std::endl;
				return false;
			}
		}

		if (p.y != 0) {
			res = NT_GotoPositionRelative_S(_handle, NTU_AXIS_Y, p.y, 0);
			if (res != NT_OK) {
				std::cerr << "Failed to move axis_y, error status: " << res << std::endl;
				return false;
			}
		}

		if (p.z != 0) {
			res = NT_GotoPositionRelative_S(_handle, NTU_AXIS_Z, p.z, 0);
			if (res != NT_OK) {
				std::cerr << "Failed to move axis_z, error status: " << res << std::endl;
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// 步进运动
	/// </summary>
	/// <param name="axis">运动的轴 1-3</param>
	/// <param name="steps">运动步数</param>
	/// <param name="amplitude">幅值</param>
	/// <param name="frequency">频率</param>
	void NatorMotor::StepMove(unsigned int axis, signed int steps, unsigned int amplitude, unsigned int frequency)
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
	/// 获取运动状态
	/// </summary>
	/// <param name="axis"></param>
	/// <returns></returns>
	unsigned int NatorMotor::GetStatus(int axis)
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

	// 急停------------------------------------------------
	bool NatorMotor::Stop() {
		auto res = NT_OK;
		res = NT_Stop_S(_handle, NTU_AXIS_X);
		if (res != NT_OK) {
			std::cerr << "Failed to stop axis_x, error status: " << res << std::endl;
			return false;
		}
		res = NT_Stop_S(_handle, NTU_AXIS_Y);
		if (res != NT_OK) {
			std::cerr << "Failed to stop axis_y, error status: " << res << std::endl;
			return false;
		}
		res = NT_Stop_S(_handle, NTU_AXIS_Z);
		if (res != NT_OK) {
			std::cerr << "Failed to stop axis_z, error status: " << res << std::endl;
			return false;
		}
		return true;
	}

	NT_INDEX NatorMotor::MapAxisToChannel(int axis) {
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
} // namespace D5R