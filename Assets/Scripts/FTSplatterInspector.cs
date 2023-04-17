using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FTSplatter))]
public class FTSplatterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FTSplatter foliageTool = (FTSplatter)target;

        if (GUILayout.Button("Generate"))
        {
            foliageTool.GenerateData();
        }
    }
}
