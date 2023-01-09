using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class VehicleController : MonoBehaviour
{
    //휠 콜라이더 4개
    [SerializeField]
    private WheelCollider[] wheels = new WheelCollider[4];
    // 차량 모델의 바퀴 부분 4개
    GameObject[] wheelMesh = new GameObject[4];

    [SerializeField]
    private float power = 400f; // 바퀴를 회전시킬 힘

    [SerializeField]
    public float downForceValue;

    [SerializeField]
    public float radius = 5;

    enum driveType
    {
        FRONTDRIVE,
        REARDRIVE,
        ALLDRIVE
    }

    [SerializeField] driveType drive;

    InputManager IM;
    Rigidbody rb;

    void Start()
    {
        IM = GetComponent<InputManager>();
        rb = GetComponent<Rigidbody>();

        // 무게 중심을 y축 아래방향으로 낮춘다.
        rb.centerOfMass = new Vector3(0, 0, 0);

        // 바퀴 모델을 태그를 통해서 찾아온다.(차량이 변경되더라도 자동으로 찾기위해서)
        wheelMesh = GameObject.FindGameObjectsWithTag("WheelMesh");

        for (int i = 0; i < wheelMesh.Length; i++)
        {	// 휠콜라이더의 위치를 바퀴메쉬의 위치로 각각 이동시킨다.
            wheels[i].steerAngle = 90;
            wheels[i].transform.position = wheelMesh[i].transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMeshesPostion(); //바퀴가 돌아가는게 보이도록 함
    }

    void FixedUpdate()
    {
        AddDownForce();
        Drive();
        SteerVehicle();
    }
    
    void Drive()
    {
        // 전륜 구동일 때
        if (drive == driveType.ALLDRIVE)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].motorTorque = IM.vertical * (power / 4);
            }

            if (IM.vertical == 0)	// 전진 중이 아닐 때
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].brakeTorque = power / 4;
                }
            }
            else	// 키를 눌렀을 때
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].brakeTorque = 0; // 브레이크 해제
                }
            }
        }

        else if (drive == driveType.REARDRIVE)	// 후륜구동일 때
        {
            // 뒷바퀴에만.
            for (int i = 2; i < wheels.Length; i++)
            {
                wheels[i].motorTorque = IM.vertical * (power / 2);
            }
            if (IM.vertical == 0)
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].brakeTorque = power / 2;
                }
            }
            else
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].brakeTorque = 0;
                }
            }
        }
        else	// 전륜 구동일 때
        {	// 앞바퀴에만
            for (int i = 0; i < 2; i++)
            {
                wheels[i].motorTorque = IM.vertical * (power / 2);
            }
            if (IM.vertical == 0)
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].brakeTorque = power / 2;
                }
            }
            else
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].brakeTorque = 0;
                }
            }
        }
    }

    void AddDownForce()
    {
        rb.AddForce(-transform.up * downForceValue * rb.velocity.magnitude);
    }

    void UpdateMeshesPostion()
    {
        for (int i = 0; i < 4; i++)
        {
            Quaternion quat;
            Vector3 pos;

            wheels[i].GetWorldPose(out pos, out quat);
            wheelMesh[i].transform.position = pos;
            wheelMesh[i].transform.rotation = quat * Quaternion.Euler(new Vector3(0, -90, 0));
        }
    }

    void SteerVehicle()
    {
        // 애커만 조향
        //steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * horizontalInput;
        if (Input.GetAxis("Horizontal") > 0)
        {   // rear tracks size is set to 1.5f          wheel base has been set to 2.55f
            wheels[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * IM.horizontal + 90;
            wheels[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius - (1.5f / 2))) * IM.horizontal + 90;
        }
        else if (Input.GetAxis("Horizontal") < 0)
        {
            wheels[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius - (1.5f / 2))) * IM.horizontal + 90;
            wheels[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * IM.horizontal + 90;
            // transform.Rotate(Vector3.up * steerHelping)
        }
        else
        {
            wheels[0].steerAngle = 90;
            wheels[1].steerAngle = 90;
        }
    }
}
