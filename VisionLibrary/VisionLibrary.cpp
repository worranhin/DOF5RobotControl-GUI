#include "pch.h"

#include "VisionLibrary.h"
#include "VisionException.h"
#include <string>
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>


namespace VisionLibrary {
	using namespace System;
	
	VisionWrapper::VisionWrapper() : VisionWrapper(gcnew System::String("./model/")) {
		// 无需额外实现，委托给有参构造函数
	}

	VisionWrapper::VisionWrapper(System::String^ modelBasePath) {
		try {
			auto path = ConvertToStdString(modelBasePath);
			instance = new NativeVision::VisualController(path);
		}
		catch (cv::Exception& cvEx) {
			throw gcnew VisionException(gcnew System::String(cvEx.what()));
		}
		catch (HalconCpp::HException& hEx) {
			throw gcnew VisionException(gcnew System::String(hEx.ErrorMessage().Text()) + " 请在可执行文件目录中提供 model");
		}
		catch (const std::exception& e) {
			throw gcnew VisionException(gcnew System::String(e.what()));
		}
		catch (...) {
			throw gcnew VisionException("Unknown exception occurred when initialize VisualController.");
		}
	}

	VisionWrapper::~VisionWrapper() {
		delete instance;
	}

	void VisionWrapper::JawLibSegmentation(IntPtr imgBuffer, int width, int height, int stride)
	{
		try {
			unsigned char* data = static_cast<unsigned char*>(imgBuffer.ToPointer());
			cv::Mat mat = cv::Mat(height, width, CV_8UC1, data, stride).clone();  // clone 表示生拷贝，将数据复制到 mat 里面，避免原数据被修改或删除影响后续的图像处理

			instance->JawLibSegmentation(mat, 2);
		}
		catch (cv::Exception& exc) {
			throw gcnew VisionException(gcnew System::String(exc.what()));
		}
		catch (HalconCpp::HTupleAccessException& ex) {
			throw gcnew VisionException(gcnew System::String(ex.ErrorMessage().Text()));
		}
		catch (InvalidOperationException^) {
			throw;
		}
	}


	/// <summary>
	/// 通过顶部相机获取的图像，得到任务空间中夹钳与钳口的误差
	/// </summary>
	/// <param name="imgBuffer">存储图像信息的 Buffer 指针，函数退出前请勿释放内存</param>
	/// <param name="width">图像宽度</param>
	/// <param name="height">图像高度</param>
	/// <param name="stride">图像单行的字节数</param>
	/// <param name="mode">FINE 表示插入后的定位点，ROUGH 表示插入前的定位点</param>
	/// <returns>误差结构体</returns>
	TaskSpaceError VisionWrapper::GetTaskSpaceError(IntPtr imgBuffer, int width, int height, int stride, MatchingMode mode) {
		try {
			unsigned char* data = static_cast<unsigned char*>(imgBuffer.ToPointer());
			cv::Mat mat = cv::Mat(height, width, CV_8UC1, data, stride).clone();  // clone 表示生拷贝，将数据复制到 mat 里面，避免原数据被修改或删除影响后续的图像处理, 耗时 4ms
			NativeVision::MatchingMode nativeMode = (mode == MatchingMode::FINE ? NativeVision::FINE : NativeVision::ROUGH);

			instance->JawLibSegmentation(mat, 2); // 耗时 219 ms
			auto error = instance->GetTaskSpaceError(mat, nativeMode); // 耗时 1808ms
			TaskSpaceError managedError{
				error.Px, error.Py, error.Pz, error.Ry, error.Rz
			};
			return managedError;
		}
		catch (const cv::Exception& exc) {
			throw gcnew VisionException(gcnew System::String(exc.what()));
		}
		catch (const HalconCpp::HTupleAccessException& ex) {
			throw gcnew VisionException(gcnew System::String(ex.ErrorMessage().Text()));
		}
		catch (const std::exception& ex) {
			throw gcnew VisionException(gcnew System::String(ex.what()));
		}
		catch (InvalidOperationException^) {
			throw;
		}
	}

	void VisionWrapper::GetHorizontalLine(IntPtr imgBuffer, int width, int height, int stride)
	{
		try {
			unsigned char* data = static_cast<unsigned char*>(imgBuffer.ToPointer());
			cv::Mat mat = cv::Mat(height, width, CV_8UC1, data, stride);

			instance->GetHorizontalLine(mat, 1);
		}
		catch (const cv::Exception& exc) {
			throw gcnew VisionException(gcnew System::String(exc.what()));
		}
		catch (const HalconCpp::HTupleAccessException& ex) {
			throw gcnew VisionException(gcnew System::String(ex.ErrorMessage().Text()));
		}
		catch (const std::exception& ex) {
			throw gcnew VisionException(gcnew System::String(ex.what()));
		}
		catch (InvalidOperationException^) {
			throw;
		}
	}

	/// <summary>
	/// 通过底部相机的图像，获取竖直高度上夹钳与钳口平面之间的距离
	/// </summary>
	/// <param name="imgBuffer">存储图像信息的 Buffer 指针，函数退出前请勿释放内存</param>
	/// <param name="width">图像的宽度</param>
	/// <param name="height">图像的高度</param>
	/// <param name="stride">图像单行的字节数</param>
	/// <returns>夹钳与钳口之间的距离，单位为 mm</returns>
	double VisionWrapper::GetVerticalError(IntPtr imgBuffer, int width, int height, int stride)
	{
		try {
			unsigned char* data = static_cast<unsigned char*>(imgBuffer.ToPointer());
			cv::Mat mat = cv::Mat(height, width, CV_8U, data, stride);

			instance->GetHorizontalLine(mat, 1);
			return instance->GetVerticalDistance(mat, 1);
		}
		catch (const cv::Exception& exc) {
			throw gcnew VisionException(gcnew System::String(exc.what()));
		}
		catch (const HalconCpp::HTupleAccessException& ex) {
			throw gcnew VisionException(gcnew System::String(ex.ErrorMessage().Text()));
		}
		catch (const std::exception& ex) {
			throw gcnew VisionException(gcnew System::String(ex.what()));
		}
		catch (InvalidOperationException^) {
			throw;
		}
	}

	/// <summary>
	/// 通过 Halcon 算法获取钳口的位姿
	/// </summary>
	/// <param name="imgBuffer">存储图像信息的 Buffer 指针，函数退出前请勿释放内存</param>
	/// <param name="width">图像的宽度</param>
	/// <param name="height">图像的高度</param>
	/// <param name="stride">图像单行的字节数</param>
	/// <returns>表示钳口位姿的元组 (x, y, rz)</returns>
	System::ValueTuple<double, double, double> VisionWrapper::GetJawPos(IntPtr imgBuffer, int width, int height, int stride)
	{
		try {
			// 数据转换
			unsigned char* data = static_cast<unsigned char*>(imgBuffer.ToPointer());
			cv::Mat mat = cv::Mat(height, width, CV_8U, data, stride);
			auto hImage = instance->Mat2HImage(mat);

			// 获取钳口位姿
			auto jawPos = instance->GetJawPos(hImage);
			return System::ValueTuple<double, double, double>(jawPos.x, jawPos.y, jawPos.angle);
		}
		catch (const HalconCpp::HException ex) {
			throw gcnew VisionException(gcnew System::String(ex.ErrorMessage().Text()));
		}
		catch (const std::exception& ex) {
			throw gcnew VisionException(gcnew System::String(ex.what()));
		}
	}

	ValueTuple<double, double> VisionWrapper::GetJawLibLine() 
	{
		try
		{
			double a, b;
			if (!instance->GetJawLibLine(a, b))
				throw gcnew VisionException("GetJawLibLine failed");

			return ValueTuple<double, double>(a, b);
		}
		catch (const std::exception& ex)
		{
			throw gcnew VisionException(gcnew System::String(ex.what()));
		}
	}

	/// <summary>
	/// convert the managed string to std::string
	/// </summary>
	/// <param name="managedStr">the managed string to be converted</param>
	/// <returns></returns>
	std::string VisionWrapper::ConvertToStdString(System::String^ managedStr)
	{
		using namespace System;
		using namespace System::Runtime::InteropServices;
		using namespace msclr::interop;

		std::string str = marshal_as<std::string>(managedStr);
		return str;

		//char* pChar = (char*)(Marshal::StringToHGlobalAnsi(managedStr)).ToPointer();  // Marshal managed string to unmanaged memory
		//std::string result(pChar);
		//Marshal::FreeHGlobal(IntPtr(pChar));  // free the unmanaged string.

		//return result;
	}
}

