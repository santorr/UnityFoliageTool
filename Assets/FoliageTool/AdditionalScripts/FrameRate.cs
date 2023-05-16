using UnityEngine;

public class FrameRate : MonoBehaviour
{
    private float _framerate;
    private float _minimumFramerate = 999;
    private float _maximumFramerate;

    private GUIStyle _style;

    /// <summary>
    /// Initialize
    /// </summary>
    private void Awake()
    {
        Application.targetFrameRate = -1;
        SetupStyle();
        InvokeRepeating("CalculateFramerate", 5f, 0.5f);
    }

    /// <summary>
    /// Display framerate on the GUI
    /// </summary>
    private void OnGUI()
    {
        GUILayout.Label("FPS : " + _framerate.ToString(), _style);
        GUILayout.Label("MIN FPS : " + _minimumFramerate.ToString(), _style);
        GUILayout.Label("MAX FPS : " + _maximumFramerate.ToString(), _style);
    }

    /// <summary>
    /// Create label styles
    /// </summary>
    private void SetupStyle()
    {
        _style = new GUIStyle();
        _style.normal.textColor = Color.yellow;
        _style.fontSize = 30;
        _style.fontStyle = FontStyle.Bold;
    }

    /// <summary>
    /// Setup the framerate (current, min, max)
    /// </summary>
    private void CalculateFramerate()
    {
        _framerate = Mathf.Ceil(1.0f / Time.deltaTime);
        _maximumFramerate = Mathf.Max(_framerate, _maximumFramerate);
        _minimumFramerate = Mathf.Min(_framerate, _minimumFramerate);
    }
}
