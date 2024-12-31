#pragma once

namespace D5R {

public enum class ErrorCode {
  OK = 0,
  NotImplementException,
  SystemError = 100,
  CreateInstanceError = 101,
  DestroyInstanceError_nullptr,
  SerialError = 200,
  SerialInitError = 201,
  SerialCloseError = 202,
  SerialSendError = 203,
  SerialReceiveError,
  SerialReceiveError_LessThanExpected,
  NatorError = 300,
  NatorInitError = 301,
  NatorMoveError,
  RMDError = 400,
  RMDInitError = 401,
  RMDGetPIError = 402,
  RMDFormatError,
  RMDChecksumError,
  RMDMoveError
};
}