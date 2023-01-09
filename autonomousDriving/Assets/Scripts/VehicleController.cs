using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class VehicleController : MonoBehaviour
{
    //�� �ݶ��̴� 4��
    [SerializeField]
    private WheelCollider[] wheels = new WheelCollider[4];
    // ���� ���� ���� �κ� 4��
    GameObject[] wheelMesh = new GameObject[4];

    [SerializeField]
    private float power = 400f; // ������ ȸ����ų ��

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

        // ���� �߽��� y�� �Ʒ��������� �����.
        rb.centerOfMass = new Vector3(0, 0, 0);

        // ���� ���� �±׸� ���ؼ� ã�ƿ´�.(������ ����Ǵ��� �ڵ����� ã�����ؼ�)
        wheelMesh = GameObject.FindGameObjectsWithTag("WheelMesh");

        for (int i = 0; i < wheelMesh.Length; i++)
        {	// ���ݶ��̴��� ��ġ�� �����޽��� ��ġ�� ���� �̵���Ų��.
            wheels[i].steerAngle = 90;
            wheels[i].transform.position = wheelMesh[i].transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMeshesPostion(); //������ ���ư��°� ���̵��� ��
    }

    void FixedUpdate()
    {
        AddDownForce();
        Drive();
        SteerVehicle();
    }
    
    void Drive()
    {
        // ���� ������ ��
        if (drive == driveType.ALLDRIVE)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].motorTorque = IM.vertical * (power / 4);
            }

            if (IM.vertical == 0)	// ���� ���� �ƴ� ��
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].brakeTorque = power / 4;
                }
            }
            else	// Ű�� ������ ��
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].brakeTorque = 0; // �극��ũ ����
                }
            }
        }

        else if (drive == driveType.REARDRIVE)	// �ķ������� ��
        {
            // �޹�������.
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
        else	// ���� ������ ��
        {	// �չ�������
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
        // ��Ŀ�� ����
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
