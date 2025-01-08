#pragma once

#include "Vision.h"

using namespace System;

namespace VisionLibrary {
	public value struct TaskSpaceError
	{
		double Px;
		double Py;
		double Pz;
		double Ry;
		double Rz;
	};

	public enum class MatchingMode {
		FINE,
		ROUGH
	};

	public ref class VisionWrapper
	{
	private:
		NativeVision::VisualController* instance;

	public:
		VisionWrapper();
		~VisionWrapper();
		TaskSpaceError GetTaskSpaceError(IntPtr imgBuffer, int width, int height, MatchingMode mode);
	};
}
