using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(NoiseTesting))]
public class NoiseTestButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        NoiseTesting myScript = (NoiseTesting)target;
        if(GUILayout.Button("Make noise"))
        {
            myScript.Noise();
        }

        if(GUILayout.Button("Up"))
        {
            myScript.Up();
        }
        if(GUILayout.Button("Down"))
        {
            myScript.Down();
        }
        if(GUILayout.Button("Right"))
        {
            myScript.Right();
        }
        if(GUILayout.Button("Left"))
        {
            myScript.Left();
        }
    }
}