using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using UnityEngine.UI;
using System;
using System.Linq;

public static class ExtensionMethod
{
    public static Texture2D toTexture2D(this RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        var old_rt = RenderTexture.active;
        RenderTexture.active = rTex;

        tex.ReadPixels(new UnityEngine.Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();

        RenderTexture.active = old_rt;
        return tex;
    }
}

public class LaneDetect : MonoBehaviour
{
    [SerializeField]
    private RawImage rawImage;

    Camera cam;

    List<List<Vec4i>> separated_lines = new();
    List<Point> lane = new();

    double img_center;
    double left_m, right_m;
    Point left_b, right_b;
    bool left_detect = false, right_detect = false;

    double poly_height = 50;
    double side_width = 70;
    double side_width_m = 200;

    Mat filter_colors(Mat img_frame)
    {
        Mat output = img_frame.Clone();

        Mat img_hsv = new();
        Mat white_mask = new();
        Mat white_image = new();
        Mat yellow_mask = new();
        Mat yellow_image = new();

        //흰색 필터링
        Cv2.InRange(output, new(170, 170, 170), new(255, 255, 255), white_mask);
        Cv2.BitwiseAnd(output, output, white_image, white_mask);

        Cv2.CvtColor(output, img_hsv, ColorConversionCodes.BGR2HSV);

        //노란색 필터링
        Cv2.InRange(img_hsv, new(0, 130, 130), new(40, 255, 255), yellow_mask);
        Cv2.BitwiseAnd(output, output, yellow_image, yellow_mask);

        //두 영상을 합친다.
        Cv2.AddWeighted(white_image, 1.0, yellow_image, 1.0, 0.0, output);

        return output;
    }

    Mat limit_region(Mat img_edges)
    {
        int width = img_edges.Cols;
        int height = img_edges.Rows;

        Mat output = new();
        Mat mask = Mat.Zeros(height, width, MatType.CV_8UC1);

        //관심 영역 정점 계산
        Point[] points =
            {
            /*new Point((width * (1 - poly_bottom_width)) / 2, height),
            new Point((width *(1 - poly_top_width)) / 2, height - height * poly_height),
            new Point(width -(width *(1 - poly_top_width)) / 2, height - height * poly_height),
	        new Point(width -(width *(1 - poly_bottom_width)) / 2, height)*/
            new Point(side_width, height),
            new Point(width - side_width, height),
            new Point(width - side_width_m, height / 2  + poly_height),
            new Point(side_width_m, height / 2 + poly_height)
        };

        //정점으로 정의된 다각형 내부의 색상을 채워 그린다.
        Cv2.FillConvexPoly(mask, points, new Scalar(255, 0, 0));

        //결과를 얻기 위해 edges 이미지와 mask를 곱한다.
        Cv2.BitwiseAnd(img_edges, mask, output);

        return output;
    }

    List<List<Vec4i>> separateLine(Mat img_edges, LineSegmentPoint[] lines)
    {
        List<List<Vec4i>> output = new(2);
        Point p1, p2;
        List<double> slopes = new();
        List<Vec4i> final_lines = new();
        List<Vec4i> left_lines = new();
        List<Vec4i> right_lines = new();

        double slope_thresh = 0.1;
        
        //검출된 직선들의 기울기를 계산
        for (int i = 0; i < lines.Count(); i++)
        {
            LineSegmentPoint line = lines[i];

            p1 = new Point(line.P1.X, line.P1.Y);
            p2 = new Point(line.P2.X, line.P2.Y);

            Vec4i pt = new(p1.X, p1.Y, p2.X, p2.Y);

            double slope;
            if (p2.X - p1.X == 0)  //코너 일 경우
                slope = 999.0;
            else
                slope = (p2.Y - p1.Y) / (double)(p2.X - p1.X);

            //기울기가 너무 수평인 선은 제외
            if (Math.Abs(slope) > slope_thresh)
            {
                slopes.Add(slope);
                final_lines.Add(pt);
            }
        }

        //선들을 좌우 선으로 분류
        img_center = (double)((img_edges.Cols / 2));

        for (int i = 0; i < final_lines.Count; i++)
        {
            p1 = new Point(final_lines[i][0], final_lines[i][1]);
            p2 = new Point(final_lines[i][2], final_lines[i][3]);

            if (slopes[i] > 0 && p1.X > img_center && p2.X > img_center)
            {
                right_detect = true;
                right_lines.Add(final_lines[i]);
            }
            else if (slopes[i] < 0 && p1.X < img_center && p2.X < img_center)
            {
                left_detect = true;
                left_lines.Add(final_lines[i]);
            }
        }

        output.Insert(0, right_lines);
        output.Insert(1, left_lines);

        return output;
    }

    List<Point> regression(List<List<Vec4i>> separatedLines, Mat img_input)
    {
        List<Point> output = new(4);
        Point p1, p2, p3, p4;
        Line2D left_line;
        Line2D right_line;
        List<Point> left_points = new();
        List<Point> right_points = new();

        if (right_detect)
        {
            foreach (Vec4i i in separatedLines[0])
            {
                p1 = new Point(i[0], i[1]);
                p2 = new Point(i[2], i[3]);

                right_points.Add(p1);
                right_points.Add(p2);
            }

            if (right_points.Count > 0)
            {
                //주어진 contour에 최적화된 직선 추출
                right_line = Cv2.FitLine(right_points, DistanceTypes.L2, 0, 0.01, 0.01);
                
                right_m = right_line.Vy / right_line.Vx;  //기울기
                right_b = new Point(right_line.X1, right_line.Y1);
            }
        }

        if (left_detect)
        {
            foreach (Vec4i j in separatedLines[1])
            {
                p3 = new Point(j[0], j[1]);
                p4 = new Point(j[2], j[3]);

                left_points.Add(p3);
                left_points.Add(p4);
            }

            if (left_points.Count > 0)
            {
                //주어진 contour에 최적화된 직선 추출
                left_line = Cv2.FitLine(left_points, DistanceTypes.L2, 0, 0.01, 0.01);

                left_m = left_line.Vy / left_line.Vx;  //기울기
                left_b = new Point(left_line.X1, left_line.Y1);
            }
        }

        //좌우 선 각각의 두 점을 계산한다.
        //y = m*x + b  --> x = (y-b) / m
        int y1 = img_input.Rows;
        int y2 = img_input.Cols / 2 - 50;

        double right_x1 = ((y1 - right_b.Y) / right_m) + right_b.X;
        double right_x2 = ((y2 - right_b.Y) / right_m) + right_b.X;

        double left_x1 = ((y1 - left_b.Y) / left_m) + left_b.X;
        double left_x2 = ((y2 - left_b.Y) / left_m) + left_b.X;

        output.Insert(0, new Point(right_x1, y1));
        output.Insert(1, new Point(right_x2, y2));
        output.Insert(2, new Point(left_x1, y1));
        output.Insert(3, new Point(left_x2, y2));

        return output;
    }

    Mat drawLine(Mat img_input, List<Point> lane)
    {
        List<Point> poly_points = new();

        Mat output = new();
        img_input.CopyTo(output);

        poly_points.Add(lane[2]);
        poly_points.Add(lane[0]);
        poly_points.Add(lane[1]);
        poly_points.Add(lane[3]);

        Cv2.FillConvexPoly(output, poly_points, new Scalar(0, 230, 30), LineTypes.AntiAlias, 0);  //다각형 색 채우기
        Cv2.AddWeighted(output, 0.3, img_input, 0.7, 0, output);  //영상 합하기

        //좌우 차선 선 그리기
        Cv2.Line(output, lane[0], lane[1], new Scalar(0, 255, 255), 5, LineTypes.AntiAlias);
        Cv2.Line(output, lane[2], lane[3], new Scalar(0, 255, 255), 5, LineTypes.AntiAlias);

        img_input.Release();

        return output;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        Texture2D img_copy = cam.targetTexture.toTexture2D();
        Mat img_frame = OpenCvSharp.Unity.TextureToMat(img_copy);

        Mat img_filter = filter_colors(img_frame);

        //3. 영상을 GrayScale 으로 변환한다.
        Cv2.CvtColor(img_filter, img_filter, ColorConversionCodes.BGR2GRAY);

        //4. Canny Edge Detection으로 에지를 추출. (잡음 제거를 위한 Gaussian 필터링도 포함)
        Mat blur = new();
        Mat img_edges = new();

        Cv2.GaussianBlur(img_filter, blur, new Size(3, 3), 1, 0, BorderTypes.Default);
        Cv2.Canny(blur, img_edges, 50, 150);

        Mat img_mask = limit_region(img_edges);

        LineSegmentPoint[] line = Cv2.HoughLinesP(img_mask, 1, Math.PI / 180, 20, 10, 20);

        if (line.Count() > 0)
        {
            //7. 추출한 직선성분으로 좌우 차선에 있을 가능성이 있는 직선들만 따로 뽑아서 좌우 각각 직선을 계산한다. 
            // 선형 회귀를 하여 가장 적합한 선을 찾는다.
            separated_lines = separateLine(img_mask, line);
            lane = regression(separated_lines, img_frame);

            //9. 영상에 최종 차선을 선으로 그리고 내부 다각형을 색으로 채운다. 예측 진행 방향 텍스트를 영상에 출력
            Mat img_result = drawLine(img_frame, lane);

            rawImage.texture = OpenCvSharp.Unity.MatToTexture(img_result);

            Resources.UnloadUnusedAssets();
        }
    }
}
