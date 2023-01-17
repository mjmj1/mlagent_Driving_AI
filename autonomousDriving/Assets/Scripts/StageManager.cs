using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public List<GameObject> maps = new();

    void Start()
    {
        maps.Add(transform.GetChild(0).gameObject);
        maps.Add(transform.GetChild(1).gameObject);
        //maps.Add(transform.GetChild(2).gameObject);
        //maps.Add(transform.GetChild(3).gameObject);
        //maps.Add(transform.GetChild(4).gameObject);
    }

    public void ActiveMap(int index, bool set)
    {
        maps[index].SetActive(set);
    }
}
