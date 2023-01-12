using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using UnityEngine.UI;
using System;
using System.Linq;

public class LaneDetect_v2 : MonoBehaviour
{
    [SerializeField]
    private RawImage rawImage;

    Camera cam;

    Mat Process(Mat image)
    {
        int height = image.Height;
        int width = image.Width;

        Point[] region_of_interest_vertices =
            {
            new Point(0, height),
            new Point(width * 0.39, height * 0.6),
            new Point(width * 0.61, height * 0.6),
            new Point(width, height)
        };

        Mat[] mats = Bird_eye_view(image, width, height, region_of_interest_vertices);
        Mat bv_crop = Color_filter(mats[0]);    //8UC1
        Mat histogram = Lane_histogram(bv_crop);

        Point leftbases;
        Point rightbases;
        Lane_peak(histogram, out leftbases, out rightbases);

        List<List<Point>> temp = slide_window_search(bv_crop, leftbases, rightbases);

        foreach (List<Point> temp_list in temp)
        {
            Cv2.Circle(mats[0], temp_list[0], 5, Scalar.Red);
            Cv2.Circle(mats[0], temp_list[1], 5, Scalar.Red);
            Debug.Log(temp_list[0].Length() + ", " + temp_list[1].Length());
        }

        return mats[0];
        //return bv_crop;
    }

    Mat[] Bird_eye_view(Mat img_frame, int width, int height, Point[] region_of_interest_vertices)
    {
        Mat b_v = new();

        Point2f left_bottom = region_of_interest_vertices[0];
        Point2f left_top = region_of_interest_vertices[1];
        Point2f right_top = region_of_interest_vertices[2];
        Point2f right_bottom = region_of_interest_vertices[3];

        List<Point2f> pts1 = new();
        List<Point2f> pts2 = new();

        pts1.Add(left_top);
        pts1.Add(left_bottom);
        pts1.Add(right_top);
        pts1.Add(right_bottom);

        pts2.Add(new Point2f(0, 0));
        pts2.Add(new Point2f(0, height));
        pts2.Add(new Point2f(width, 0));
        pts2.Add(new Point2f(width, height));

        // pts1의 좌표에 표시. perspective 변환 후 이동 점 확인
        Mat M = Cv2.GetPerspectiveTransform(pts1, pts2);
        Mat ret_m = Cv2.GetPerspectiveTransform(pts2, pts1);
        Cv2.WarpPerspective(img_frame, b_v, M, new Size(width, height));

        Mat[] mats = new Mat[2];

        mats[0] = b_v; // 버드뷰
        mats[1] = ret_m;

        return mats;
    }

    Mat Color_filter(Mat image)
    {
        Mat output = new();
        Mat hsv = new();

        Mat white_mask = new();
        Mat yellow_mask = new();
        Mat gray_image = new();

        Mat white_img = new();
        Mat yellow_img = new();
        Mat res = new();

        Cv2.CvtColor(image, hsv, ColorConversionCodes.BGR2HSV);

        Cv2.InRange(hsv, new(0, 0, 170), new(30, 255, 255), white_mask);
        Cv2.InRange(hsv, new(0, 130, 130), new(40, 255, 255), yellow_mask);
        
        Cv2.BitwiseAnd(image, image, white_img, white_mask);
        Cv2.BitwiseAnd(image, image, yellow_img, yellow_mask);
        Cv2.BitwiseOr(white_img, yellow_img, res);

        Cv2.CvtColor(res, gray_image, ColorConversionCodes.BGR2HSV);
        Cv2.Canny(gray_image, output, 100, 120);

        return output;
    }

    Mat Lane_histogram(Mat image)
    {
        Mat output = new();
        Mat cropped = new(image, new OpenCvSharp.Rect(0, image.Rows / 2, image.Cols, image.Rows / 2));
        Cv2.Reduce(cropped / 255, output, ReduceDimension.Row, ReduceTypes.Sum, 4);

        return output;
    }

    void Lane_peak(Mat image, out Point left_max_loc, out Point right_max_loc)
    {
        Point temp;

        double min, max;
        int midpoint = image.Cols / 2;

        Mat left_half = image.ColRange(0, midpoint);
        Mat right_half = image.ColRange(midpoint, image.Cols);

        Cv2.MinMaxLoc(left_half, out min, out max, out temp, out left_max_loc);
        Cv2.MinMaxLoc(right_half, out min, out max, out temp, out right_max_loc);
        right_max_loc += new Point(midpoint, 0);
    }

    List<List<Point>> slide_window_search(Mat binary_warped, Point left_current, Point right_current)
    {
        Mat output = new(binary_warped.Size(), MatType.CV_8UC1);

        int nwindows = 12;
        int window_height = binary_warped.Height / nwindows;

        List<Point> nonzero = new();

        Cv2.FindNonZero(binary_warped, OutputArray.Create(nonzero));

        List<int> nonzero_x = new();
        List<int> nonzero_y = new();

        foreach (Point a in nonzero)
        {
            nonzero_x.Add(a.X);  //선이 있는 부분 x의 인덱스 값
        }

        foreach (Point a in nonzero)
        {
            nonzero_y.Add(a.Y);  //선이 있는 부분 y의 인덱스 값
        }

        int margin = 10;
        int minpix = 2;
        int thickness = 2;

        Scalar color = new Scalar(0, 255, 0);

        List<Point> left_lane = new();
        List<Point> right_lane = new();

        for (int w = 0; w < nwindows; w++)
        {
            int win_y_low = binary_warped.Height - (w + 1) * window_height;  // window 윗부분
            int win_y_high = binary_warped.Height - w * window_height;  // window 아랫 부분
            int win_xleft_low = left_current.X - margin;  // 왼쪽 window 왼쪽 위
            int win_xleft_high = left_current.X + margin; // 왼쪽 window 오른쪽 아래
            int win_xright_low = right_current.X - margin;  // 오른쪽 window 왼쪽 위
            int win_xright_high = right_current.X + margin; // 오른쪽 window 오른쪽 아래

            Cv2.Rectangle(output, new Point(win_xleft_low, win_y_low), new Point(win_xleft_high, win_y_high), color, thickness);
            Cv2.Rectangle(output, new Point(win_xright_low, win_y_low), new Point(win_xright_high, win_y_high), color, thickness);

            for(int i = 0; i < nonzero_y.Count; i++)
            {
                if ((nonzero_y[i] >= win_y_low) & (nonzero_y[i] < win_y_high) & (nonzero_x[i] >= win_xleft_low) & (nonzero_x[i] < win_xleft_high))
                {
                    Point good_left = new Point(nonzero_x[i], nonzero_y[i]);
                    left_lane.Add(good_left);
                }
            }

            for (int i = 0; i < nonzero_y.Count; i++)
            {
                if ((nonzero_y[i] >= win_y_low) & (nonzero_y[i] < win_y_high) & (nonzero_x[i] >= win_xright_low) & (nonzero_x[i] < win_xright_high))
                {
                    Point good_right = new Point(nonzero_x[i], nonzero_y[i]);
                    right_lane.Add(good_right);
                }
            }
            

            /*if (good_left.Length() > minpix)
            {
                left_current = nonzero_x.Average(good_left.X);
            }
            if (good_right.Length() > minpix)
            {
                right_current = np.int32(np.mean(nonzero_x[good_right]);
            }*/
        }

        return new List<List<Point>> { left_lane, right_lane };

        /*left_lane = np.concatenate(left_lane);  // np.concatenate() -> array를 1차원으로 합침
        right_lane = np.concatenate(right_lane);

        leftx = nonzero_x[left_lane];
        lefty = nonzero_y[left_lane];
        rightx = nonzero_x[right_lane];
        righty = nonzero_y[right_lane];

        left_fit = np.polyfit(lefty, leftx, 2);
        right_fit = np.polyfit(righty, rightx, 2);

        ploty = np.linspace(0, binary_warped.shape[0] - 1, binary_warped.shape[0]);
        left_fitx = left_fit[0] * ploty * *2 + left_fit[1] * ploty + left_fit[2];
        right_fitx = right_fit[0] * ploty * *2 + right_fit[1] * ploty + right_fit[2];

        ltx = np.trunc(left_fitx);  // np.trunc() -> 소수점 부분을 버림
        rtx = np.trunc(right_fitx);

        out_img[nonzero_y[left_lane], nonzero_x[left_lane]] = [255, 0, 0];
        out_img[nonzero_y[right_lane], nonzero_x[right_lane]] = [0, 0, 255];

        ret = { 'left_fitx': ltx, 'right_fitx': rtx, 'ploty': ploty };

        return ret;*/
    }

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        Texture2D img_copy = cam.targetTexture.toTexture2D();
        Mat img_frame = OpenCvSharp.Unity.TextureToMat(img_copy);

        Mat img_result = Process(img_frame);

        rawImage.texture = OpenCvSharp.Unity.MatToTexture(img_result);

        Resources.UnloadUnusedAssets();
    }
}
