using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using UnityEngine.UI;

public class LaneDetect_v2 : MonoBehaviour
{
    [SerializeField]
    private RawImage rawImage;

    Camera cam;

    Mat Process(Mat image)
    {
        Mat output = new();
        int height = image.Height;
        int width = image.Width;

        image.CopyTo(output);

        Point[] region_of_interest_vertices =
            {
            new Point(0, height),
            new Point(width * 0.35, height * 0.65),
            new Point(width * 0.65, height * 0.65),
            new Point(width, height)
        };

        Mat[] mats = Bird_eye_view(image, width, height, region_of_interest_vertices);
        Mat bv_crop = Color_filter(mats[0]);    //8UC1
        Mat histogram = Lane_histogram(bv_crop);

        Point leftbases;
        Point rightbases;
        Lane_peak(histogram, out leftbases, out rightbases);

        List<List<Point>> drawinfo = slide_window_search(bv_crop, leftbases, rightbases);

        drawinfo[0].Insert(0, new Point(leftbases.X, height));
        drawinfo[1].Insert(0, new Point(rightbases.X, height));

        List<Point> drawinfo_all = new(drawinfo[1]);
        drawinfo[0].Reverse();
        drawinfo_all.AddRange(drawinfo[0]);

        Cv2.FillConvexPoly(mats[0], drawinfo_all, new Scalar(0, 255, 0));

        Cv2.Polylines(mats[0], drawinfo, false, new Scalar(0, 255, 255), 2);

        Cv2.WarpPerspective(mats[0], output, mats[1], new Size(width, height));

        Cv2.BitwiseOr(image, output, output);

        return output;
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

        Cv2.InRange(hsv, new(0, 0, 160), new(255, 255, 255), white_mask);
        Cv2.InRange(hsv, new(20, 30, 100), new(40, 255, 255), yellow_mask);
        
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
        int nwindows = 12;
        int window_height = binary_warped.Height / nwindows;

        List<Point> nonzero = new();

        Cv2.FindNonZero(binary_warped, OutputArray.Create(nonzero));

        int margin = 20;

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

            foreach (Point pt in nonzero)
            {
                if ((pt.Y >= win_y_low) & (pt.Y < win_y_high) & (pt.X >= win_xleft_low) & (pt.X < win_xleft_high))
                {
                    Point good_left = new Point(pt.X, pt.Y);
                    left_lane.Add(good_left);
                    break;
                }
            }

            foreach (Point pt in nonzero)
            {
                if ((pt.Y >= win_y_low) & (pt.Y < win_y_high) & (pt.X >= win_xright_low) & (pt.X < win_xright_high))
                {
                    Point good_right = new Point(pt.X, pt.Y);
                    right_lane.Add(good_right);
                    break;
                }
            }
        }

        return new List<List<Point>> { left_lane, right_lane };
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
