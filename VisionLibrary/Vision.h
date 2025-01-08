#pragma once

namespace NativeVision {

	struct Clamp {
		cv::Mat img;
		cv::Point2f center;
		cv::Point2f point;
		std::vector<cv::KeyPoint> keypoints;
		cv::Mat descriptors;
	};

	struct Jaw {
		cv::Mat img;
		cv::Point2f center;
		HalconCpp::HTuple temp_dl, temp_dr;
	};

	struct BotModel {
		cv::Mat model;
		std::vector<cv::Point2f> pos;
	};

	enum Models {
		CLAMP = 0,
		JAW_MIN,
		JAW_MID,
		JAW_MAX
	};

	enum MatchingMode {
		FINE,
		ROUGH
	};

	struct JawPos {
		double x;
		double y;
		double angle;
		int flag;
	};

	struct TaskSpaceError {
		double Px;
		double Py;
		double Pz;
		double Ry;
		double Rz;
	};


	class VisualController
	{
	public:
		VisualController();
		~VisualController();
		HalconCpp::HObject Mat2HImage(cv::Mat img);
		cv::Mat HImage2Mat(HalconCpp::HObject img);
		void JawLibSegmentation(cv::Mat img, int index);
		std::vector<cv::Point2f> SIFT(cv::Mat img, Models m);
		void GetHorizontalLine(cv::Mat img, int index);
		double GetVerticalDistance(cv::Mat img, int index);
		JawPos GetJawPos(HalconCpp::HObject img);
		TaskSpaceError GetTaskSpaceError(cv::Mat img, MatchingMode m);



		// ±äÁ¿½Ó¿Ú
		Clamp GetClamp();
		Jaw GetJaw();
		cv::Point2f GetROIPos();
		cv::Point2f GetRoughPosPoint();
		double GetMapParam();


	private:
		// topC
		Clamp _clamp;
		Jaw _jawMid;
		cv::Mat _posTemplate_2;
		cv::Point2f _roiPos;
		cv::Point2f _roughPosPoint;
		// botC
		BotModel _clampBot;
		float _jawLibLine_a, _jawLibLine_b;
		float _ringLibLine_a, _ringLibLine_b;
		double _mapParam;
	};

} // namespace NativeVision