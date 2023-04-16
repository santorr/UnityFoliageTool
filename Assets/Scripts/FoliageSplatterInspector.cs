using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FoliageSplatterTool))]
public class FoliageSplatterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FoliageSplatterTool foliageTool = (FoliageSplatterTool)target;

        if (GUILayout.Button("Generate"))
        {
            foliageTool.GenerateData();
        }
    }
}
