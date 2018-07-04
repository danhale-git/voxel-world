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
    }
}