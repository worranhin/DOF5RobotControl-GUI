#include "pch.h"

#include "VisionLibrary.h"

namespace VisionLibrary {
	VisionWrapper::VisionWrapper() {
		try {
			instance = new NativeVision::VisualController();
		}
		catch (const std::exception& e) {
			throw gcnew System::Exception(gcnew System::String(e.what()));
		}
		catch (cv::Exception& cvEx) {
			throw gcnew System::Exception(gcnew System::String(cvEx.what()));
		}
		catch (HalconCpp::HException& hEx) {
			throw gcnew System::Exception(gcnew System::String(hEx.ErrorMessage().Text()));
		}
		catch (...) {
			throw gcnew System::Exception("Unknown exception occurred when initialize VisualController.");
		}
	}

	VisionWrapper::~VisionWrapper() {
		delete instance;
	}

	TaskSpaceError VisionWrapper::GetTaskSpaceError(IntPtr imgBuffer, int width, int height, MatchingMode mode) {
		cv::Mat mat = cv::Mat(height, width, CV_8U, imgBuffer.ToPointer());
		NativeVision::MatchingMode nativeMode = mode == MatchingMode::FINE ? NativeVision::FINE : NativeVision::ROUGH;
		auto error = instance->GetTaskSpaceError(mat, nativeMode);
		TaskSpaceError managedError{
			error.Px, error.Py, error.Pz, error.Ry, error.Rz
		};

		return managedError;
	}
}

