#pragma once

#include "ErrorCode.h"
#include <exception>
#include <string>

namespace D5R {
	using namespace System;
	using namespace System::Runtime::InteropServices;

	public ref class RobotException : public System::Exception {
	private:
		char* _message;
	public:
		ErrorCode Code;
		RobotException() {};
		RobotException(ErrorCode code) { this->Code = code; }
		RobotException(const RobotException% other) : Code(other.Code) {}
		RobotException% operator=(const RobotException% other) {
			this->Code = other.Code;
			return *this;
		}

		~RobotException() {
			Marshal::FreeHGlobal((IntPtr)_message);
		}

		const char* what() {
			System::String^ str = Code.ToString();
			_message = (char*)Marshal::StringToHGlobalAnsi(str).ToPointer();
			return _message;
		}
	};

} // namespace D5R