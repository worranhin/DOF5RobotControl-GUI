#pragma once

#include "LogUtil.h"
#include "NatorMotor.h"
#include "RMDMotor.h"
#include "RobotException.hpp"
#include "SerialPort.h"

using namespace System;

namespace D5R {

public ref struct Joints {
  int R1;
  int P2;
  int P3;
  int P4;
  int R5;

  Joints() {};
  Joints(int r1, int p2, int p3, int p4, int r5) : R1(r1), P2(p2), P3(p3), P4(p4), R5(r5) {};
};

public ref struct TestStruct {
    int X;
};

struct Pose {
  double px;
  double py;
  double pz;
  double ry;
  double rz;
};

public ref class D5Robot {
private:
  SerialPort* _port;
  bool _isInit;

public:
  NatorMotor* natorMotor;
  RMDMotor* topRMDMotor;
  RMDMotor* botRMDMotor;

  D5Robot(const char *serialPort, std::string natorID, uint8_t topRMDID,
          uint8_t botRMDID);
  D5Robot(String^ portName, String^ natorID, byte topRMDId, byte bottomRMDId);
  ~D5Robot();
  bool IsInit();
  bool SetZero();
  bool Stop();
  void JointsMoveAbsolute(Joints^ j);
  void JointsMoveRelative(Joints^ j);
  void Test(TestStruct^ t);
  Joints^ GetCurrentJoint();
  Pose GetCurrentPose();
};
} // namespace D5R
