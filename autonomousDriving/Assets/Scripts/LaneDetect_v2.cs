using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using UnityEngine.UI;
using NumSharp;

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

        Point[] region_of_interest_vertices =
            {
            new Point(0, height),
            new Point(width * 0.39, height * 0.6),
            new Point(width * 0.61, height * 0.6),
            new Point(width, height)
        };

        Mat[] mats = Bird_eye_view(image, width, height, region_of_interest_vertices);
        Mat bv_crop = Color_filter(mats[0]);
        int[] bases = Plothistogram(bv_crop);
        //Mat bases = Plothistogram(bv_crop);

        /*ret = slide_window_search(bv_crop, bases[0], bases[1]);

        pts_mean, result = draw_lane_lines(image, bv, ret_m, ret);

        */

        return bv_crop;
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

    int[] Plothistogram(Mat image)
    {
        double[,] copied = new double[image.Width, image.Height];
        image.GetArray(0, 0, copied);

        int[] output = new int[2];

        NDArray copy = new NDArray(copied);

        NDArray histogram = np.sum(copy[copy["320:"], copy[":"]], 0);

        int midpoint = copy.shape[0] / 2;

        int leftbase = np.argmax(histogram[0, midpoint]);
        int rightbase = np.argmax(histogram[midpoint, histogram.shape[0]]) + midpoint;

        output[0] = leftbase;
        output[1] = rightbase;

        Debug.Log(midpoint);

        return output;
    }

    /*Mat slide_window_search(Mat image, int left_current, int right_current)
    {
        byte[] binary_warped = image.ToBytes();
        var output = np.dstack(binary_warped, binary_warped, binary_warped);
        int nwindow = 12;

        Mat nonzero = image.FindNonZero();
        byte[] b_nonzero = nonzero.ToBytes();

        Mat nonzero_y = 

        var nonzero_y = np.array[b_nonzero[0]];

    }*/

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
