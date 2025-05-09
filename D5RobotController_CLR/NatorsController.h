#pragma once

#include "NTControl.h"

namespace D5R {
	using namespace System;
	public ref class NatorsController
	{
	private:
		static const int NTU_AXIS_X = 1 - 1;
		static const int NTU_AXIS_Y = 2 - 1;
		static const int NTU_AXIS_Z = 3 - 1;

		NT_INDEX _handle;
		String^ _id;

	public:
		NatorsController(String^ id);
		~NatorsController();

		/// <summary>
		/// 查询 NanoMotor 的位置
		/// </summary>
		/// <param name="axis">轴，从基座到末端为 1-3</param>
		/// <returns>位置值，单位为纳米</returns>
		int GetPosition(int axis);

		/// <summary>
		/// 获取状态
		/// </summary>
		/// <param name="axis">选择的轴，从基座到末端为 1-3</param>
		/// <returns></returns>
		unsigned int GetStatus(int axis);

		/// <summary>
		/// 获取通道数
		/// </summary>
		/// <returns>通道数</returns>
		unsigned int GetChannelCount();

		/// <summary>
		/// 设置当前位置
		/// </summary>
		/// <param name="axis">选择设置的轴，范围 1-3</param>
		/// <param name="position">定义当前位置为该值</param>
		void SetPosition(int axis, signed int position);

		/// <summary>
		/// 绝对移动
		/// </summary>
		/// <param name="axis">移动的轴，从基座到末端为 1-3</param>
		/// <param name="position">移动的位置</param>
		void MoveAbsolute(int axis, int position);

		/// <summary>
		/// 相对移动
		/// </summary>
		/// <param name="axis">移动的轴，从基座到末端为 1-3</param>
		/// <param name="diff">相对移动的位置</param>
		void MoveRelative(int axis, int diff);

		/// <summary>
		/// 扫描模式移动到绝对位置
		/// </summary>
		/// <param name="axis">选择运动的轴，范围 1-3</param>
		/// <param name="target">绝对目标位置，范围 0...4095，0 对应 0V，4095 对应 100V</param>
		/// <param name="scanSpeed">扫描速度，范围 0...4,095,000，表示每秒执行 target 的个数（以 0...4095 为单位）</param>
		void ScanMoveAbsolute(int axis, unsigned int target, unsigned int scanSpeed);

		/// <summary>
		/// 扫描模式移动到相对目标位置
		/// </summary>
		/// <param name="axis">选择运动的轴，范围 1-3</param>
		/// <param name="diff">相对目标位置，范围 0...4095，0 对应 0V，4095 对应 100V</param>
		/// <param name="scanSpeed">扫描速度，范围 0...4,095,000，表示每秒执行 target 的个数（以 0...4095 为单位）</param>
		void ScanMoveRelative(int axis, signed int diff, unsigned int scanSpeed);

		/// <summary>
		/// 步进运动
		/// </summary>
		/// <param name="axis">运动的轴 1-3</param>
		/// <param name="steps">运动步数，范围 -30,000...30,000，值为 0 时停止，值为 ±30,000 时持续移动</param>
		/// <param name="amplitude">幅值，范围 100...4,095，0 对应 0V，4095 对应 100V</param>
		/// <param name="frequency">频率，单位为 Hz，有效范围 1...18,500</param>
		void StepMove(int axis, signed int steps, unsigned int amplitude, unsigned int frequency);

		/// <summary>
		/// 停止电机运动
		/// </summary>
		/// <param name="axis">选择电机轴，范围 1-3</param>
		void Stop(int axis);

	private:
		static NT_INDEX MapAxisToChannel(int axis);
		static void CheckResult(NT_STATUS result, String^ message);
	};
}
