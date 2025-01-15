#include "pch.h"

#include "VisionLibrary.h"

namespace VisionLibrary {
	VisionWrapper::VisionWrapper() {
		try {
			instance = new NativeVision::VisualController();
		}
		catch (cv::Exception& cvEx) {
			throw gcnew System::Exception(gcnew System::String(cvEx.what()));
		}
		catch (HalconCpp::HException& hEx) {
			throw gcnew System::Exception(gcnew System::String(hEx.ErrorMessage().Text()) + " ���ڿ�ִ���ļ�Ŀ¼���ṩ model");
		}
		catch (const std::exception& e) {
			throw gcnew System::Exception(gcnew System::String(e.what()));
		}
		catch (...) {
			throw gcnew System::Exception("Unknown exception occurred when initialize VisualController.");
		}
	}

	VisionWrapper::~VisionWrapper() {
		delete instance;
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
			cv::Mat mat = cv::Mat(height, width, CV_8U, data, stride);
			NativeVision::MatchingMode nativeMode = (mode == MatchingMode::FINE ? NativeVision::FINE : NativeVision::ROUGH);

			instance->JawLibSegmentation(mat, 2);
			auto error = instance->GetTaskSpaceError(mat, nativeMode);
			TaskSpaceError managedError{
				error.Px, error.Py, error.Pz, error.Ry, error.Rz
			};
			return managedError;
		}
		catch (cv::Exception& exc) {
			throw gcnew Exception(gcnew System::String(exc.what()));
		}
		catch (HalconCpp::HTupleAccessException& ex) {
			throw gcnew Exception(gcnew System::String(ex.ErrorMessage().Text()));
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
		catch (cv::Exception& exc) {
			throw gcnew Exception(gcnew System::String(exc.what()));
		}
		catch (HalconCpp::HTupleAccessException& ex) {
			throw gcnew Exception(gcnew System::String(ex.ErrorMessage().Text()));
		}
		return 0.0;
	}
}

