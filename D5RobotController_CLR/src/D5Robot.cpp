#include "pch.h"

#include "D5Robot.h"

namespace D5R {
	D5Robot::D5Robot(const char* serialPort, std::string natorID, uint8_t topRMDID,
		uint8_t botRMDID) {
		_port = new SerialPort(serialPort);
		natorMotor = new NatorMotor(natorID);
		topRMDMotor = new RMDMotor(std::ref(*_port), topRMDID);
		botRMDMotor = new RMDMotor(std::ref(*_port), botRMDID);

		_isInit = natorMotor->IsInit() && topRMDMotor->isInit() && botRMDMotor->isInit();
		if (!_isInit) {
			throw gcnew RobotException(ErrorCode::CreateInstanceError);
		}
	}
	D5Robot::D5Robot(String^ portName, String^ natorID, byte topRMDId, byte bottomRMDId)
	{
		using namespace System::Runtime::InteropServices;
		auto portName_c = (char*)Marshal::StringToHGlobalAnsi(portName).ToPointer();
		auto natorId_c = (char*)Marshal::StringToHGlobalAnsi(natorID).ToPointer();
		try {
			_port = new SerialPort(portName_c);
			natorMotor = new NatorMotor(natorId_c);
			topRMDMotor = new RMDMotor(std::ref(*_port), topRMDId);
			botRMDMotor = new RMDMotor(std::ref(*_port), bottomRMDId);

			_isInit = natorMotor->IsInit() && topRMDMotor->isInit() && botRMDMotor->isInit();
			if (!_isInit) {
	 			throw gcnew RobotException(ErrorCode::CreateInstanceError);
			}
		}
		finally {
			Marshal::FreeHGlobal((IntPtr)portName_c);
			Marshal::FreeHGlobal((IntPtr)natorId_c);
		}
	}
	D5Robot::~D5Robot() {
		delete natorMotor; natorMotor = nullptr;
		delete topRMDMotor;	topRMDMotor = nullptr;
		delete botRMDMotor;	botRMDMotor = nullptr;
		delete _port; _port = nullptr;
	}

	D5Robot::!D5Robot() {
		delete natorMotor; natorMotor = nullptr;
		delete topRMDMotor;	topRMDMotor = nullptr;
		delete botRMDMotor;	botRMDMotor = nullptr;
		delete _port; _port = nullptr;
	}

	bool D5Robot::IsInit() { return _isInit; }

	bool D5Robot::SetZero() {
		if (!natorMotor->SetZero()) {
			ERROR_("Failed to set nator motor zero");
			return false;
		}
		if (!topRMDMotor->SetZero()) {
			ERROR_("Failed to set TOP RMD motor zero");
			return false;
		}
		if (!botRMDMotor->SetZero()) {
			ERROR_("Failed to set BOT RMD motor zero");
			return false;
		}
		return true;
	}
	bool D5Robot::Stop() {
		if (!natorMotor->Stop()) {
			ERROR_("Failed to stop nator motor");
			return false;
		}
		if (!topRMDMotor->Stop()) {
			ERROR_("Failed to stop TOP RMD motor");
			return false;
		}
		if (!botRMDMotor->Stop()) {
			ERROR_("Failed to stop BOT RMD motor");
			return false;
		}
		return true;
	}

	void D5Robot::Test(TestStruct^ t) {
		using namespace System;
		throw gcnew NotImplementedException();
	}

	void D5Robot::JointsMoveAbsolute(Joints^ j) {
		NTU_Point p{ j->P2, j->P3, j->P4 };
		if (!natorMotor->GoToPoint_A(p)) {
			throw gcnew RobotException(ErrorCode::NatorMoveError);
		}
		if (!topRMDMotor->GoAngleAbsolute(j->R1)) {
			throw gcnew RobotException(ErrorCode::RMDMoveError);
		}
		if (!botRMDMotor->GoAngleAbsolute(j->R5)) {
			throw gcnew RobotException(ErrorCode::RMDMoveError);
		}
	}
	void D5Robot::JointsMoveRelative(Joints^ j) {
		NTU_Point p{ j->P2, j->P3, j->P4 };
		if (!natorMotor->GoToPoint_R(p)) {
			throw gcnew RobotException(ErrorCode::NatorMoveError);
		}
		if (!topRMDMotor->GoAngleRelative(j->R1)) {
			throw gcnew RobotException(ErrorCode::RMDMoveError);
		}
		if (!botRMDMotor->GoAngleRelative(j->R5)) {
			throw gcnew RobotException(ErrorCode::RMDMoveError);
		}
	}

	Joints^ D5Robot::GetCurrentJoint() {
		Joints^ j = gcnew Joints(0, 0, 0, 0, 0);

		j->R1 = topRMDMotor->GetSingleAngle_s();
		j->R5 = botRMDMotor->GetSingleAngle_s();

		NTU_Point np;
		this->natorMotor->GetPosition(&np);
		j->P2 = np.x;
		j->P3 = np.y;
		j->P4 = np.z;

		return j;
	}
	Pose D5Robot::GetCurrentPose() {
		throw std::logic_error("Not implemented");
		//  return Pose();
	}

} // namespace D5R