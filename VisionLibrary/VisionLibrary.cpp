#include "pch.h"

#include "VisionLibrary.h"
#include "VisionException.h"
#include <string>
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>


namespace VisionLibrary {
	using namespace System;
	
	VisionWrapper::VisionWrapper() : VisionWrapper(gcnew System::String("./model/")) {
		// �������ʵ�֣�ί�и��вι��캯��
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
			throw gcnew VisionException(gcnew System::String(hEx.ErrorMessage().Text()) + " ���ڿ�ִ���ļ�Ŀ¼���ṩ model");
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
			cv::Mat mat = cv::Mat(height, width, CV_8UC1, data, stride).clone();  // clone ��ʾ�������������ݸ��Ƶ� mat ���棬����ԭ���ݱ��޸Ļ�ɾ��Ӱ�������ͼ����

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
	/// ͨ�����������ȡ��ͼ�񣬵õ�����ռ��м�ǯ��ǯ�ڵ����
	/// </summary>
	/// <param name="imgBuffer">�洢ͼ����Ϣ�� Buffer ָ�룬�����˳�ǰ�����ͷ��ڴ�</param>
	/// <param name="width">ͼ����</param>
	/// <param name="height">ͼ��߶�</param>
	/// <param name="stride">ͼ���е��ֽ���</param>
	/// <param name="mode">FINE ��ʾ�����Ķ�λ�㣬ROUGH ��ʾ����ǰ�Ķ�λ��</param>
	/// <returns>���ṹ��</returns>
	TaskSpaceError VisionWrapper::GetTaskSpaceError(IntPtr imgBuffer, int width, int height, int stride, MatchingMode mode) {
		try {
			unsigned char* data = static_cast<unsigned char*>(imgBuffer.ToPointer());
			cv::Mat mat = cv::Mat(height, width, CV_8UC1, data, stride).clone();  // clone ��ʾ�������������ݸ��Ƶ� mat ���棬����ԭ���ݱ��޸Ļ�ɾ��Ӱ�������ͼ����, ��ʱ 4ms
			NativeVision::MatchingMode nativeMode = (mode == MatchingMode::FINE ? NativeVision::FINE : NativeVision::ROUGH);

			instance->JawLibSegmentation(mat, 2); // ��ʱ 219 ms
			auto error = instance->GetTaskSpaceError(mat, nativeMode); // ��ʱ 1808ms
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
	/// ͨ���ײ������ͼ�񣬻�ȡ��ֱ�߶��ϼ�ǯ��ǯ��ƽ��֮��ľ���
	/// </summary>
	/// <param name="imgBuffer">�洢ͼ����Ϣ�� Buffer ָ�룬�����˳�ǰ�����ͷ��ڴ�</param>
	/// <param name="width">ͼ��Ŀ��</param>
	/// <param name="height">ͼ��ĸ߶�</param>
	/// <param name="stride">ͼ���е��ֽ���</param>
	/// <returns>��ǯ��ǯ��֮��ľ��룬��λΪ mm</returns>
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
	/// ͨ�� Halcon �㷨��ȡǯ�ڵ�λ��
	/// </summary>
	/// <param name="imgBuffer">�洢ͼ����Ϣ�� Buffer ָ�룬�����˳�ǰ�����ͷ��ڴ�</param>
	/// <param name="width">ͼ��Ŀ��</param>
	/// <param name="height">ͼ��ĸ߶�</param>
	/// <param name="stride">ͼ���е��ֽ���</param>
	/// <returns>��ʾǯ��λ�˵�Ԫ�� (x, y, rz)</returns>
	System::ValueTuple<double, double, double> VisionWrapper::GetJawPos(IntPtr imgBuffer, int width, int height, int stride)
	{
		try {
			// ����ת��
			unsigned char* data = static_cast<unsigned char*>(imgBuffer.ToPointer());
			cv::Mat mat = cv::Mat(height, width, CV_8U, data, stride);
			auto hImage = instance->Mat2HImage(mat);

			// ��ȡǯ��λ��
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

