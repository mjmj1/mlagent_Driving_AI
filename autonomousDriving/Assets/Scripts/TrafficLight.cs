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

    public float count = 10f;

    void Update()
    {
        //������
        if(count < 0)
        {
            Red.SetActive(false);
            Yellow.SetActive(true);
            Green.SetActive(true);
            
            StopLine.SetActive(true);
            StopZone.SetActive(true);

            count = Random.Range(20, 30);
        }
        //�����
        else if (count < 3)
        {
            Green.SetActive(true);
            Yellow.SetActive(false);

            count -= Time.deltaTime;
        }
        //�ʷϺ�
        else if (count < 12)
        {
            Red.SetActive(true);
            Yellow.SetActive(true);
            Green.SetActive(false);

            StopLine.SetActive(false);
            StopZone.SetActive(false);

            count -= Time.deltaTime;
        }
        else
        {
            count -= Time.deltaTime;
        }
    }
}
