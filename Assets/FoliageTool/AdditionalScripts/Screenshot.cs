// using UnityEditor;
using System.IO;
using UnityEngine;

public class Screenshot : MonoBehaviour
{
    [Header("Inputs")]
    public KeyCode firstInput = KeyCode.LeftControl;
    public KeyCode secondInput = KeyCode.S;
    [Header("Settings")]
    [Range(1, 5)] public int SizeMultpilier = 1;
    public string _screenshotName = "Screenshot";

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
        string path = GetUniquePath();

        if (path == null) return;

        ScreenCapture.CaptureScreenshot(filename: path, superSize: SizeMultpilier);
    }

    private string GetUniquePath()
    {
        string basePath = Application.dataPath + "/FoliageTool/Screenshots/";
        string extension = ".jpg";
        int index = 0;

        if (!Directory.Exists(basePath)) return null;

        while (File.Exists(basePath + _screenshotName + "_" + index + extension))
        {
            index++;
        }

        return basePath + _screenshotName + "_" + index + extension;
    }
}
