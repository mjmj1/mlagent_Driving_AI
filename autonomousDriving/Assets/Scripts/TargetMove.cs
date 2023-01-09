using PathCreation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class TargetMove : MonoBehaviour
{
    Rigidbody rb;
    public PathCreator PathCreator;
    float distanceTravelled;
    public float speed = 5;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        distanceTravelled += speed * Time.deltaTime;
        transform.position = PathCreator.path.GetPointAtDistance(distanceTravelled);
        transform.rotation = PathCreator.path.GetRotationAtDistance(distanceTravelled);
    }

    private void FixedUpdate()
    {
        speed = speed * Random.Range(0.1f, 2f) * Time.deltaTime;
    }
}
