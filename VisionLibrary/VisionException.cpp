#include "pch.h"
#include "VisionException.h"

namespace VisionLibrary {

	VisionException::VisionException() {}

	VisionException::VisionException(String^ message) : Exception(message) {}

	VisionException::VisionException(String^ message, Exception^ innerException) : Exception(message, innerException) {}
}