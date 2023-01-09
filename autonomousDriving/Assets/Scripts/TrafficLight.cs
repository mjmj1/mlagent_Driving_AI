using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    [SerializeField]
    private GameObject Red;

    [SerializeField]
    private GameObject Yellow;

    [SerializeField]
    private GameObject Green;

    [SerializeField]
    private GameObject DeathLine;

    public float count = 10f;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(count < 0)
        {
            Yellow.SetActive(true);
            Red.SetActive(false);
            Green.SetActive(true);
            DeathLine.SetActive(true);
            count = Random.Range(20, 30);
        }
        else if (count < 3)
        {
            Green.SetActive(true);
            Yellow.SetActive(false);
            count -= Time.deltaTime;
        }
        else if(count < 12)
        {
            Green.SetActive(false);
            Yellow.SetActive(true);
            Red.SetActive(true);
            DeathLine.SetActive(false);
            count -= Time.deltaTime;
        }
        else
        {
            count -= Time.deltaTime;
        }
    }
}
