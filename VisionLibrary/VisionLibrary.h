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
			array<Object^>^ args = gcnew array<Object^> { Px, Py, Pz, Ry, Rz };
			return String::Format("TaskSpaceError Px:{0:F4} Py:{1:F4} Pz:{2:F4} Ry:{3:F4} Rz:{4:F4}", args);
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
