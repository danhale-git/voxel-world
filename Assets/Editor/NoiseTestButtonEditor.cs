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
            myScript.SaveSettings();

            switch(myScript.noiseMode)
            {
                case NoiseTesting.NoiseMode.Draw:
                    myScript.Noise();
                    break;

                case NoiseTesting.NoiseMode.Log1Noise:
                    myScript.LogNoise();
                    break;

                case NoiseTesting.NoiseMode.LogRandomNoise:
                    myScript.LogRandomNoise();
                    break;
            }
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