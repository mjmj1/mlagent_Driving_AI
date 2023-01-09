using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollowing : MonoBehaviour
{
    // ī�޶� �ٶ� ���
    [SerializeField]
    private GameObject cameraView;

    // ī�޶��� ��ġ
    [SerializeField]
    private GameObject cameraPos;

    // ī�޶� ������ �ӵ�
    [SerializeField]
    private float speed;

    // ī�޶��� ó���� LateUpdate���� ó���ϵ��� �Ѵ�.
    private void LateUpdate()
    {
        // Lerp�� ����ؼ� ī�޶� ������� ������ ���󰡵��� ������ش�.
        gameObject.transform.position = Vector3.Lerp(transform.position, cameraPos.transform.position, Time.deltaTime * speed);
        // ī�޶� �ٶ� ����� �����ش�.
        gameObject.transform.LookAt(cameraView.transform);
    }
}
