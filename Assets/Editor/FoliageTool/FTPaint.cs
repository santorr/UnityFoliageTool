using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

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

    public readonly FTBrush Brush = new FTBrush();
    float _paintFrequency = 0.15f;
    bool _canPaint = true;
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

    protected void OnEnable()
    {
        LoadParameters();
    }

    protected void OnDisable()
    {
        SaveParameters();
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

        _paintMode = (EPaintMode)EditorGUILayout.EnumPopup("Mode", _paintMode, GUILayout.Height(30));

        #region BRUSH
        GUILayout.Label("Brush", FTStyles.Title);
        GUILayout.Space(5);

        // Size parameter
        GUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent("Brush size", "Shift + Scroll wheel"), FTStyles.Label, GUILayout.Width(150));
        Brush.Size = (float)GUILayout.HorizontalSlider(Brush.Size, Brush.MinSize, Brush.MaxSize);
        GUILayout.Label(Brush.Size.ToString("F1"), GUILayout.Width(50));
        GUILayout.EndHorizontal();
        GUILayout.Space(5);

        // Density parameter
        GUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent("Brush density (/m2)", "Ctrl + Scroll ctrl"), FTStyles.Label, GUILayout.Width(150));
        Brush.Density = (float)GUILayout.HorizontalSlider(Brush.Density, Brush.MinDensity, Brush.MaxDensity);
        GUILayout.Label(Brush.Density.ToString("F2"), GUILayout.Width(50));
        GUILayout.EndHorizontal();
        GUILayout.Space(5);

        // Disorder parameter
        GUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent("Brush disorder", "Alt + Scroll ctrl"), FTStyles.Label, GUILayout.Width(150));
        Brush.Disorder = (float)GUILayout.HorizontalSlider(Brush.Disorder, Brush.MinDisorder, Brush.MaxDisorder);
        GUILayout.Label(Brush.Disorder.ToString("F2"), GUILayout.Width(50));
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
            Brush.Display = false;
            return;
        }

        DrawBrush();

        if (Brush.Display)
        {
            HandleSceneViewInputs();
        }
    }

    // Handle event in the scene
    private void HandleSceneViewInputs()
    {
        Event e = Event.current;
        int controlId = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

        // If scroll whell + alt = increase/decrease brush size
        if (e.type == EventType.ScrollWheel && e.shift)
        {
            e.Use();

            float delta = 0.5f;

            if (e.delta.y > 0)
            {
                Brush.Size -= delta;
            }
            else
            {
                Brush.Size += delta;
            }
            Repaint();
        }

        // If scroll whell + shift = increase/decrease brush density
        if (e.type == EventType.ScrollWheel && e.control)
        {
            e.Use();

            float delta = 0.5f;

            if (e.delta.y > 0)
            {
                Brush.Density -= delta;
            }
            else
            {
                Brush.Density += delta;
            }
            Repaint();
        }

        // If scroll whell + shift = increase/decrease brush disorder
        if (e.type == EventType.ScrollWheel && e.alt)
        {
            e.Use();

            float delta = 0.1f;

            if (e.delta.y > 0)
            {
                Brush.Disorder -= delta;
            }
            else
            {
                Brush.Disorder += delta;
            }
            Repaint();
        }

        if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && !e.alt)
        {
            if (e.button == 0)
            {
                switch (_paintMode)
                {
                    case EPaintMode.Paint:
                        if (e.shift)
                        {
                            Erase();
                            return;
                        }
                        if (_canPaint)
                        {
                            Paint();
                            PaintDelayAsync();
                        }
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
        if (SelectedFoliageType == null)
        {
            return;
        }

        _parametersSectionScrollPosition = GUILayout.BeginScrollView(_parametersSectionScrollPosition);

        #region Mesh area
        GUILayout.Label("Mesh", FTStyles.Title);
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Mesh", FTStyles.Label, GUILayout.Width(150));
        SelectedFoliageType.Mesh = (Mesh)EditorGUILayout.ObjectField(SelectedFoliageType.Mesh, typeof(Mesh), false);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Material", FTStyles.Label, GUILayout.Width(150));
        SelectedFoliageType.Material = (Material)EditorGUILayout.ObjectField(SelectedFoliageType.Material, typeof(Material), false);
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.Space(5);
        #region Painting area
        GUILayout.Label("Painting", FTStyles.Title);
        GUILayout.Space(5);
        // Splatter distance
        GUILayout.BeginHorizontal();
        GUILayout.Label("Splatter distance", FTStyles.Label, GUILayout.Width(150));
        SelectedFoliageType.SplatterDistance = EditorGUILayout.FloatField(SelectedFoliageType.SplatterDistance);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Layer mask
        GUILayout.BeginHorizontal();
        GUILayout.Label("Layer mask", FTStyles.Label, GUILayout.Width(150));
        int flags = SelectedFoliageType.LayerMask.value;
        string[] options = Enumerable.Range(0, 32).Select(index => LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l)).ToArray();
        SelectedFoliageType.LayerMask = EditorGUILayout.MaskField(flags, options);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Random scale
        GUILayout.BeginHorizontal();
        GUILayout.Label("Random scale", FTStyles.Label, GUILayout.Width(150));

        EditorGUILayout.LabelField("", SelectedFoliageType.MinimumScale.ToString("F1"), FTStyles.MinMaxLabel, GUILayout.Width(40));
        EditorGUILayout.MinMaxSlider(ref SelectedFoliageType.MinimumScale, ref SelectedFoliageType.MaximumScale, 0, 10);
        EditorGUILayout.LabelField("", SelectedFoliageType.MaximumScale.ToString("F1"), FTStyles.MinMaxLabel, GUILayout.Width(40));

        GUILayout.EndHorizontal();
        #endregion
        GUILayout.Space(5);
        #region Placement area
        GUILayout.Label("Placement", FTStyles.Title);
        GUILayout.Space(5);
        // Align to normal
        GUILayout.BeginHorizontal();
        GUILayout.Label("Align to normal", FTStyles.Label, GUILayout.Width(150));
        SelectedFoliageType.AlignToNormal = EditorGUILayout.Toggle(SelectedFoliageType.AlignToNormal);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Random rotation
        GUILayout.BeginHorizontal();
        GUILayout.Label("Random rotation", FTStyles.Label, GUILayout.Width(150));
        SelectedFoliageType.RandomRotation = EditorGUILayout.Toggle(SelectedFoliageType.RandomRotation);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Add offset
        GUILayout.BeginHorizontal();
        GUILayout.Label("Z offset", FTStyles.Label, GUILayout.Width(150));
        SelectedFoliageType.Offset = EditorGUILayout.FloatField(SelectedFoliageType.Offset);
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.Space(5);
        #region Instance settings area
        GUILayout.Label("Instance settings", FTStyles.Title);
        GUILayout.Space(5);
        // Cast shadows
        GUILayout.BeginHorizontal();
        GUILayout.Label("Cast shadows", FTStyles.Label, GUILayout.Width(150));
        SelectedFoliageType.RenderShadows = (ShadowCastingMode)EditorGUILayout.EnumFlagsField(SelectedFoliageType.RenderShadows);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Receive shadows
        GUILayout.BeginHorizontal();
        GUILayout.Label("Receive shadows", FTStyles.Label, GUILayout.Width(150));
        SelectedFoliageType.ReceiveShadows = EditorGUILayout.Toggle(SelectedFoliageType.ReceiveShadows);
        GUILayout.EndHorizontal();
        #endregion

        GUILayout.EndScrollView();
    }

    private void Paint()
    {
        Vector3[] points = Brush.GetPoints();

        // Pour chacun des points du brush on lance un rayon qui va tester les collisions
        for (int i = 0; i < points.Length; i++)
        {
            RaycastHit hit;
            Vector3 startRayPosition = points[i] + Brush.Normal * Brush.DebugLinePointSize;
            float rayDistance = Brush.DebugLinePointSize + 0.5f;

            if (Physics.Raycast(startRayPosition, Brush.InvertNormal, out hit, rayDistance, SelectedFoliageType.LayerMask))
            {
                // Scale
                Vector3 randomScale = FTUtils.RandomUniformVector3(minimum: SelectedFoliageType.MinimumScale, maximum: SelectedFoliageType.MaximumScale);

                // Rotation
                Quaternion finalRotation = Quaternion.identity;

                if (SelectedFoliageType.AlignToNormal)
                {
                    finalRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }
                if (SelectedFoliageType.RandomRotation)
                {
                    Quaternion yRotation = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up);
                    finalRotation *= yRotation;
                }

                // Position
                Vector3 position = hit.point;
                FoliageData foliageData = SceneManager.SceneData.GetFoliageDataFromId(SelectedFoliageType.GetID);

                if (foliageData == null)
                {
                    // Create a new foliage Data and add it to data container
                    FoliageData newFoliageData = new FoliageData(
                        id: SelectedFoliageType.GetID,
                        mesh: SelectedFoliageType.Mesh,
                        material: SelectedFoliageType.Material,
                        renderShadows: SelectedFoliageType.RenderShadows,
                        receiveShadows: SelectedFoliageType.ReceiveShadows
                        );
                    SceneManager.SceneData.FoliageData.Add(newFoliageData);
                    foliageData = newFoliageData;
                }

                Matrix4x4 matrice = Matrix4x4.TRS(position, finalRotation, randomScale);
                foliageData.Matrice.Add(matrice);
            }

            // Update visualisation
            SceneManager.UpdateFoliage();

            EditorUtility.SetDirty(SceneManager.SceneData);
        }
    }

    private void Erase()
    {
        FoliageData foliageToRemove = SceneManager.SceneData.GetFoliageDataFromId(SelectedFoliageType.GetID);

        for (int i = 0; i < foliageToRemove.Matrice.Count; i++)
        {
            if (Vector3.Distance(Brush.Position, foliageToRemove.Position(i)) < Brush.Radius)
            {
                foliageToRemove.Matrice.RemoveAt(i);
            }
        }
        SceneManager.UpdateFoliage();
        EditorUtility.SetDirty(SceneManager.SceneData);
    }

    private void DrawBrush()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Brush.Display = true;
            Brush.Position = hit.point;
            Brush.Normal = hit.normal.normalized;

            switch (_paintMode)
            {
                case EPaintMode.None:
                    Brush.Display = false;
                    return;
                case EPaintMode.Paint:
                    Brush.DrawCircles(colorPreset: Brush.PaintPreset);
                    Brush.DrawPoints();
                    break;
                case EPaintMode.Erase:
                    Brush.DrawCircles(colorPreset: Brush.ErasePreset);
                    break;
                default: 
                    break;
            }
        }
        else
        {
            Brush.Display = false;
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

    private FoliageType SelectedFoliageType
    {
        get { return _foliageTypes[_selectedIndex];}
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

    async void PaintDelayAsync()
    {
        _canPaint = false;
        await Task.Delay((int)(_paintFrequency * 1000));
        _canPaint = true;
    }

    private void SaveParameters()
    {
        // Save brush size
        EditorPrefs.SetFloat("BrushSize", Brush.Size);

        // Save brush density
        EditorPrefs.SetFloat("BrushDensity", Brush.Density);

        // Save brush disorder
        EditorPrefs.SetFloat("BrushDisorder", Brush.Disorder);
    }

    private void LoadParameters()
    {
        // Load brush size
        if (EditorPrefs.HasKey("BrushSize"))
        {
            Brush.Size = EditorPrefs.GetFloat("BrushSize");
        }
        else
        {
            EditorPrefs.SetFloat("BrushSize", Brush.Size);
        }
        // Load brush density
        if (EditorPrefs.HasKey("BrushDensity"))
        {
            Brush.Density = EditorPrefs.GetFloat("BrushDensity");
        }
        else
        {
            EditorPrefs.SetFloat("BrushDensity", Brush.Density);
        }
        // Load brush disorder
        if (EditorPrefs.HasKey("BrushDisorder"))
        {
            Brush.Disorder = EditorPrefs.GetFloat("BrushDisorder");
        }
        else
        {
            EditorPrefs.SetFloat("BrushDisorder", Brush.Disorder);
        }
    }
}

public class FTBrush
{
    public bool Display = false;
    public Vector3 Position;
    public Vector3 Normal;

    // Size
    private float _size = 2f;
    public float MinSize { get; private set; } = 0.5f;
    public float MaxSize { get; private set; } = 10f;

    // Density per square meter
    private float _density = 5f;
    public float MinDensity { get; private set; } = 0f;
    public float MaxDensity { get; private set; } = 20f;

    // Disorder to create some random
    private float _disorder = 0;
    public float MinDisorder { get; private set; } = 0f;
    public float MaxDisorder { get; private set; } = 2f;

    // Seed
    private int _seed = 56785;

    public float DebugLinePointSize { get; private set; } = 0.3f;

    public readonly FTBrushPreset PaintPreset;
    public readonly FTBrushPreset ErasePreset;

    public FTBrush()
    {
        PaintPreset = new FTBrushPreset(innerColor: new Color(0.27f, 0.38f, 0.49f, 0.25f), outerColor: new Color(0.27f, 0.38f, 0.49f, 1));
        ErasePreset = new FTBrushPreset(innerColor: new Color(1f, 0f, 0f, 0.25f), outerColor: new Color(1f, 0f, 0f, 1));
    }

    public float Size
    {
        get { return _size; }
        set { _size = Mathf.Clamp(value, MinSize, MaxSize); }
    }

    public float Density
    {
        get { return _density; }
        set { _density = Mathf.Clamp(value, MinDensity, MaxDensity); }
    }

    public float Disorder
    {
        get { return _disorder; }
        set { _disorder = Mathf.Clamp(value, MinDisorder, MaxDisorder); }
    }

    public int Seed
    {
        get { return _seed; }
        set { _seed = Mathf.Max(value, 0); }
    }

    public float Radius
    {
        get { return Size / 2; }
    }

    public Vector3 InvertNormal
    {
        get { return Normal * -1; }
    }

    private float BrushArea
    {
        get { return Mathf.PI * Mathf.Pow(((Size) / 2), 2); }
    }

    public void DrawCircles(FTBrushPreset colorPreset)
    {
        Handles.color = colorPreset.InnerColor;
        Handles.DrawSolidDisc(Position, Normal, Radius);
        Handles.color = colorPreset.OuterColor;
        Handles.DrawWireDisc(Position, Normal, Radius, 3f);
    }

    public void DrawPoints()
    {
        Vector3[] paintPoints = GetPoints();
        for (int i = 0; i < paintPoints.Length; i++)
        {
            Handles.DrawLine(paintPoints[i], paintPoints[i] + Normal * DebugLinePointSize, 3f);
        }
    }

    public Vector3[] GetPoints()
    {
        // Initialize the seed
        Random.InitState(Seed);

        List<Vector3> points = new List<Vector3>();

        // Calculate the number of rows and columns
        int numRows = Mathf.CeilToInt(Mathf.Sqrt(BrushArea * Density));

        // Calculate the distance between each points
        float distance = (Size) / numRows;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, Normal);

        // Create a grid of points in the brush area based on density per square metter, radius and disorder parameters
        for (int i = 0; i < numRows; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                // Randomize offset from disorder parameter
                float randomX = Random.Range(-Disorder, Disorder);
                float randomZ = Random.Range(-Disorder, Disorder);

                Vector3 pointOffset = new Vector3((i * distance + randomX) - Radius, 0f, (j * distance + randomZ) - Radius);
                Vector3 point = Position + rotation * pointOffset;

                if (Vector3.Distance(Position, point) <= Radius)
                {
                    points.Add(point);
                }
            }
        }

        // If point list is empty then add a point by default at brush position
        if (points.Count == 0)
        {
            points.Add(Position);
        }

        // Return the array of all points
        return points.ToArray();
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