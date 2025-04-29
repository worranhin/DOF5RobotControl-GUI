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
		VisionWrapper(System::String^ modelBasePath);
		~VisionWrapper();
		void JawLibSegmentation(IntPtr imgBuffer, int width, int height, int stride);
		TaskSpaceError GetTaskSpaceError(IntPtr imgBuffer, int width, int height, int stride, MatchingMode mode);
		void GetHorizontalLine(IntPtr imgBuffer, int width, int height, int stride);
		double GetVerticalError(IntPtr imgBuffer, int width, int height, int stride);

		/// <summary>
		/// 通过 Halcon 算法获取钳口的位姿
		/// </summary>
		/// <param name="imgBuffer">存储图像信息的 Buffer 指针，函数退出前请勿释放内存</param>
		/// <param name="width">图像的宽度</param>
		/// <param name="height">图像的高度</param>
		/// <param name="stride">图像单行的字节数</param>
		/// <returns>表示钳口位姿的元组 (x, y, rz)</returns>
		System::ValueTuple<double, double, double> GetJawPos(IntPtr imgBuffer, int width, int height, int stride);
		
	private:
		std::string ConvertToStdString(System::String^ managedStr);
	};


}
