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
		/// ��ѯ NanoMotor ��λ��
		/// </summary>
		/// <param name="axis">�ᣬ�ӻ�����ĩ��Ϊ 1-3</param>
		/// <returns>λ��ֵ����λΪ����</returns>
		int GetPosition(int axis);

		/// <summary>
		/// ��ȡ״̬
		/// </summary>
		/// <param name="axis">ѡ����ᣬ�ӻ�����ĩ��Ϊ 1-3</param>
		/// <returns></returns>
		unsigned int GetStatus(int axis);

		/// <summary>
		/// ��ȡͨ����
		/// </summary>
		/// <returns>ͨ����</returns>
		unsigned int GetChannelCount();

		/// <summary>
		/// ���õ�ǰλ��
		/// </summary>
		/// <param name="axis">ѡ�����õ��ᣬ��Χ 1-3</param>
		/// <param name="position">���嵱ǰλ��Ϊ��ֵ</param>
		void SetPosition(int axis, signed int position);

		/// <summary>
		/// �����ƶ�
		/// </summary>
		/// <param name="axis">�ƶ����ᣬ�ӻ�����ĩ��Ϊ 1-3</param>
		/// <param name="position">�ƶ���λ��</param>
		void MoveAbsolute(int axis, int position);

		/// <summary>
		/// ����ƶ�
		/// </summary>
		/// <param name="axis">�ƶ����ᣬ�ӻ�����ĩ��Ϊ 1-3</param>
		/// <param name="diff">����ƶ���λ��</param>
		void MoveRelative(int axis, int diff);

		/// <summary>
		/// ɨ��ģʽ�ƶ�������λ��
		/// </summary>
		/// <param name="axis">ѡ���˶����ᣬ��Χ 1-3</param>
		/// <param name="target">����Ŀ��λ�ã���Χ 0...4095��0 ��Ӧ 0V��4095 ��Ӧ 100V</param>
		/// <param name="scanSpeed">ɨ���ٶȣ���Χ 0...4,095,000����ʾÿ��ִ�� target �ĸ������� 0...4095 Ϊ��λ��</param>
		void ScanMoveAbsolute(int axis, unsigned int target, unsigned int scanSpeed);

		/// <summary>
		/// ɨ��ģʽ�ƶ������Ŀ��λ��
		/// </summary>
		/// <param name="axis">ѡ���˶����ᣬ��Χ 1-3</param>
		/// <param name="diff">���Ŀ��λ�ã���Χ 0...4095��0 ��Ӧ 0V��4095 ��Ӧ 100V</param>
		/// <param name="scanSpeed">ɨ���ٶȣ���Χ 0...4,095,000����ʾÿ��ִ�� target �ĸ������� 0...4095 Ϊ��λ��</param>
		void ScanMoveRelative(int axis, signed int diff, unsigned int scanSpeed);

		/// <summary>
		/// �����˶�
		/// </summary>
		/// <param name="axis">�˶����� 1-3</param>
		/// <param name="steps">�˶���������Χ -30,000...30,000��ֵΪ 0 ʱֹͣ��ֵΪ ��30,000 ʱ�����ƶ�</param>
		/// <param name="amplitude">��ֵ����Χ 100...4,095��0 ��Ӧ 0V��4095 ��Ӧ 100V</param>
		/// <param name="frequency">Ƶ�ʣ���λΪ Hz����Ч��Χ 1...18,500</param>
		void StepMove(int axis, signed int steps, unsigned int amplitude, unsigned int frequency);

		/// <summary>
		/// ֹͣ����˶�
		/// </summary>
		/// <param name="axis">ѡ�����ᣬ��Χ 1-3</param>
		void Stop(int axis);

	private:
		static NT_INDEX MapAxisToChannel(int axis);
		static void CheckResult(NT_STATUS result, String^ message);
	};
}
