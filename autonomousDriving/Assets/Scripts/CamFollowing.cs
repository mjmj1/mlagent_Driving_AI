using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollowing : MonoBehaviour
{
    // 카메라가 바라볼 대상
    [SerializeField]
    private GameObject cameraView;

    // 카메라의 위치
    [SerializeField]
    private GameObject cameraPos;

    // 카메라가 움직일 속도
    [SerializeField]
    private float speed;

    // 카메라의 처리는 LateUpdate에서 처리하도록 한다.
    private void LateUpdate()
    {
        // Lerp를 사용해서 카메라서 끊김없이 서서히 따라가도록 만들어준다.
        gameObject.transform.position = Vector3.Lerp(transform.position, cameraPos.transform.position, Time.deltaTime * speed);
        // 카메라가 바라볼 대상을 정해준다.
        gameObject.transform.LookAt(cameraView.transform);
    }
}
