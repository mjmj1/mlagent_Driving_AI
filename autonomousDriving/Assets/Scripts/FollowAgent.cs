using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using PathCreation.Examples;

public class FollowAgent : Agent
{
    //�� �ݶ��̴� 4��
    [SerializeField]
    private WheelCollider[] wheels = new WheelCollider[4];

    // ���� ���� ���� �κ� 4��
    [SerializeField]
    private GameObject[] wheelMesh = new GameObject[4];

    [SerializeField]
    private float power; // ������ ȸ����ų ��

    [SerializeField]
    private float downForceValue;

    [SerializeField]
    private float radius = 6;

    [SerializeField]
    private PathFollower follower;

    [SerializeField]
    private GameObject Target;

    enum DriveType
    {
        FRONTDRIVE,
        REARDRIVE,
        ALLDRIVE
    }

    [SerializeField] DriveType drive;

    private new Transform transform;
    private new Rigidbody rigidbody;

    public void Update()
    {
        UpdateMeshesPostion();
        AddDownForce();
    }

    public override void Initialize()
    {
        MaxStep = 5000;

        transform = GetComponent<Transform>();
        rigidbody = GetComponent<Rigidbody>();

        // ���� �߽��� y�� �Ʒ��������� �����.
        rigidbody.centerOfMass = new Vector3(0, -1f, 0);

        for (int i = 0; i < wheelMesh.Length; i++)
        {
            // ���ݶ��̴��� ��ġ�� �����޽��� ��ġ�� ���� �̵���Ų��.
            wheels[i].steerAngle = 0;
            wheels[i].transform.position = wheelMesh[i].transform.position;
        }
    }

    public override void OnEpisodeBegin()
    {
        rigidbody.velocity = rigidbody.angularVelocity = Vector3.zero;
        transform.localPosition = new Vector3(0f, -0.4f, 0f);
        transform.localRotation = Quaternion.Euler(new Vector3(0, 90f, 0));
        
        follower.distanceTravelled = 0;
        follower.speed = 2f;
        follower.count = 2.5f;

        Resources.UnloadUnusedAssets();
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var action = actions.ContinuousActions;

        Drive(action[0]);
        SteerVehicle(action[1]);

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
        if(other.CompareTag("DeathLine"))
        {
            SetReward(-1f);
            EndEpisode();
        }

        if (other.CompareTag("Target"))
        {
            SetReward(-1f);
            EndEpisode();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("SafeZone"))
        {
            AddReward(1f / MaxStep);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SafeZone"))
        {
            SetReward(-1f);
            EndEpisode();
        }
    }

    void Drive(float vertical)
    {
        // ���� ������ ��
        if (drive == DriveType.ALLDRIVE)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].motorTorque = vertical * (power / 4);
            }

            if (vertical == 0)	// ���� ���� �ƴ� ��
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

        else if (drive == DriveType.REARDRIVE)	// �ķ������� ��
        {
            // �޹�������.
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
        else	// ���� ������ ��
        {	// �չ�������
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
