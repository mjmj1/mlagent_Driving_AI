using UnityEditor;
using UnityEditor.Scripting.Python;
using UnityEngine;

public class MenuItem_LaneDetect_V2_Class : MonoBehaviour
{
   public static void LaneDetect_V2()
   {
       PythonRunner.RunFile("Assets/Scripts/Python/LaneDetect_V2.py");
       }
};
