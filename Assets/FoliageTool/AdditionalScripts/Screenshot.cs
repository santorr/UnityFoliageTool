using UnityEditor;
using UnityEngine;

public class Screenshot : MonoBehaviour
{
    [Header("Inputs")]
    public KeyCode firstInput = KeyCode.LeftControl;
    public KeyCode secondInput = KeyCode.S;
    [Header("Settings")]
    [Range(1, 5)] public int SizeMultpilier = 1;

    private string _screenshotName = "Screenshot";

    void Update()
    {
        if (firstInput != KeyCode.None && Input.GetKey(firstInput))
        {
            if (secondInput != KeyCode.None && Input.GetKeyDown(secondInput))
            {
                SaveScreenshot();
            }
        }
    }

    private void SaveScreenshot()
    {
        string path = AssetDatabase.GenerateUniqueAssetPath(Application.dataPath + "/FoliageTool/Screenshots/" + _screenshotName + ".jpg");
        ScreenCapture.CaptureScreenshot(filename: path, superSize: SizeMultpilier);
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log("Saved screenshot.");
    }
}
