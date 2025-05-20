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
		res = NT_OpenSystem(pHandle, id_cstr, "sync");  // ����ϵͳ
		CheckResult(res, "Failed to init device");

		res = NT_SetHCMEnabled(_handle, NT_HCM_ENABLED);  // ʹ���ֶ�����ģ��
		CheckResult(res, "Failed to set HCM enabled");

		res = NT_SetSensorEnabled_S(_handle, NT_SENSOR_ENABLED);  // ʹ�ܴ�����
		CheckResult(res, "Failed to set sensor enabled");

		// ��������˶����ۼ� Set Accumulate relative positions
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
	/// ��ѯ NanoMotor ��λ��
	/// </summary>
	/// <param name="axis">�ᣬ�ӻ�����ĩ��Ϊ 1-3</param>
	/// <returns>λ��ֵ����λΪ����</returns>
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
	/// �����˶�
	/// </summary>
	/// <param name="axis">�˶����� 1-3</param>
	/// <param name="steps">�˶�����</param>
	/// <param name="amplitude">��ֵ</param>
	/// <param name="frequency">Ƶ��</param>
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
	/// ֹͣ����˶�
	/// </summary>
	/// <param name="axis">ѡ�����ᣬ��Χ 1-3</param>
	void NatorsController::Stop(int axis)
	{
		auto channel = MapAxisToChannel(axis);
		auto result = NT_Stop_S(_handle, channel);
		CheckResult(result, "Fail to stop motor");
	}

	/// <summary>
	/// ��ȡ�˶�״̬
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
		auto result = NT_GetNumberOfChannels(_handle, &channels);  // ��ȡͨ����
		CheckResult(result, "Failed to get number of channels");
		return channels;
	}

	/// <summary>
	/// ���õ�ǰλ��
	/// </summary>
	/// <param name="axis">ѡ�����õ��ᣬ��Χ 1-3</param>
	/// <param name="position">���嵱ǰλ��Ϊ��ֵ</param>
	void NatorsController::SetPosition(int axis, signed int position)
	{
		auto channel = MapAxisToChannel(axis);
		auto result = NT_SetPosition_S(_handle, channel, position);
		CheckResult(result, "Fail to set position");
	}

	/// <summary>
	/// �ƶ������ָ��λ��
	/// </summary>
	/// <param name="axis">�ƶ��ĵ������Χ 1-3</param>
	/// <param name="position">�ƶ��ľ���λ�ã���λ����</param>
	void NatorsController::MoveAbsolute(int axis, int position)
	{
		auto channel = MapAxisToChannel(axis);
		auto result = NT_GotoPositionAbsolute_S(_handle, channel, position, 0);  // �ĵ�˵�����һ���������ɵ�
		CheckResult(result, "Failed to move to absolute position");
	}

	/// <summary>
	/// ����ƶ�
	/// </summary>
	/// <param name="axis">Ҫ�ƶ��ĵ���ᣬ��Χ 1-3</param>
	/// <param name="diff">����˶��ľ��룬��λ����</param>
	void NatorsController::MoveRelative(int axis, int diff)
	{
		auto channel = MapAxisToChannel(axis);
		auto result = NT_GotoPositionRelative_S(_handle, channel, diff, 0);  // �ĵ�˵�����һ���������ɵ�
		CheckResult(result, "Failed to move relatively");
	}

	/// <summary>
	/// ɨ��ģʽ�ƶ�������λ��
	/// </summary>
	/// <param name="axis">ѡ���˶����ᣬ��Χ 1-3</param>
	/// <param name="target">����Ŀ��λ�ã���Χ 0...4095��0 ��Ӧ 0V��4095 ��Ӧ 100V</param>
	/// <param name="scanSpeed">ɨ���ٶȣ���Χ 0...4,095,000����ʾÿ��ִ�� target �ĸ������� 0...4095 Ϊ��λ��</param>
	void NatorsController::ScanMoveAbsolute(int axis, unsigned int target, unsigned int scanSpeed)
	{
		auto channel = MapAxisToChannel(axis);
		auto result = NT_ScanMoveAbsolute_S(_handle, channel, target, scanSpeed);
		CheckResult(result, "Fail to scan move absolute");
	}

	/// <summary>
	/// ɨ��ģʽ�ƶ������Ŀ��λ��
	/// </summary>
	/// <param name="axis">ѡ���˶����ᣬ��Χ 1-3</param>
	/// <param name="diff">���Ŀ��λ�ã���Χ 0...4095��0 ��Ӧ 0V��4095 ��Ӧ 100V</param>
	/// <param name="scanSpeed">ɨ���ٶȣ���Χ 0...4,095,000����ʾÿ��ִ�� target �ĸ������� 0...4095 Ϊ��λ��</param>
	void NatorsController::ScanMoveRelative(int axis, signed int diff, unsigned int scanSpeed)
	{
		auto channel = MapAxisToChannel(axis);
		auto result = NT_ScanMoveRelative_S(_handle, channel, diff, scanSpeed);
		CheckResult(result, "Fail to scan move relative");
	}

	/// <summary>
	/// ����ӳ�䵽 SDK �� Channel ֵ
	/// </summary>
	/// <param name="axis">�ᣬ��Χ 1-3</param>
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
	/// ��� SDK ���ص� NT_STATUS
	/// </summary>
	/// <param name="result">NATORS SDK �������н����NT_STATUS��</param>
	/// <param name="message">������Ϣ</param>
	void NatorsController::CheckResult(NT_STATUS result, String^ message)
	{
		if (result != NT_OK) {
			throw gcnew InvalidOperationException(
				String::Format(message + ", error code: {}", result));
		}
	}
}
