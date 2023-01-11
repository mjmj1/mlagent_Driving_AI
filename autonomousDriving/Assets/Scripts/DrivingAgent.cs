using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

public class DrivingAgent : Agent
{
    //휠 콜라이더 4개
    [SerializeField]
    private WheelCollider[] wheels = new WheelCollider[4];

    // 차량 모델의 바퀴 부분 4개
    [SerializeField]
    private GameObject[] wheelMesh = new GameObject[4];

    [SerializeField]
    private float power; // 바퀴를 회전시킬 힘

    [SerializeField]
    private float downForceValue;

    [SerializeField]
    private float radius = 6;

    enum driveType
    {
        FRONTDRIVE,
        REARDRIVE,
        ALLDRIVE
    }

    [SerializeField] driveType drive;

    private new Transform transform;
    private new Rigidbody rigidbody;

    float reward;

    public void Update()
    {
        UpdateMeshesPostion();
        AddDownForce();
    }

    public override void Initialize()
    {
        MaxStep = 10000;

        transform = GetComponent<Transform>();
        rigidbody = GetComponent<Rigidbody>();

        // 무게 중심을 y축 아래방향으로 낮춘다.
        rigidbody.centerOfMass = new Vector3(0, -1f, 0);

        for (int i = 0; i < wheelMesh.Length; i++)
        {
            // 휠콜라이더의 위치를 바퀴메쉬의 위치로 각각 이동시킨다.
            wheels[i].steerAngle = 90;
            wheels[i].transform.position = wheelMesh[i].transform.position;
        }
    }

    public override void OnEpisodeBegin()
    {
        rigidbody.velocity = rigidbody.angularVelocity = Vector3.zero;
        transform.localPosition = new Vector3(0f, 0.5f, 0);
        transform.localRotation = Quaternion.identity;

        Resources.UnloadUnusedAssets();
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var action = actions.ContinuousActions;

        Drive(action[0]);
        SteerVehicle(action[1]);

        if (action[0] > 0)
        {
            reward = 1f;
        }
        else if (action[0] < 0)
        {
            reward = -1f;
        }

        if (transform.position.y < 0)
        {
            SetReward(-1f);
            EndEpisode();
        }

        Debug.Log(transform.parent.name + " : " + action[0] + ", " + action[1]);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var action = actionsOut.ContinuousActions;

        action[0] = Input.GetAxis("Vertical");
        action[1] = Input.GetAxis("Horizontal");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathLine"))
        {
            SetReward(-1f);
            EndEpisode();
        }

        if (other.CompareTag("SafeZone"))
        {
            AddReward(0.1f * reward);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("SafeZone"))
        { 
            AddReward(-1f / MaxStep);
        }

        if (other.CompareTag("StopZone"))
        {
            AddReward(1f / MaxStep);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("StopZone"))
        {
            AddReward(0.1f * reward);
        }
    }

    void Drive(float vertical)
    {
        // 전륜 구동일 때
        if (drive == driveType.ALLDRIVE)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].motorTorque = vertical * (power / 4);
            }

            if (vertical == 0)	// 전진 중이 아닐 때
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
                wheels[i].motorTorque = vertical * (power / 2);
            }
            if (vertical == 0)
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
                wheels[i].motorTorque = vertical * (power / 2);
            }
            if (vertical == 0)
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
        rigidbody.AddForce(-transform.up * downForceValue * rigidbody.velocity.magnitude);
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

    void SteerVehicle(float horizontal)
    {
        //steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * horizontalInput;
        if (horizontal > 0)
        {   // rear tracks size is set to 1.5f          wheel base has been set to 2.55f
            wheels[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * horizontal + 90;
            wheels[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius - (1.5f / 2))) * horizontal + 90;
        }
        else if (horizontal < 0)
        {
            wheels[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius - (1.5f / 2))) * horizontal + 90;
            wheels[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * horizontal + 90;
            // transform.Rotate(Vector3.up * steerHelping)
        }
        else
        {
            wheels[0].steerAngle = 90;
            wheels[1].steerAngle = 90;
        }
    }
}
