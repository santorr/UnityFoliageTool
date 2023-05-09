using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Unity.VisualScripting;

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
            Label.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 1f);
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
    public enum EPaintMode
    {
        None,
        Paint,
        Fill,
        Replace
    }
 
    public readonly FTBrush Brush = new FTBrush();
    public float PaintFrequency { get; private set; } = 0.15f;
    public bool CanPaint { get; private set; } = true;
    public int SelectedIndex { get; private set; }
    public FoliageType[] FoliageTypes { get; private set; }
    public EPaintMode PaintMode { get; private set; }
    public bool InvertPaintMode { get; private set; }
    public Vector2 FoliageTypesScrollPosition { get; private set; }
    public Vector2 ParametersScrollPosition { get; private set; }
    public FTComponentData[] activeComponents { get; private set; }

    FTManager _componentsManager;

    private FTManager ComponentsManager
    {
        get
        {
            if (_componentsManager == null)
            {
                _componentsManager = FindAnyObjectByType<FTManager>();
                return _componentsManager;
            }
            return _componentsManager;
        }
    }

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

    private void OnEnable()
    {
        LoadParameters();
    }

    private void OnDisable()
    {
        SaveParameters();
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
        if (FindObjectOfType<FTManager>() == null)
        {
            GUILayout.Label("You need at least one FTSceneManager in the scene. You can create it manually by adding the script 'FTSceneManager' to an empty GameObject or by clicking on the button.", FTStyles.Label);

            if (GUILayout.Button("Create scene manager", GUILayout.Height(30)))
            {
                _componentsManager = CreateComponentsManager();
            }
            return;
        }

        // Check ici si le FTSceneManager contient un FTSceneData
        if (FindObjectOfType<FTManager>().SceneData == null)
        {
            GUILayout.Label("The 'FTSceneManager' has no input FTSceneData. It means you can't store foliage data. You can create it manually by right clicking in your content 'Create > Foliage > Data container'.\n" +
                "Or drag and drop an existing FTSceneData if you already have for this scene.", FTStyles.Label);

            _componentsManager.SceneData = (FTSceneData)EditorGUILayout.ObjectField(_componentsManager.SceneData, typeof(FTSceneData), false);

            GUILayout.Label("You can also create one by clicking on the button above. It will create a FTScene data at scene location in the content, make sure you don't have one because it will overwrite.", FTStyles.Label);
            if (GUILayout.Button("Create scene data", GUILayout.Height(30)))
            {
                _componentsManager.SceneData = CreateSceneData();
            }

            return;
        }

        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Mode", FTStyles.Label, GUILayout.Width(150));
        PaintMode = (EPaintMode)GUILayout.Toolbar((int)PaintMode, EPaintMode.GetNames(typeof(EPaintMode)));
        GUILayout.EndHorizontal(); 

        GUILayout.Space(5);

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
        #endregion

        #region FOLIAGE TYPES
        int numberColumn = 2;
        GUILayout.Label("Foliage types", FTStyles.Title);
        GUILayout.Space(5);
        FoliageTypesScrollPosition = GUILayout.BeginScrollView(FoliageTypesScrollPosition, GUILayout.Height(200));

        List<GUIContent> foliageTypesGUI = new List<GUIContent>();
        foreach (FoliageType foliageType in FoliageTypes)
        {
            Texture2D texture = AssetPreview.GetMiniThumbnail(foliageType);
            string name = foliageType.name;

            GUIContent content = new GUIContent(name, texture, "Foliage type");

            foliageTypesGUI.Add(content);
        }
        SelectedIndex = GUILayout.SelectionGrid(SelectedIndex, foliageTypesGUI.ToArray(), numberColumn, GUILayout.Height(Mathf.CeilToInt(foliageTypesGUI.Count/2f) * 50f));

        GUILayout.EndScrollView();
        #endregion

        GUISelectedFoliageProperties();
    }

    // While tab is open
    private void OnSceneGUI(SceneView sceneView)
    {
        if (PaintMode == EPaintMode.None)
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

            float delta = Mathf.Sign(e.delta.y) * -0.5f;

            Brush.Size += delta;

            Repaint();
        }

        // If Paint or erase mode
        if (PaintMode == EPaintMode.Paint)
        {


            InvertPaintMode = e.shift ? true : false;

            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && !e.alt && e.button == 0)
            {
                // Erase
                if (InvertPaintMode)
                {
                    Erase();
                }
                else
                {
                    if (CanPaint)
                    {
                        Paint();
                        PaintDelayAsync();
                    }
                }
            }
        }

        if (PaintMode == EPaintMode.Fill)
        {
            InvertPaintMode = e.shift ? true : false;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (InvertPaintMode)
                {
                    EraseFill();
                }
                else
                {
                    Fill();
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

        ParametersScrollPosition = GUILayout.BeginScrollView(ParametersScrollPosition);

        #region Mesh area
        GUILayout.Label("Mesh", FTStyles.Title);
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Prefab", FTStyles.Label, GUILayout.Width(150));
        SelectedFoliageType.Prefab = (GameObject)EditorGUILayout.ObjectField(SelectedFoliageType.Prefab, typeof(GameObject), false);
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.Space(5);
        #region Painting area
        GUILayout.Label("Painting", FTStyles.Title);
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
        GUILayout.Space(5);
        // Density per square meter
        GUILayout.BeginHorizontal();
        SelectedFoliageType.Density = EditorGUILayout.FloatField("Density (/m2)", SelectedFoliageType.Density);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Disorder
        GUILayout.BeginHorizontal();
        SelectedFoliageType.Disorder = EditorGUILayout.FloatField("Disorder", SelectedFoliageType.Disorder);
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
        SelectedFoliageType.Offset = EditorGUILayout.FloatField("Z offset", SelectedFoliageType.Offset);
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

        EditorUtility.SetDirty(SelectedFoliageType);
    }

    private void Paint()
    {
        Vector3[] points = Brush.GetPoints(density: SelectedFoliageType.Density, disorder: SelectedFoliageType.Disorder);

        // Pour chacun des points du brush on lance un rayon qui va tester les collisions
        for (int i = 0; i < points.Length; i++)
        {
            RaycastHit hit;
            Vector3 startRayPosition = points[i] + Brush.Normal * Brush.DebugLinePointSize;
            float rayDistance = Brush.DebugLinePointSize + 0.5f;

            if (Physics.Raycast(startRayPosition, Brush.InvertNormal, out hit, rayDistance, SelectedFoliageType.LayerMask))
            {
                // Create the instance matrix
                Matrix4x4 matrice = CalculateMatrix(position: hit.point, normal: hit.normal);

                ComponentsManager.SceneData.AddFoliage(SelectedFoliageType, matrice);
            }

            EditorUtility.SetDirty(ComponentsManager.SceneData);
        }
    }

    // Find the SelectedFoliageType from his ID in the FoliageData and loop over all matrix to compare distance from brush center, remove them if distance is less than radius
    private void Erase()
    {
        // Get all components around position
        FTComponentData[] components = ComponentsManager.SceneData.GetClosestComponentsData(Brush.Position);

        // Loop over all components
        for (int i=0; i< components.Length; i++)
        {
            // Get the foliage data in the current component
            FTComponentData.FoliageData foliageToRemove = components[i].GetFoliageDataFromFoliageType(SelectedFoliageType);

            if (foliageToRemove == null) continue;

            for (int j = 0; j < foliageToRemove.Matrices.Count; j++)
            {
                if (Vector3.Distance(Brush.Position, foliageToRemove.GetInstancePosition(j)) < Brush.Radius)
                {
                    foliageToRemove.Matrices.RemoveAt(j);
                }
            }

            ComponentsManager.SceneData.CleanComponent(components[i]);
            ComponentsManager.UpdateComponent(components[i]);
        }

        EditorUtility.SetDirty(ComponentsManager.SceneData);
    }

    // ...
    private void Fill()
    {
        // Check component at position
        FTComponentData activeComponent = ComponentsManager.SceneData.GetComponentDataAtPosition(Brush.Position);

        if (activeComponent == null)
        {
            activeComponent = ComponentsManager.SceneData.AddComponentData(Brush.Position);
        }

        // On lance des rayon en grille dans le component pour faire spawn le foliage
        int numRows = Mathf.CeilToInt(Mathf.Sqrt((25f * 25f) * SelectedFoliageType.Density));
        // Calculate the distance between each points
        float distance = (25f) / numRows;
        // Create a grid of points in the brush area based on density per square metter, radius and disorder parameters
        for (int i = 0; i < numRows; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                // Start ray position
                float randomX = Random.Range(-SelectedFoliageType.Disorder, SelectedFoliageType.Disorder);
                float randomZ = Random.Range(-SelectedFoliageType.Disorder, SelectedFoliageType.Disorder);

                Vector3 pointOffset = new Vector3((i * distance + randomX) - 12.5f, 12.5f, (j * distance + randomZ) - 12.5f);
                Vector3 point = activeComponent.Bounds.center + pointOffset;

                RaycastHit hit;
                if (Physics.Raycast(point, Vector3.down, out hit, Mathf.Infinity, SelectedFoliageType.LayerMask))
                {
                    if (activeComponent.Bounds.Contains(hit.point))
                    {
                        Matrix4x4 matrice = CalculateMatrix(hit.point, hit.normal);
                        ComponentsManager.SceneData.AddFoliage(SelectedFoliageType, matrice);
                    }
                }
            }
        }
        EditorUtility.SetDirty(ComponentsManager.SceneData);
    }

    private void EraseFill()
    {
        // Check component at position
        FTComponentData activeComponent = ComponentsManager.SceneData.GetComponentDataAtPosition(Brush.Position);

        if (activeComponent == null) return;

        ComponentsManager.SceneData.RemoveFoliageDataInComponentData(componentData: activeComponent, SelectedFoliageType);
        EditorUtility.SetDirty(ComponentsManager.SceneData);
    }

    private Matrix4x4 CalculateMatrix(Vector3 position, Vector3 normal)
    {
        // Calculate scale based on perlin noise
        float noiseScale = 1f;
        float noiseValue = Mathf.PerlinNoise(position.x * noiseScale, position.z * noiseScale);
        float scale = FTUtils.Remap(noiseValue, 0, 1, SelectedFoliageType.MinimumScale, SelectedFoliageType.MaximumScale);
        Vector3 randomScale = Vector3.one * scale;

        // Rotation
        Quaternion finalRotation = Quaternion.identity;
        if (SelectedFoliageType.AlignToNormal)
        {
            finalRotation = Quaternion.FromToRotation(Vector3.up, normal);
        }
        if (SelectedFoliageType.RandomRotation)
        {
            Quaternion yRotation = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up);
            finalRotation *= yRotation;
        }

        return Matrix4x4.TRS(position, finalRotation, randomScale);
    }

    // Draw the brush in the scene depending on _paintMode and SelectedFoliageType LayerMask
    private void DrawBrush()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, SelectedFoliageType.LayerMask))
        {
            Brush.Display = true;
            Brush.Position = hit.point;
            Brush.Normal = hit.normal.normalized;

            switch (PaintMode)
            {
                case EPaintMode.None:
                    Brush.Display = false;
                    return;
                case EPaintMode.Paint:
                    if (InvertPaintMode)
                    {
                        Brush.DrawCircles(colorPreset: Brush.ErasePreset);
                    }
                    else
                    {
                        Brush.DrawCircles(colorPreset: Brush.PaintPreset);
                    }
                    break;
                case EPaintMode.Fill:
                    int componentSize = ComponentsManager.SceneData.ComponentSize;
                    Vector3 position = FTUtils.TransformWorldToGrid(Brush.Position);

                    if (InvertPaintMode)
                    {
                        Brush.DrawCube(position: position, size: new Vector3(componentSize, 0, componentSize), colorPreset: Brush.ErasePreset);
                    }
                    else
                    {
                        Brush.DrawCube(position: position, size: new Vector3(componentSize, 0, componentSize), colorPreset: Brush.PaintPreset);
                    }
                    
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

    // Get all FoliageType from the project and fill the _foliageTypes with FoliageType assets
    private void RefreshFoliageTypes()
    {
        string[] projectFoliageTypesGuid = AssetDatabase.FindAssets($"t: {typeof(FoliageType)}");

        FoliageTypes = new FoliageType[projectFoliageTypesGuid.Length];

        for (int i=0; i< projectFoliageTypesGuid.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(projectFoliageTypesGuid[i]);
            FoliageTypes[i] = AssetDatabase.LoadAssetAtPath<FoliageType>(assetPath);
        }
    }

    // Get the selected foliage, return null if not valid
    private FoliageType SelectedFoliageType
    {
        get 
        {
            if (FoliageTypes.Length >= SelectedIndex)
            {
                return FoliageTypes[SelectedIndex];
            }
            return null;
        }
    }

    // Create and return a FTSceneManager in the current scene that is able to store a FTSceneManager and display data
    private FTManager CreateComponentsManager()
    {
        GameObject FTManagerObject = new GameObject("FT_Manager");
        return FTManagerObject.AddComponent<FTManager>();
    }

    // Create and return a FTSceneData asset at scene content location that contains all scene foliage data
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

    // Create timer between each paint
    async void PaintDelayAsync()
    {
        CanPaint = false;
        await Task.Delay((int)(PaintFrequency * 1000));
        CanPaint = true;
    }

    // Save parameters on disable tool
    private void SaveParameters()
    {
        // Save brush size
        EditorPrefs.SetFloat("BrushSize", Brush.Size);
    }

    // Load parameters on enable tool
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

    public float DebugLinePointSize { get; private set; } = 0.3f;

    public readonly FTBrushPreset PaintPreset;
    public readonly FTBrushPreset ErasePreset;

    // Constructor
    public FTBrush()
    {
        PaintPreset = new FTBrushPreset(color: new Color(0f, 0.75f, 1f, 1f));
        ErasePreset = new FTBrushPreset(color: new Color(1f, 0f, 0f, 1f));
    }

    // Getter | Setter : Size
    public float Size
    {
        get { return _size; }
        set { _size = Mathf.Clamp(value, MinSize, MaxSize); }
    }

    // Getter | Setter : Radius
    public float Radius
    {
        get { return Size / 2; }
    }

    // Getter | Setter : InvertNormal (Raydirection)
    public Vector3 InvertNormal
    {
        get { return Normal * -1; }
    }

    // Getter | Setter : BrushArea
    private float BrushArea
    {
        get { return Mathf.PI * Mathf.Pow(((Size) / 2), 2); }
    }

    // Draw a circle at brush position based on color preset and radius
    public void DrawCircles(FTBrushPreset colorPreset)
    {
        Handles.color = colorPreset.Color * new Color(1f, 1f, 1f, 0.25f);
        Handles.DrawSolidDisc(Position, Normal, Radius);
        Handles.color = colorPreset.Color;
        Handles.DrawWireDisc(Position, Normal, Radius, 3f);
    }

    public void DrawCube(Vector3 position, Vector3 size, FTBrushPreset colorPreset)
    {
        Handles.color = colorPreset.Color;
        // Handles.DrawWireCube(position, size);

        Vector3 normal = Vector3.up;
        Vector3 right = Vector3.right * (size.x / 2);
        Vector3 forward = Vector3.forward * (size.z / 2);
        Vector3 upperLeft = position - right - forward;
        Vector3 upperRight = position + right - forward;
        Vector3 lowerLeft = position - right + forward;
        Vector3 lowerRight = position + right + forward;

        Handles.DrawAAPolyLine(5, new Vector3[] { upperLeft, upperRight });
        Handles.DrawAAPolyLine(5, new Vector3[] { upperRight, lowerRight });
        Handles.DrawAAPolyLine(5, new Vector3[] { lowerRight, lowerLeft });
        Handles.DrawAAPolyLine(5, new Vector3[] { lowerLeft, upperLeft });

        Handles.DrawSolidRectangleWithOutline(new Vector3[] { upperLeft, upperRight, lowerRight, lowerLeft }, colorPreset.Color * new Color(1f, 1f, 1f, 0.25f), new Color(1f, 1f, 1f, 0f));


    }

    // A sunflower algorythm to draw lines for foliage types spawning
    private Vector3[] SunflowerAlgorythm(float density, float disorder)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, Normal);

        int pointNumbers = (int)(BrushArea * density);
        Vector3[] points = new Vector3[pointNumbers];

        float alpha = 2f;
        int b = Mathf.RoundToInt(alpha * Mathf.Sqrt(pointNumbers));
        float phi = (Mathf.Sqrt(5f) + 1f) / 2f;

        for (int i = 0; i < pointNumbers; i++)
        {
            float randomX = Random.Range(-disorder, disorder);
            float randomZ = Random.Range(-disorder, disorder);

            float r = SunFlowerRadius(i, pointNumbers, b);
            float theta = 2f * Mathf.PI * i / Mathf.Pow(phi, 2f);
            Vector3 pointOffset = new Vector3(r * Mathf.Cos(theta) + randomX, 0f, r * Mathf.Sin(theta) + randomZ) * Radius;

            Vector3 point = Position + rotation * pointOffset;
            points[i] = point;
        }

        if (points.Length <= 1)
        {
            points = new Vector3[1];
            points[0] = Position;
        }

        return points;
    }

    // A grid algorythm to draw lines for foliage types spawning
    private Vector3[] GridAlgorythm(float density, float disorder)
    {
        List<Vector3> points = new List<Vector3>();
        // Calculate the number of rows and columns
        int numRows = Mathf.CeilToInt(Mathf.Sqrt(BrushArea * density));
        // Calculate the distance between each points
        float distance = (Size) / numRows;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, Normal);
        // Create a grid of points in the brush area based on density per square metter, radius and disorder parameters
        for (int i = 0; i < numRows; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                // Randomize offset from disorder parameter
                float randomX = Random.Range(-disorder, disorder);
                float randomZ = Random.Range(-disorder, disorder);

                Vector3 pointOffset = new Vector3((i * distance + randomX) - Radius, 0f, (j * distance + randomZ) - Radius);
                Vector3 point = Position + rotation * pointOffset;

                if (Vector3.Distance(Position, point) <= Radius)
                {
                    points.Add(point);
                }
            }
        }

        if (points.Count == 0)
        {
            points.Add(Position);
        }

        // Return the array of all points
        return points.ToArray();
    }

    private float SunFlowerRadius(int pointIndex, int pointNumbers, int b)
    {
        if (pointIndex > pointNumbers - b)
        {
            return 1f;
        }
        else
        {
            return Mathf.Sqrt(pointIndex - 0.5f) / Mathf.Sqrt(pointNumbers - (b + 0.5f));
        }
    }

    public Vector3[] GetPoints(float density, float disorder)
    {
        return SunflowerAlgorythm(density: density, disorder: disorder);
        // return GridAlgorythm();
    }

    public class FTBrushPreset
    {
        public readonly Color Color;

        public FTBrushPreset(Color color)
        {
            Color = color;
        }
    }
}