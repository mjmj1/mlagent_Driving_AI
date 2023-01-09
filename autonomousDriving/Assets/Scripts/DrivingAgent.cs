using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using OpenCvSharp;

public class DrivingAgent : Agent
{
    //ÈÙ ÄÝ¶óÀÌ´õ 4°³
    [SerializeField]
    private WheelCollider[] wheels = new WheelCollider[4];

    // Â÷·® ¸ðµ¨ÀÇ ¹ÙÄû ºÎºÐ 4°³
    [SerializeField]
    private GameObject[] wheelMesh = new GameObject[4];

    [SerializeField]
    private float power; // ¹ÙÄû¸¦ È¸Àü½ÃÅ³ Èû

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
    private StageManager stageManager;

    float reward;
    int idx;

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
        //stageManager = transform.parent.GetComponent<StageManager>();

        //idx = Random.Range(0, 5);

        // ¹«°Ô Áß½ÉÀ» yÃà ¾Æ·¡¹æÇâÀ¸·Î ³·Ãá´Ù.
        rigidbody.centerOfMass = new Vector3(0, -1f, 0);

        for (int i = 0; i < wheelMesh.Length; i++)
        {
            // ÈÙÄÝ¶óÀÌ´õÀÇ À§Ä¡¸¦ ¹ÙÄû¸Þ½¬ÀÇ À§Ä¡·Î °¢°¢ ÀÌµ¿½ÃÅ²´Ù.
            wheels[i].steerAngle = 90;
            wheels[i].transform.position = wheelMesh[i].transform.position;
        }
    }

    public override void OnEpisodeBegin()
    {
        rigidbody.velocity = rigidbody.angularVelocity = Vector3.zero;
        transform.localPosition = new Vector3(0f, 0.2f, 0);
        transform.localRotation = Quaternion.identity;

        /*stageManager.ActiveMap(idx, false);
        idx = Random.Range(0, 5);
<<<<<<< HEAD
        stageManager.ActiveMap(idx, true);*/

        Resources.UnloadUnusedAssets();
=======
        stageManager.ActiveMap(idx, true);
>>>>>>> parent of 0ede0e7f (ë©”ëª¨ë¦¬ ëˆ„ìˆ˜ í•´ê²° 2)
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
            SetReward(-0.5f);
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
            AddReward(1f / MaxStep);
        }
    }

    void Drive(float vertical)
    {
        // Àü·û ±¸µ¿ÀÏ ¶§
        if (drive == driveType.ALLDRIVE)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].motorTorque = vertical * (power / 4);
            }

            if (vertical == 0)	// ÀüÁø ÁßÀÌ ¾Æ´Ò ¶§
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].brakeTorque = power / 4;
                }
            }
            else	// Å°¸¦ ´­·¶À» ¶§
            {
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i].brakeTorque = 0; // ºê·¹ÀÌÅ© ÇØÁ¦
                }
            }
        }

        else if (drive == driveType.REARDRIVE)	// ÈÄ·û±¸µ¿ÀÏ ¶§
        {
            // µÞ¹ÙÄû¿¡¸¸.
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
        else	// Àü·û ±¸µ¿ÀÏ ¶§
        {	// ¾Õ¹ÙÄû¿¡¸¸
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
