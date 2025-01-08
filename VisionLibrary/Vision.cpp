#include "VC.h"

VisualController::VisualController() {
	// topC
	// 夹钳模板
	_clamp.img = cv::imread("./model/clampTemplate/clamp.png", 0);
	_clamp.center = cv::Point2f(324.0f, 119.0f);
	_clamp.point = cv::Point2f(328.2f, 212.9f);
	cv::FileStorage fs1("./model/clampTemplate/KeyPoints_Clamp.yml", cv::FileStorage::READ);
	fs1["keypoints"] >> _clamp.keypoints;
	fs1.release();
	cv::FileStorage fs2("./model/clampTemplate/Descriptors_Clamp.yml", cv::FileStorage::READ);
	fs2["descriptors"] >> _clamp.descriptors;
	fs2.release();

	// 中号钳口模板
	HalconCpp::ReadShapeModel("./model/jawTemplate/shm/Temp_DL.shm", &_jawMid.temp_dl);
	HalconCpp::ReadShapeModel("./model/jawTemplate/shm/Temp_DR.shm", &_jawMid.temp_dr);

	// 钳口库定位模板
	_posTemplate_2 = cv::imread("./model/posTemplate/PosTemple_2.png", 0);

	// 粗定位点，相对于_roiPos而言
	_roughPosPoint = cv::Point2f(450, 1050);


	// botC
	// 夹钳模板
	_clampBot.model = cv::imread("./model/botCTemplate/clamp_bot.png", 0);
	_clampBot.pos.push_back(cv::Point2f(93.9549, 95.2925));
	_clampBot.pos.push_back(cv::Point2f(513.976, 91.9765));

	// 钳口库平台水平线
	_jawLibLine_a = -0.00116294;
	_jawLibLine_b = 1718.94;
	// 图像与显示映射参数
	_mapParam = 0.00945084;
}
VisualController::~VisualController() {}

/**
 * @brief 将OpenCV Mat格式的图片转换成Halcon HObject格式
 * @param img
 * @return
 */
HalconCpp::HObject VisualController::Mat2HImage(cv::Mat img) {
	if (img.empty() || img.channels() != 1) {
		std::cout << "Error, invaid img" << std::endl;
		return {};
	}

	int width = img.cols, height = img.rows;
	uchar* temp = new uchar[height * width];
	memcpy(temp, img.data, height * width);
	HalconCpp::HObject ho_img;
	HalconCpp::GenImage1(&ho_img, "byte", width, height, (Hlong)(temp));
	delete[] temp;
	return ho_img;
}
/**
 * @brief 将Halcon HObject格式转换为OpenCV Mat格式
 * @param img
 * @return
 */
cv::Mat VisualController::HImage2Mat(HalconCpp::HObject img) {
	HalconCpp::HTuple channels;
	HalconCpp::CountChannels(img, &channels);
	if (channels.I() != 1) {
		std::cout << "Error, Invaid img type" << std::endl;
		return {};
	}

	HalconCpp::HTuple hv_Pointer, hv_type, width, height;
	HalconCpp::GetImagePointer1(img, &hv_Pointer, &hv_type, &width, &height);
	int w = width.I();
	int h = height.I();
	int size = w * h;
	cv::Mat cv_img = cv::Mat::zeros(h, w, CV_8UC1);
	memcpy(cv_img.data, (void*)(hv_Pointer.L()), size);
	return cv_img;

}
/**
 * @brief 分割钳口库，更新类内钳口库roi信息
 * @param img
 * @param index
 */
void VisualController::JawLibSegmentation(cv::Mat img, int index) {
	cv::Mat result;
	cv::Point minLoc, maxLoc;
	double minVal, maxVal;
	switch (index)
	{
	case 1:
	case 2:
		cv::matchTemplate(img, _posTemplate_2, result, cv::TM_CCOEFF_NORMED);
		cv::minMaxLoc(result, &minVal, &maxVal, &minLoc, &maxLoc);
		// 测试匹配效果
		//cv::rectangle(img, maxLoc, cv::Point(maxLoc.x + 280, maxLoc.y + 280), cv::Scalar(0), 4);
		//cv::namedWindow("test", cv::WINDOW_NORMAL);
		//cv::resizeWindow("test", cv::Size(1300, 1000));
		//cv::imshow("test", img);
		//cv::waitKey(0);
		_roiPos = cv::Point2f(maxLoc.x - 300.0f, maxLoc.y + 300.0f);
		break;
	case 3:
	default:
		break;
	}
}
/**
 * @brief 根据现有的模板进行SIFT匹配，返回模板中定位点在img中的位置信息
 * @param img
 * @param m
 * @return pst, 匹配失败则返回空
 */
std::vector<cv::Point2f> VisualController::SIFT(cv::Mat img, Models m) {
	cv::Mat model;
	std::vector<cv::Point2f> modelPosition;
	std::vector<cv::KeyPoint> keyPoints_Model;
	cv::Mat descriptors_model;
	switch (m)
	{
	case CLAMP:
		model = _clamp.img.clone();
		modelPosition.push_back(_clamp.center);
		modelPosition.push_back(_clamp.point);
		keyPoints_Model = _clamp.keypoints;
		descriptors_model = _clamp.descriptors;
		break;
	case JAW_MIN:
		break;
	case JAW_MID:
		break;
	case JAW_MAX:
		break;
	default:
		break;
	}
	// ROI
	cv::Rect roi = cv::Rect(static_cast<cv::Point>(_roiPos), cv::Size(850, 2046 - static_cast<int>(_roiPos.y)));
	cv::Mat ROI = img(roi).clone();

	// SIFT特征点
	cv::Ptr<cv::SIFT> sift = cv::SIFT::create();
	std::vector<cv::KeyPoint> keyPoints_Img;
	sift->detect(ROI, keyPoints_Img);
	// 描述
	cv::Mat descriptors_Img;
	sift->compute(ROI, keyPoints_Img, descriptors_Img);
	// 匹配
	cv::Ptr<cv::DescriptorMatcher> matcher =
		cv::DescriptorMatcher::create(cv::DescriptorMatcher::BRUTEFORCE);
	std::vector<std::vector<cv::DMatch>> knn_matches;
	const float ratio_thresh = 0.7f;
	std::vector<cv::DMatch> goodMatches;
	matcher->knnMatch(descriptors_model, descriptors_Img, knn_matches, 2);
	for (auto& knn_matche : knn_matches) {
		if (knn_matche[0].distance < ratio_thresh * knn_matche[1].distance) {
			goodMatches.push_back(knn_matche[0]);
		}
	}
	// 显示匹配结果
	//cv::Mat img_matches_res;
	//cv::drawMatches(model, keyPoints_Model, ROI, keyPoints_Img, goodMatches, img_matches_res, cv::Scalar::all(-1),
	//	cv::Scalar::all(-1), std::vector<char>(), cv::DrawMatchesFlags::NOT_DRAW_SINGLE_POINTS);
	//cv::imshow("test", img_matches_res);
	//cv::waitKey(0);
	if (goodMatches.size() < 8) {
		std::cerr << "Failed to SIFT" << std::endl;
		return {};
	}
	std::vector<cv::Point2f> model_P, img_P;
	for (const auto& match : goodMatches) {
		model_P.push_back(keyPoints_Model[match.queryIdx].pt);
		img_P.push_back(keyPoints_Img[match.trainIdx].pt);
	}
	cv::Mat homography = cv::findHomography(model_P, img_P, cv::RANSAC);
	std::vector<cv::Point2f> pst;
	std::vector<cv::Point2f> pst_Global;
	cv::perspectiveTransform(modelPosition, pst, homography);
	for (auto& p : pst) {
		p.x += _roiPos.x;
		p.y += _roiPos.y;
		pst_Global.push_back(p);
	}
	// 测试匹配效果
	//cv::line(img, pst_Global[0], pst_Global[1], cv::Scalar(0), 4);
	//cv::imshow("test", img);
	//cv::waitKey(0);
	return pst_Global;

}
/**
 * @brief 更新平台水平线
 * @param img
 * @param index 1：钳口库 2：物料台
 */
void VisualController::GetHorizontalLine(cv::Mat img, int index) {
	// 将钳口台下半部分遮住，防止干扰，具体使用根据钳口台与相机高度而定
	cv::Point2f roiPos(200, 1500);
	cv::Rect roi = cv::Rect(roiPos, cv::Size(2200, 548));
	cv::Mat ROI = img(roi).clone();

	// 图像处理
	cv::Mat gauss;
	cv::GaussianBlur(ROI, gauss, cv::Size(5, 5), 25);
	cv::Mat bin;
	cv::threshold(gauss, bin, 180, 255, cv::THRESH_BINARY);
	cv::Mat edge;
	cv::Canny(bin, edge, 50, 150);

	//// 测试效果
	//cv::imshow("test", ROI);
	//cv::waitKey(0);
	//cv::imshow("test", bin);
	//cv::waitKey(0);
	//cv::imshow("test", edge);
	//cv::waitKey(0);

	std::vector<cv::Vec4f> lines;
	cv::HoughLinesP(edge, lines, 1, CV_PI / 180, 200, 500, 300);
	std::cout << lines.size() << std::endl;
	// 最小二乘拟合
	int n = lines.size() * 2;
	float sum_x = 0, sum_y = 0, sum_xy = 0, sum_x2 = 0;
	for (auto& line : lines) {
		sum_x += (line[0] + line[2] + 2 * roiPos.x);
		sum_y += (line[1] + line[3] + 2 * roiPos.y);
		sum_xy += ((line[0] + roiPos.x) * (line[1] + roiPos.y) + (line[2] + roiPos.x) * (line[3] + roiPos.y));
		sum_x2 += ((line[0] + roiPos.x) * (line[0] + roiPos.x) + (line[2] + roiPos.x) * (line[2] + roiPos.x));

	}
	float mean_x = sum_x / n;
	float mean_y = sum_y / n;

	if (index == 1) {
		_jawLibLine_a = (sum_xy - n * mean_x * mean_y) / (sum_x2 - n * mean_x * mean_x);
		_jawLibLine_b = (mean_y - _jawLibLine_a * mean_x);

		//// 测试拟合效果
		//cv::line(img, cv::Point2f(50, 50 * _jawLibLine_a + _jawLibLine_b), 
		//	cv::Point2f(2000, 2000 * _jawLibLine_a + _jawLibLine_b), cv::Scalar(0), 4);
		//cv::namedWindow("a", cv::WINDOW_NORMAL);
		//cv::resizeWindow("a", cv::Size(1295, 1024));
		//cv::imshow("a", img);
		//cv::waitKey(0);
	}
	else {
		_ringLibLine_a = (sum_xy - n * mean_x * mean_y) / (sum_x2 - n * mean_x * mean_x);
		_ringLibLine_b = (mean_y - _ringLibLine_a * mean_x);
	}
}
/**
 * @brief 获取机器人z轴移动距离
 * @param img
 * @param index
 * @return
 */
double VisualController::GetVerticalDistance(cv::Mat img, int index) {
	cv::Mat res;
	cv::Point minLoc, maxLoc;
	double minVal, maxVal;
	float a, b;
	if (index == 1) {
		a = _jawLibLine_a;
		b = _jawLibLine_b;
	}
	else {
		a = _ringLibLine_a;
		b = _ringLibLine_b;
	}
	cv::matchTemplate(img, _clampBot.model, res, cv::TM_CCOEFF_NORMED);
	cv::minMaxLoc(res, &minVal, &maxVal, &minLoc, &maxLoc);
	cv::Point2f maxLoc_(maxLoc.x, maxLoc.y);
	double distance = 0;
	for (int i = 0; i < _clampBot.pos.size(); ++i) {
		distance += (abs(a * (_clampBot.pos[i].x + maxLoc_.x) -
			_clampBot.pos[i].y - maxLoc_.y + b) / sqrt(a * a + 1));
	}
	distance /= _clampBot.pos.size();
	// 测试效果
	//cv::rectangle(img, minLoc_, cv::Point2f(minLoc_.x + _clampBot.model.size().width, 
	//	minLoc_.y + _clampBot.model.size().height), cv::Scalar(255), 4);
	//cv::line(img, cv::Point2f(50, 50 * a + b), 
	//	cv::Point2f(2000, 2000 * a + b), cv::Scalar(0), 4);
	//cv::namedWindow("a", cv::WINDOW_NORMAL);
	//cv::resizeWindow("a", cv::Size(1295, 1024));
	//cv::imshow("a", img);
	//cv::waitKey(0);
	return -distance * _mapParam + 0.35;

}
/**
 * @brief 获取jaw中心定位信息
 * @param ho_img
 * @return {x, y, angle, flag}
 */
JawPos VisualController::GetJawPos(HalconCpp::HObject ho_img) {
	using namespace HalconCpp;
	HObject ho_search_ROI_DL, ho_search_ROI_DR, ho_ROI_DL, ho_ROI_DR, ho_init_search_rect, ho_ImageReduced;
	HTuple hv_start, hv_range, hv_Height_DT, hv_Width_DT, hv_Height_DS, hv_Width_DS;

	//参数设置
	hv_start = -0.131;
	hv_range = 0.262;

	//模板size
	hv_Height_DT = 200;
	hv_Width_DT = 50;

	//搜索size
	hv_Height_DS = 300;
	hv_Width_DS = 150;

	//设置历史变量
	static HTuple hv_Last_Row_DL = 1000.0, hv_Last_Col_DL = 1000.0, hv_Last_Angle_DL = 0.0;
	static HTuple hv_Last_Row_DR = 1000.0, hv_Last_Col_DR = 1000.0, hv_Last_Angle_DR = 0.0;
	static bool firstTime = true;

	HTuple hv_Row_DL, hv_Col_DL, hv_Angle_DL, hv_Score_DL;
	HTuple hv_Row_DR, hv_Col_DR, hv_Angle_DR, hv_Score_DR;

	int flag = 0;

	GenRectangle1(&ho_init_search_rect, _roiPos.y, _roiPos.x, 1500, _roiPos.x + 850);
	if (firstTime) {
		ReduceDomain(ho_img, ho_init_search_rect, &ho_ImageReduced);
		FindShapeModel(ho_ImageReduced, _jawMid.temp_dl, hv_start, hv_range, 0.7, 1, 0.5,
			(HTuple("least_squares").Append("max_deformation 2")), 0, 0.9, &hv_Row_DL,
			&hv_Col_DL, &hv_Angle_DL, &hv_Score_DL);
		ReduceDomain(ho_img, ho_init_search_rect, &ho_ImageReduced);
		FindShapeModel(ho_ImageReduced, _jawMid.temp_dr, hv_start, hv_range, 0.7, 1, 0.5,
			(HTuple("least_squares").Append("max_deformation 2")), 0, 0.9, &hv_Row_DR,
			&hv_Col_DR, &hv_Angle_DR, &hv_Score_DR);

		hv_Last_Row_DL = hv_Row_DL;
		hv_Last_Col_DL = hv_Col_DL;
		hv_Last_Angle_DL = hv_Angle_DL;
		hv_Last_Row_DR = hv_Row_DR;
		hv_Last_Col_DR = hv_Col_DR;
		hv_Last_Angle_DR = hv_Angle_DR;
		firstTime = false;
	}
	else {

		GenRectangle2(&ho_search_ROI_DL, hv_Last_Row_DL, hv_Last_Col_DL, hv_Last_Angle_DL,
			hv_Width_DS / 2, hv_Height_DS / 2);
		GenRectangle2(&ho_search_ROI_DR, hv_Last_Row_DR, hv_Last_Col_DR, hv_Last_Angle_DR,
			hv_Width_DS / 2, hv_Height_DS / 2);
		ReduceDomain(ho_img, ho_search_ROI_DL, &ho_ROI_DL);
		ReduceDomain(ho_img, ho_search_ROI_DR, &ho_ROI_DR);
		FindShapeModel(ho_ROI_DL, _jawMid.temp_dl, hv_start, hv_range, 0.7, 1, 0.5, (HTuple("least_squares").Append("max_deformation 2")),
			0, 0.9, &hv_Row_DL, &hv_Col_DL, &hv_Angle_DL, &hv_Score_DL);
		FindShapeModel(ho_ROI_DR, _jawMid.temp_dr, hv_start, hv_range, 0.7, 1, 0.5, (HTuple("least_squares").Append("max_deformation 2")),
			0, 0.9, &hv_Row_DR, &hv_Col_DR, &hv_Angle_DR, &hv_Score_DR);
		if (hv_Score_DL.Length() > 0 && hv_Score_DL.D() >= 0.7)
		{
			hv_Last_Row_DL = hv_Row_DL;
			hv_Last_Col_DL = hv_Col_DL;
			hv_Last_Angle_DL = hv_Angle_DL;
		}
		else
		{
			ReduceDomain(ho_img, ho_init_search_rect, &ho_ImageReduced);
			FindShapeModel(ho_ImageReduced, _jawMid.temp_dl, hv_start, hv_range, 0.7, 1, 0.5,
				(HTuple("least_squares").Append("max_deformation 2")), 0, 0.9, &hv_Row_DL,
				&hv_Col_DL, &hv_Angle_DL, &hv_Score_DL);
			if (hv_Score_DL.Length() > 0 && hv_Score_DL.D() >= 0.7)
			{
				hv_Last_Row_DL = hv_Row_DL;
				hv_Last_Col_DL = hv_Col_DL;
				hv_Last_Angle_DL = hv_Angle_DL;
			}
			else {
				flag++;
			}
		}
		if (hv_Score_DR.Length() > 0 && hv_Score_DR.D() >= 0.7)
		{
			hv_Last_Row_DR = hv_Row_DR;
			hv_Last_Col_DR = hv_Col_DR;
			hv_Last_Angle_DR = hv_Angle_DR;
		}
		else
		{
			ReduceDomain(ho_img, ho_init_search_rect, &ho_ImageReduced);
			FindShapeModel(ho_ImageReduced, _jawMid.temp_dr, hv_start, hv_range, 0.7, 1, 0.5,
				(HTuple("least_squares").Append("max_deformation 2")), 0, 0.9, &hv_Row_DR,
				&hv_Col_DR, &hv_Angle_DR, &hv_Score_DR);
			if (hv_Score_DR.Length() > 0 && hv_Score_DR.D() >= 0.7)
			{
				hv_Last_Row_DR = hv_Row_DR;
				hv_Last_Col_DR = hv_Col_DR;
				hv_Last_Angle_DR = hv_Angle_DR;
			}
			else {
				flag++;
			}
		}
	}
	HTuple hv_Angle = (hv_Last_Angle_DR + hv_Last_Angle_DL) / 2;
	HTuple hv_Row = (hv_Last_Row_DL + hv_Last_Row_DR) * 0.5 - 150;
	HTuple hv_Col = hv_Last_Col_DL * 0.5 + hv_Last_Col_DR * 0.5;

	return { hv_Col.D(), hv_Row.D(),hv_Angle.D(), flag };
}
/**
 * @brief
 * @param img
 * @param m
 * @return
 */
TaskSpaceError VisualController::GetTaskSpaceError(cv::Mat img, MatchingMode m) {
	std::vector<cv::Point2f> clampPos = SIFT(img, Models::CLAMP);
	float clampAngle = static_cast<float>(atan2f(clampPos[0].y - clampPos[1].y, clampPos[0].x - clampPos[1].x) * (-180) / CV_PI);
	HalconCpp::HObject ho_img = Mat2HImage(img);
	JawPos jawPos = GetJawPos(ho_img);
	TaskSpaceError res;
	switch (m)
	{
	case FINE:
		res = { (clampPos[0].y - jawPos.y) * _mapParam,
				(clampPos[0].x - jawPos.x) * _mapParam,
				 0, 0,
				(clampAngle - jawPos.angle * 180 / CV_PI - 90) };
		break;
	case ROUGH:
		res = { (clampPos[0].y - jawPos.y) * _mapParam,
				(clampPos[0].x - GetROIPos().x - GetRoughPosPoint().x) * _mapParam,
				 0, 0,
				(clampAngle - jawPos.angle * 180 / CV_PI - 90) };
		break;
	default:
		break;
	}
	// 测试
	cv::namedWindow("test", cv::WINDOW_NORMAL);
	cv::resizeWindow("test", cv::Size(1300, 1000));
	cv::Point2f h_1(jawPos.x, jawPos.y);
	int h_2_x = static_cast<int>(h_1.x - 100 * cos(jawPos.angle + CV_PI / 2));
	int h_2_y = static_cast<int>(h_1.y - 100 * sin(jawPos.angle + CV_PI / 2));
	cv::line(img, h_1, cv::Point(h_2_x, h_2_y), cv::Scalar(0), 4);
	cv::line(img, clampPos[0], clampPos[1], cv::Scalar(0), 4);
	cv::imshow("test", img);
	cv::waitKey(0);

	return res;

}

// 类内变量接口
Clamp VisualController::GetClamp() { return _clamp; }

Jaw VisualController::GetJaw() { return _jawMid; }

cv::Point2f VisualController::GetROIPos() { return _roiPos; }

cv::Point2f VisualController::GetRoughPosPoint() { return _roughPosPoint; }

double VisualController::GetMapParam() { return _mapParam; }