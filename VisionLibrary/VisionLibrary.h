#pragma once

#include "pch.h"
#include "Vision.h"
#include <format>

using namespace System;

namespace VisionLibrary {
	public value struct TaskSpaceError
	{
		double Px;
		double Py;
		double Pz;
		double Ry;
		double Rz;

		String^ ToString() override {
			//cli::array<double>^ args = { Px, Py, Pz, Ry, Rz };
			//IFormatProvider^ provider = System::Globalization::CultureInfo::InvariantCulture;
			return String::Format("TaskSpaceError Px:{0} Py:{1} Pz:{2} Ry:{3} Rz:{4}", Px, Py, Pz, Ry, Rz);
		}
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
		TaskSpaceError GetTaskSpaceError(IntPtr imgBuffer, int width, int height, int stride, MatchingMode mode);
		double GetVerticalError(IntPtr imgBuffer, int width, int height, int stride);
	};
}
