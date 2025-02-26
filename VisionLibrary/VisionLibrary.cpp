#include "pch.h"

#include "VisionLibrary.h"
#include "VisionException.h"

namespace VisionLibrary {
	VisionWrapper::VisionWrapper() {
		try {
			instance = new NativeVision::VisualController();
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
			cv::Mat mat = cv::Mat(height, width, CV_8UC1, data, stride).clone();  // clone 表示生拷贝，将数据复制到 mat 里面，避免原数据被修改或删除影响后续的图像处理
			NativeVision::MatchingMode nativeMode = (mode == MatchingMode::FINE ? NativeVision::FINE : NativeVision::ROUGH);

			instance->JawLibSegmentation(mat, 2);
			auto error = instance->GetTaskSpaceError(mat, nativeMode);
			TaskSpaceError managedError{
				error.Px, error.Py, error.Pz, error.Ry, error.Rz
			};
			return managedError;
		}
		catch (cv::Exception& exc) {
			//throw gcnew VisionException(gcnew System::String(exc.what()));
			throw;
		}
		catch (HalconCpp::HTupleAccessException& ex) {
			throw gcnew VisionException(gcnew System::String(ex.ErrorMessage().Text()));
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
		catch (cv::Exception& exc) {
			throw gcnew VisionException(gcnew System::String(exc.what()));
		}
		catch (HalconCpp::HTupleAccessException& ex) {
			throw gcnew VisionException(gcnew System::String(ex.ErrorMessage().Text()));
		}
		catch (InvalidOperationException^) {
			throw;
		}
		return 0.0;
	}
}

