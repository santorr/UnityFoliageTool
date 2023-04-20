using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FTPaint : EditorWindow
{
    // Style
    static class FTStyles
    {
        public static readonly GUIStyle Label;
        public static readonly GUIStyle MinMaxLabel;
        public static readonly GUIStyle Title;

        static FTStyles()
        {
            // Simple label
            Label = new GUIStyle();
            Label.fontSize = 12;
            Label.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            // Label.fontStyle = FontStyle.Bold;
            Label.padding = new RectOffset(5, 5, 0, 0);
            Label.alignment = TextAnchor.MiddleLeft;
            Label.wordWrap = true;

            // Min max label
            MinMaxLabel = new GUIStyle();
            MinMaxLabel.fontSize = 12;
            MinMaxLabel.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            MinMaxLabel.padding = new RectOffset(5, 5, 0, 0);
            MinMaxLabel.alignment = TextAnchor.MiddleCenter;
            MinMaxLabel.wordWrap = true;

            // Title
            Title = new GUIStyle();
            Title.normal = new GUIStyleState();
            Title.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            Title.fontStyle = FontStyle.Bold;
            Title.fontSize = 14;
            Title.padding = new RectOffset(5, 5, 5, 5);
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.17f, 0.17f, 0.17f));
            tex.Apply();
            Title.normal.background = tex;
        }
    }

    // Paint enum
    enum EPaintMode
    {
        None,
        Paint,
        Erase
    }

    FTBrush _brush = new FTBrush();
    FTSceneManager _sceneManager;
    EPaintMode _paintMode;
    int _selectedIndex;
    List<FoliageType> _foliageTypes = new List<FoliageType>();
    Vector2 _foliageTypesSectionScrollPosition;
    Vector2 _parametersSectionScrollPosition;

    // Create the window
    [MenuItem("Tools/FT Paint")]
    private static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(FTPaint));
    }

    // Select window tab || Click on tab || Open tab
    private void OnFocus()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
        SceneView.duringSceneGui += this.OnSceneGUI;

        RefreshFoliageTypes();
    }

    // Exit tab
    private void OnDestroy()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
    }

    // While tab is visible
    private void Update()
    {

    }

    // While cursor move on tab
    private void OnGUI()
    {
        // Check ici si la scene contient un un FTSceneManager
        if (FindObjectOfType<FTSceneManager>() == null)
        {
            GUILayout.Label("You need at least one FTSceneManager in the scene. You can create it manually by adding the script 'FTSceneManager' to an empty GameObject or by clicking on the button.", FTStyles.Label);

            if (GUILayout.Button("Create scene manager", GUILayout.Height(30)))
            {
                _sceneManager = CreateSceneManager();
            }
            return;
        }

        // Check ici si le FTSceneManager contient un FTSceneData
        if (FindObjectOfType<FTSceneManager>().SceneData == null)
        {
            GUILayout.Label("The 'FTSceneManager' has no input FTSceneData. It means you can't store foliage data. You can create it manually by right clicking in your content 'Create > Foliage > Data container'.\n" +
                "Or drag and drop an existing FTSceneData if you already have for this scene.", FTStyles.Label);

            _sceneManager.SceneData = (FTSceneData)EditorGUILayout.ObjectField(_sceneManager.SceneData, typeof(FTSceneData), false);

            GUILayout.Label("You can also create one by clicking on the button above. It will create a FTScene data at scene location in the content, make sure you don't have one because it will overwrite.", FTStyles.Label);
            if (GUILayout.Button("Create scene data", GUILayout.Height(30)))
            {
                _sceneManager.SceneData = CreateSceneData();
            }

            return;
        }

        GUILayout.Space(5);

        EPaintMode newPaintMode = (EPaintMode)EditorGUILayout.EnumPopup("Mode", _paintMode, GUILayout.Height(30));
        if (newPaintMode != _paintMode) { HandlePaintMode(newPaintMode); }

        #region BRUSH
        GUILayout.Label("Brush", FTStyles.Title);
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Brush size", FTStyles.Label, GUILayout.Width(150));
        _brush.Size = (float)GUILayout.HorizontalSlider(_brush.Size, 0.5f, 20f);
        GUILayout.Label(_brush.Size.ToString("F1"), GUILayout.Width(50));
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Brush density", FTStyles.Label, GUILayout.Width(150));
        _brush.Density = (float)GUILayout.HorizontalSlider(_brush.Density, 0f, 1f);
        GUILayout.Label(_brush.Density.ToString("F2"), GUILayout.Width(50));
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        #endregion

        #region FOLIAGE TYPES
        int numberColumn = 2;
        GUILayout.Label("Foliage types", FTStyles.Title);
        GUILayout.Space(5);
        _foliageTypesSectionScrollPosition = GUILayout.BeginScrollView(_foliageTypesSectionScrollPosition, GUILayout.Height(200));

        List<GUIContent> foliageTypesGUI = new List<GUIContent>();
        foreach (FoliageType foliageType in _foliageTypes)
        {
            Texture2D texture = AssetPreview.GetMiniThumbnail(foliageType);
            string name = foliageType.name;

            foliageTypesGUI.Add(new GUIContent(name, texture, "Foliage type"));
        }
        _selectedIndex = GUILayout.SelectionGrid(_selectedIndex, foliageTypesGUI.ToArray(), numberColumn, GUILayout.Height(50));

        GUILayout.EndScrollView();
        #endregion

        GUISelectedFoliageProperties();
    }

    // While tab is open
    private void OnSceneGUI(SceneView sceneView)
    {
        if (_paintMode == EPaintMode.None)
        {
            _brush.Display = false;
            return;
        }

        DrawBrush();

        if (_brush.Display)
        {
            HandleSceneViewInputs();
        }
    }

    // Handle event in the scene
    private void HandleSceneViewInputs()
    {
        Event current = Event.current;
        int controlId = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

        if ((current.type == EventType.MouseDrag || current.type == EventType.MouseDown) && !current.alt)
        {
            if (current.button == 0)
            {
                switch (_paintMode)
                {
                    case EPaintMode.Paint:
                        Paint();
                        break;
                    case EPaintMode.Erase:
                        Erase();
                        break;
                    default:
                        break;
                }
            }
        }

        if (Event.current.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(controlId);
        }

        SceneView.RepaintAll();
    }

    private void GUISelectedFoliageProperties()
    {
        // If no foliage type selected, don't draw properties
        if (_foliageTypes[_selectedIndex] == null)
        {
            return;
        }

        _parametersSectionScrollPosition = GUILayout.BeginScrollView(_parametersSectionScrollPosition);

        #region Mesh area
        GUILayout.Label("Mesh", FTStyles.Title);
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Mesh", FTStyles.Label, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].Mesh = (Mesh)EditorGUILayout.ObjectField(_foliageTypes[_selectedIndex].Mesh, typeof(Mesh), false);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Material", FTStyles.Label, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].Material = (Material)EditorGUILayout.ObjectField(_foliageTypes[_selectedIndex].Material, typeof(Material), false);
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.Space(5);
        #region Painting area
        GUILayout.Label("Painting", FTStyles.Title);
        GUILayout.Space(5);
        // Splatter distance
        GUILayout.BeginHorizontal();
        GUILayout.Label("Splatter distance", FTStyles.Label, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].SplatterDistance = EditorGUILayout.FloatField(_foliageTypes[_selectedIndex].SplatterDistance);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Layer mask
        GUILayout.BeginHorizontal();
        GUILayout.Label("Layer mask", FTStyles.Label, GUILayout.Width(150));
        int flags = _foliageTypes[_selectedIndex].LayerMask.value;
        string[] options = Enumerable.Range(0, 32).Select(index => LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l)).ToArray();
        _foliageTypes[_selectedIndex].LayerMask = EditorGUILayout.MaskField(flags, options);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Random scale
        GUILayout.BeginHorizontal();
        GUILayout.Label("Random scale", FTStyles.Label, GUILayout.Width(150));

        EditorGUILayout.LabelField("", _foliageTypes[_selectedIndex].MinimumScale.ToString("F1"), FTStyles.MinMaxLabel, GUILayout.Width(40));
        EditorGUILayout.MinMaxSlider(ref _foliageTypes[_selectedIndex].MinimumScale, ref _foliageTypes[_selectedIndex].MaximumScale, 0, 10);
        EditorGUILayout.LabelField("", _foliageTypes[_selectedIndex].MaximumScale.ToString("F1"), FTStyles.MinMaxLabel, GUILayout.Width(40));

        GUILayout.EndHorizontal();
        #endregion
        GUILayout.Space(5);
        #region Placement area
        GUILayout.Label("Placement", FTStyles.Title);
        GUILayout.Space(5);
        // Align to normal
        GUILayout.BeginHorizontal();
        GUILayout.Label("Align to normal", FTStyles.Label, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].AlignToNormal = EditorGUILayout.Toggle(_foliageTypes[_selectedIndex].AlignToNormal);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Random rotation
        GUILayout.BeginHorizontal();
        GUILayout.Label("Random rotation", FTStyles.Label, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].RandomRotation = EditorGUILayout.Toggle(_foliageTypes[_selectedIndex].RandomRotation);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Add offset
        GUILayout.BeginHorizontal();
        GUILayout.Label("Z offset", FTStyles.Label, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].Offset = EditorGUILayout.FloatField(_foliageTypes[_selectedIndex].Offset);
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.Space(5);
        #region Instance settings area
        GUILayout.Label("Instance settings", FTStyles.Title);
        GUILayout.Space(5);
        // Cast shadows
        GUILayout.BeginHorizontal();
        GUILayout.Label("Cast shadows", FTStyles.Label, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].RenderShadows = (ShadowCastingMode)EditorGUILayout.EnumFlagsField(_foliageTypes[_selectedIndex].RenderShadows);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Receive shadows
        GUILayout.BeginHorizontal();
        GUILayout.Label("Receive shadows", FTStyles.Label, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].ReceiveShadows = EditorGUILayout.Toggle(_foliageTypes[_selectedIndex].ReceiveShadows);
        GUILayout.EndHorizontal();
        #endregion

        GUILayout.EndScrollView();
    }

    private void Paint()
    {
        FoliageType currentFoliageType = _foliageTypes[_selectedIndex];

        for(float x = -_brush.Size; x< _brush.Size; x += 0.1f)
        {
            for (float z = -_brush.Size; z < _brush.Size; z += 0.1f)
            {
                float densityValue = Mathf.PerlinNoise(_brush.Position.x * 1 + x, _brush.Position.z * 1 + z);

            }
        }

        #region Calculate position
        Vector3 perpendicularDirection = Vector3.Cross(_brush.Normal, _brush.RayDirection);

        if (perpendicularDirection == Vector3.zero)
        {
            perpendicularDirection = Vector3.Cross(_brush.Normal, Vector3.up);
        }

        float randomAngle = Random.Range(0f, Mathf.PI * 2f); // Angle aléatoire
        Vector3 offset = Quaternion.AngleAxis(randomAngle * Mathf.Rad2Deg, _brush.Normal) * perpendicularDirection.normalized * Random.Range(0, _brush.Size); // Position relative de l'impact par rapport au centre
        Vector3 spawnPosition = _brush.Position + offset;
        #endregion

        #region Calcul scale
        Vector3 randomScale = FTUtils.RandomUniformVector3(minimum: currentFoliageType.MinimumScale, maximum: currentFoliageType.MaximumScale);
        #endregion

        #region Calcul rotation
        Quaternion finalRotation = Quaternion.identity;

        if (currentFoliageType.AlignToNormal)
        {
            finalRotation = Quaternion.FromToRotation(Vector3.up, _brush.Normal);
        }
        if (currentFoliageType.RandomRotation)
        {
            Quaternion yRotation = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up);
            finalRotation *= yRotation;
        }
        #endregion

        FoliageData foliageData = SceneManager.SceneData.GetFoliageDataFromId(currentFoliageType.GetID);

        if (foliageData == null)
        {
            // Create a new foliage Data and add it to data container
            FoliageData newFoliageData = new FoliageData(
                id: currentFoliageType.GetID,
                mesh: currentFoliageType.Mesh,
                material: currentFoliageType.Material,
                renderShadows: currentFoliageType.RenderShadows,
                receiveShadows: currentFoliageType.ReceiveShadows
                );
            SceneManager.SceneData.FoliageData.Add(newFoliageData);
            foliageData = newFoliageData;
        }

        Matrix4x4 matrice = Matrix4x4.TRS(spawnPosition, finalRotation, randomScale);
        foliageData.Matrice.Add(matrice);

        // Update visualisation
        SceneManager.UpdateFoliage();

        EditorUtility.SetDirty(SceneManager.SceneData);
    }

    private void Erase()
    {
        FoliageData foliageToRemove = SceneManager.SceneData.GetFoliageDataFromId(_foliageTypes[_selectedIndex].GetID);

        for (int i = 0; i < foliageToRemove.Matrice.Count; i++)
        {
            if (Vector3.Distance(_brush.Position, foliageToRemove.Position(i)) < _brush.Size)
            {
                foliageToRemove.Matrice.RemoveAt(i);
                SceneManager.UpdateFoliage();
                EditorUtility.SetDirty(SceneManager.SceneData);
            }
        }

    }

    private void DrawBrush()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            _brush.Position = hit.point;
            _brush.Normal = hit.normal.normalized;
            _brush.RayDirection = ray.direction;

            Handles.color = _brush.Preset.InnerColor;
            Handles.DrawSolidDisc(_brush.Position, _brush.Normal, _brush.Size);
            Handles.color = _brush.Preset.OuterColor;
            Handles.DrawWireDisc(_brush.Position, _brush.Normal, _brush.Size);
            
            _brush.Display = true;
        }
        else
        {
            _brush.Display = false;
        }
    }

    private void RefreshFoliageTypes()
    {
        _foliageTypes.Clear();
        foreach (string guid in AssetDatabase.FindAssets($"t: {typeof(FoliageType)}"))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            _foliageTypes.Add(AssetDatabase.LoadAssetAtPath<FoliageType>(assetPath));
        }
    }

    // On paint mode change
    private void HandlePaintMode(EPaintMode newPaintMode)
    {
        _paintMode = newPaintMode;
        switch (_paintMode)
        {
            case EPaintMode.Paint:
                _brush.Preset = _brush.PaintPreset;
                break;
            case EPaintMode.Erase:
                _brush.Preset = _brush.ErasePreset;
                break;
            default: break;
        }
    }

    // Get the foliage scene manager, if doesn't exist create a new one in the current scene
    private FTSceneManager SceneManager
    {
        get
        {
            if (_sceneManager == null) 
            { 
                _sceneManager = FindAnyObjectByType<FTSceneManager>();
                return _sceneManager;
            }
            return _sceneManager;
        }
    }

    // Create a new scene manager and return it
    private FTSceneManager CreateSceneManager()
    {
        GameObject FTManagerObject = new GameObject("FT_Manager");
        return FTManagerObject.AddComponent<FTSceneManager>();
    }

    private FTSceneData CreateSceneData()
    {
        string sceneDirectory;
        string assetName;
        string completePath;
        FTSceneData asset;
        Scene activeScene;

        asset = ScriptableObject.CreateInstance<FTSceneData>();
        activeScene = EditorSceneManager.GetActiveScene();

        assetName = "FTSceneData_" + activeScene.name + ".asset";

        try
        {
            sceneDirectory = Path.GetDirectoryName(activeScene.path);
        }
        catch (System.ArgumentException)
        {
            Debug.LogWarning("Path to the current scene not found, save your scene before generate FTSceneData.");
            return null;
        }

        completePath = Path.Combine(sceneDirectory, assetName);

        AssetDatabase.CreateAsset(asset, completePath);
        AssetDatabase.SaveAssets();

        return asset;
    }
}

public class FTBrush
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector3 RayDirection;
    public float Size = 1f;
    public float Density = 1f;
    public bool Display = false;

    public FTBrushPreset Preset;

    public readonly FTBrushPreset PaintPreset;
    public readonly FTBrushPreset ErasePreset; 

    // Constructor
    public FTBrush()
    {
        PaintPreset = new FTBrushPreset(innerColor: new Color(0.27f, 0.38f, 0.49f, 0.25f), outerColor: new Color(0.27f, 0.38f, 0.49f, 1));
        ErasePreset = new FTBrushPreset(innerColor: new Color(1f, 0f, 0f, 0.25f), outerColor: new Color(1f, 0f, 0f, 1));
        Preset = PaintPreset;
    }

    public class FTBrushPreset
    {
        public readonly Color InnerColor;
        public readonly Color OuterColor;

        public FTBrushPreset(Color innerColor, Color outerColor)
        {
            InnerColor = innerColor;
            OuterColor = outerColor;
        }
    }
}

public class CustomToggle: Toggle
{
    public CustomToggle(bool value, GUIContent content, GUIStyle style)
    {

    }
}