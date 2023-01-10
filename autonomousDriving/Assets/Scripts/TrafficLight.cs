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
    private GameObject StopLine;

    [SerializeField]
    private GameObject StopZone;

    [SerializeField]
    private GameObject BenefitLine;

    public float count = 10f;

    void Update()
    {
        //빨간불
        if(count < 0)
        {
            Red.SetActive(false);
            Yellow.SetActive(true);
            Green.SetActive(true);
            
            StopLine.SetActive(true);
            StopZone.SetActive(true);
            BenefitLine.SetActive(false);

            count = Random.Range(20, 30);
        }
        //노란불
        else if (count < 3)
        {
            Green.SetActive(true);
            Yellow.SetActive(false);

            count -= Time.deltaTime;
        }
        //초록불
        else if (count < 12)
        {
            Red.SetActive(true);
            Yellow.SetActive(true);
            Green.SetActive(false);

            StopLine.SetActive(false);
            StopZone.SetActive(false);
            BenefitLine.SetActive(true);

            count -= Time.deltaTime;
        }
        else
        {
            count -= Time.deltaTime;
        }
    }
}
