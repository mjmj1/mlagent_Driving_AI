using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public List<GameObject> maps = new();

    private void Start()
    {
        maps.Add(transform.GetChild(0).gameObject);
    }
}
