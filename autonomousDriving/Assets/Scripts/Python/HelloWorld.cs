using System.Collections;
using System.Collections.Generic;
using UnityEditor.Scripting.Python;
using UnityEditor;
using UnityEngine;

public class HelloWorld : MonoBehaviour
{
    void PrintHelloWorldFromPython()
    {
        PythonRunner.RunString(@"
                import UnityEngine;
                UnityEngine.Debug.Log('hello world')
                ");
    }

    private void Start()
    {
        PrintHelloWorldFromPython();
    }

    private void Update()
    {
        
    }
}

