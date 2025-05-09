/**
 * @file NatorMotor.h
 * @author drawal (2581478521@qq.com)
 * @brief
 * @version 0.1
 * @date 2024-11-05
 *
 * @copyright Copyright (c) 2024
 *
 */
#pragma once
#include "NTControl.h"
#include <cstdlib>
#include <iostream>
#include <string>
#include <windows.h>

namespace D5R {
	using namespace System;

	struct NTU_Point {
		int x;  // 关节 2 单位: nm
		int y;  // 关节 3 单位: nm
		int z;  // 关节 4 单位: nm
	};

//#define NTU_AXIS_X 1 - 1
//#define NTU_AXIS_Y 2 - 1
//#define NTU_AXIS_Z 3 - 1

	public ref class NatorMotor {
	public:
		NatorMotor(String^ id);
		~NatorMotor();
		bool Init();
		bool SetZero();
		bool IsInit();
		bool GetAllPosition(NTU_Point* p);
		int GetPosition(int axis);
		bool GoToPoint_A(NTU_Point p);
		void WaitUtilPositioned();
		bool GoToPoint_R(NTU_Point p);
		void StepMove(unsigned int axis, signed int steps, unsigned int amplitude, unsigned int frequency);
		unsigned int GetStatus(int axis);
		bool Stop();

	private:
		static NT_INDEX MapAxisToChannel(int axis);

	private:
		static const int NTU_AXIS_X = 1 - 1;
		static const int NTU_AXIS_Y = 2 - 1;
		static const int NTU_AXIS_Z = 3 - 1;

		NT_INDEX _handle;
		String^ _id;
		bool _isInit;
		unsigned int _status;
	};
} // namespace D5R