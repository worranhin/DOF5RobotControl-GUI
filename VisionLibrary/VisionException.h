#pragma once

using namespace System;

namespace VisionLibrary {

	public ref class VisionException :
		public Exception
	{
	public:
		VisionException();
		VisionException(String^ message);
		VisionException(String^ message, Exception^ innerException);
	};
}