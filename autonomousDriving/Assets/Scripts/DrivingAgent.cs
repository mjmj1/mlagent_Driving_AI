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

    enum DriveType
    {
        FRONTDRIVE,
        REARDRIVE,
        ALLDRIVE
    }

    [SerializeField] DriveType drive;

    private new Transform transform;
    private new Rigidbody rigidbody;

    float reward = 0;

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
            wheels[i].steerAngle = 0;
            wheels[i].transform.position = wheelMesh[i].transform.position;
        }
    }

    public override void OnEpisodeBegin()
    {
        Resources.UnloadUnusedAssets();

        rigidbody.velocity = rigidbody.angularVelocity = Vector3.zero;

        switch(Random.Range(0, 4))
        {
            case 0:
                {
                    transform.localPosition = new Vector3(-1.4f, 0.7f, 0f);
                    transform.localRotation = Quaternion.identity;
                    break;
                }
            case 1:
                {
                    transform.localPosition = new Vector3(2.35f, 0.7f, 0f);
                    transform.localRotation = Quaternion.identity;
                    break;
                }
            case 2:
                {
                    transform.localPosition = new Vector3(-1.4f, 0.7f, 0f);
                    transform.localRotation = Quaternion.Euler(new Vector3(0, 180f, 0));
                    break;
                }
            case 3:
                {
                    transform.localPosition = new Vector3(2.35f, 0.7f, 0f);
                    transform.localRotation = Quaternion.Euler(new Vector3(0, 180f, 0));
                    break;
                }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var action = actions.ContinuousActions;

        Drive(action[0]);
        SteerVehicle(action[1]);

        UpdateMeshesPostion();
        AddDownForce();

        if (action[0] >= 0.3f)
        {
            reward = 1f;
        }
        else if (action[0] < 0.3f)
        {
            reward = -1f;
        }

        if (transform.position.y < 0)
        {
            SetReward(-1f);
            EndEpisode();
        }

        AddReward(reward / MaxStep);

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
    }

    void Drive(float vertical)
    {
        // 전륜 구동일 때
        if (drive == DriveType.ALLDRIVE)
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

        else if (drive == DriveType.REARDRIVE)	// 후륜구동일 때
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
            wheels[i].GetWorldPose(out Vector3 pos, out Quaternion quat);
            wheelMesh[i].transform.position = pos;
            wheelMesh[i].transform.rotation = quat;
        }
    }

    void SteerVehicle(float horizontal)
    {
        //steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * horizontalInput;
        if (horizontal > 0)
        {   // rear tracks size is set to 1.5f          wheel base has been set to 2.55f
            wheels[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * horizontal;
            wheels[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius - (1.5f / 2))) * horizontal;
        }
        else if (horizontal < 0)
        {
            wheels[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius - (1.5f / 2))) * horizontal;
            wheels[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * horizontal;
            // transform.Rotate(Vector3.up * steerHelping)
        }
        else
        {
            wheels[0].steerAngle = 0;
            wheels[1].steerAngle = 0;
        }
    }
}
