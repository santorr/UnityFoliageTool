using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FoliageSplatterTool))]
public class FoliageSplatterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FoliageSplatterTool foliageTool = (FoliageSplatterTool)target;

        if (GUILayout.Button("Clear"))
        {
            foliageTool.ClearData();
        }

        if (GUILayout.Button("Generate"))
        {
            foliageTool.ClearData();
            foliageTool.GenerateData();
        }
    }
}
