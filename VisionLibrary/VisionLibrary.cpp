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
			throw gcnew System::Exception(gcnew System::String(hEx.ErrorMessage().Text()));
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

