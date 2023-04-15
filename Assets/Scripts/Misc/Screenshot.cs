using UnityEngine;

public class Screenshot : MonoBehaviour
{
    public string ScreenshotName = "Screen";
    [Range(1, 5)] public int SizeMultpilier = 1;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            string path = Application.dataPath + "/Screenshots/" + ScreenshotName + ".jpg";
            ScreenCapture.CaptureScreenshot(filename: path, superSize: SizeMultpilier);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("Saved screenshot.");
        }
    }
}
