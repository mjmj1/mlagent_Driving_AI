import cv2
import numpy as np


def slide_window_search(binary_warped, left_current, right_current):
    out_img = np.dstack((binary_warped, binary_warped, binary_warped))
    nwindows = 12
    window_height = np.int32(binary_warped.shape[0] / nwindows)
    nonzero = binary_warped.nonzero()  # 선이 있는 부분의 인덱스만 저장
    nonzero_y = np.array(nonzero[0])  # 선이 있는 부분 y의 인덱스 값
    nonzero_x = np.array(nonzero[1])  # 선이 있는 부분 x의 인덱스 값
    margin = 100
    minpix = 50
    left_lane = []
    right_lane = []
    color = [0, 255, 0]
    thickness = 2

    for w in range(nwindows):
        win_y_low = binary_warped.shape[0] - (w + 1) * window_height  # window 윗부분
        win_y_high = binary_warped.shape[0] - w * window_height  # window 아랫 부분
        win_xleft_low = left_current - margin  # 왼쪽 window 왼쪽 위
        win_xleft_high = left_current + margin  # 왼쪽 window 오른쪽 아래
        win_xright_low = right_current - margin  # 오른쪽 window 왼쪽 위
        win_xright_high = right_current + margin  # 오른쪽 window 오른쪽 아래

        cv2.rectangle(out_img, (win_xleft_low, win_y_low), (win_xleft_high, win_y_high), color, thickness)
        cv2.rectangle(out_img, (win_xright_low, win_y_low), (win_xright_high, win_y_high), color, thickness)
        good_left = ((nonzero_y >= win_y_low) & (nonzero_y < win_y_high) & (nonzero_x >= win_xleft_low) & (nonzero_x < win_xleft_high)).nonzero()[0]
        good_right = ((nonzero_y >= win_y_low) & (nonzero_y < win_y_high) & (nonzero_x >= win_xright_low) & (nonzero_x < win_xright_high)).nonzero()[0]
        left_lane.append(good_left)
        right_lane.append(good_right)

        if len(good_left) > minpix:
            left_current = np.int32(np.mean(nonzero_x[good_left]))
        if len(good_right) > minpix:
            right_current = np.int32(np.mean(nonzero_x[good_right]))

    left_lane = np.concatenate(left_lane)  # np.concatenate() -> array를 1차원으로 합침
    right_lane = np.concatenate(right_lane)

    leftx = nonzero_x[left_lane]
    lefty = nonzero_y[left_lane]
    rightx = nonzero_x[right_lane]
    righty = nonzero_y[right_lane]

    left_fit = np.polyfit(lefty, leftx, 2)
    right_fit = np.polyfit(righty, rightx, 2)

    ploty = np.linspace(0, binary_warped.shape[0] - 1, binary_warped.shape[0])
    left_fitx = left_fit[0] * ploty ** 2 + left_fit[1] * ploty + left_fit[2]
    right_fitx = right_fit[0] * ploty ** 2 + right_fit[1] * ploty + right_fit[2]

    ltx = np.trunc(left_fitx)  # np.trunc() -> 소수점 부분을 버림
    rtx = np.trunc(right_fitx)

    out_img[nonzero_y[left_lane], nonzero_x[left_lane]] = [255, 0, 0]
    out_img[nonzero_y[right_lane], nonzero_x[right_lane]] = [0, 0, 255]

    ret = {'left_fitx': ltx, 'right_fitx': rtx, 'ploty': ploty}

    return ret


def plothistogram(image):
    histogram = np.sum(image[image.shape[0]//2:, :], axis=0)
    midpoint = np.int32(histogram.shape[0]/2)
    leftbase = np.argmax(histogram[:midpoint])
    rightbase = np.argmax(histogram[midpoint:]) + midpoint

    return leftbase, rightbase


def region_of_interest(img, vertices):
    mask = np.zeros_like(img)

    match_mask_color = 255
    cv2.fillPoly(mask, vertices, match_mask_color)
    masked_image = cv2.bitwise_and(img, mask)
    return masked_image


def bird_eye_view(frame, width, height, region_of_interest_vertices):

    left_bottom = region_of_interest_vertices[0]
    left_top = region_of_interest_vertices[1]
    right_top = region_of_interest_vertices[2]
    right_bottom = region_of_interest_vertices[3]

    pts1 = np.float32([[left_top, left_bottom, right_top, right_bottom]])
    # 좌표의 이동점
    pts2 = np.float32([[0, 0], [0, height], [width, 0], [width, height]])

    # pts1의 좌표에 표시. perspective 변환 후 이동 점 확인.
    M = cv2.getPerspectiveTransform(pts1, pts2)
    ret_m = cv2.getPerspectiveTransform(pts2, pts1)
    b_v = cv2.warpPerspective(frame, M, (width, height))

    return b_v, ret_m


def image_process(image):

    hsv = cv2.cvtColor(image, cv2.COLOR_BGR2HSV)

    upper_white = np.array([30, 255, 255])
    lower_white = np.array([0, 0, 200])

    upper_yellow = np.array([40, 255, 255])
    lower_yellow = np.array([20, 30, 100])

    mask_white = cv2.inRange(hsv, lower_white, upper_white)
    mask_yellow = cv2.inRange(hsv, lower_yellow, upper_yellow)

    res = cv2.bitwise_and(image, image, mask=mask_white)
    res2 = cv2.bitwise_and(image, image, mask=mask_yellow)
    res3 = cv2.bitwise_or(res, res2)

    gray_image = cv2.cvtColor(res3, cv2.COLOR_RGB2GRAY)
    canny_image = cv2.Canny(gray_image, 100, 120)

    return canny_image


def process(image):

    height = image.shape[0]
    width = image.shape[1]
    region_of_interest_vertices = [
        (0, height),
        (int(width*0.332),int(height*0.6)),
        (int(width * 0.65), int(height * 0.6)),
        (width, height)
    ]

    bv, ret_m = bird_eye_view(image, width, height, region_of_interest_vertices)
    bv_crop = image_process(bv)

    rightbase, leftbase = plothistogram(bv_crop)

    ret = slide_window_search(bv_crop, leftbase, rightbase)

    pts_mean, result = draw_lane_lines(image, bv, ret_m, ret)

    return result


def draw_lane_lines(original_image, warped_image, Minv, draw_info):
    left_fitx = draw_info['left_fitx']
    right_fitx = draw_info['right_fitx']
    ploty = draw_info['ploty']
    warp_zero = np.zeros_like(warped_image).astype(np.uint8)

    pts_left = np.array([np.transpose(np.vstack([left_fitx, ploty]))])
    pts_right = np.array([np.flipud(np.transpose(np.vstack([right_fitx, ploty])))])
    pts = np.hstack((pts_left, pts_right))

    mean_x = np.mean((left_fitx, right_fitx), axis=0)
    print(mean_x)
    pts_mean = np.array([np.flipud(np.transpose(np.vstack([mean_x, ploty])))])

    cv2.fillPoly(warp_zero, np.int32([pts]), color=(0, 255, 0))
    cv2.fillPoly(warp_zero, np.int32([pts_mean]), color=(0, 255, 0))
    cv2.polylines(warp_zero, np.int32([pts_right]), 0, (0, 255, 255), 20)
    cv2.polylines(warp_zero, np.int32([pts_left]), 0, (0, 255, 255), 20)

    newwarp = cv2.warpPerspective(warp_zero, Minv, (original_image.shape[1], original_image.shape[0]))
    # result = cv2.addWeighted(original_image, 1, newwarp, 1, 0)

    return pts_mean, newwarp


def detect_line(image_data):
        try:
            frame = process(image_data)
            cv2.imshow('LaneDetect', frame)
        except:
            pass