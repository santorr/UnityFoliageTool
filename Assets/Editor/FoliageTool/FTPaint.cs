using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using System;

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
            Title = new GUIStyle(GUI.skin.box);
            Title.alignment = TextAnchor.MiddleLeft;
            Title.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            Title.fontStyle = FontStyle.Bold;
            Title.fontSize = 14;
            Title.padding = new RectOffset(5, 5, 5, 5);
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

            ComponentsManager.SceneData = (FTSceneData)EditorGUILayout.ObjectField(_componentsManager.SceneData, typeof(FTSceneData), false);

            GUILayout.Label("You can also create one by clicking on the button above. It will create a FTScene data at scene location in the content, make sure you don't have one because it will overwrite.", FTStyles.Label);
            if (GUILayout.Button("Create scene data", GUILayout.Height(30)))
            {
                ComponentsManager.SceneData = CreateSceneData();
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
        GUILayout.Box("Brush", FTStyles.Title, GUILayout.ExpandWidth(true));
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
        GUILayout.Box("Foliage types", FTStyles.Title, GUILayout.ExpandWidth(true));
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
        SelectedIndex = GUILayout.SelectionGrid(SelectedIndex, foliageTypesGUI.ToArray(), numberColumn, GUILayout.Height(Mathf.CeilToInt(foliageTypesGUI.Count/ (float)numberColumn) * 50f));

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
        if (SelectedFoliageType == null) return;

        ParametersScrollPosition = GUILayout.BeginScrollView(ParametersScrollPosition);

        #region Mesh area
        GUILayout.Box("Mesh", FTStyles.Title, GUILayout.ExpandWidth(true));
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Prefab", FTStyles.Label, GUILayout.Width(150));
        SelectedFoliageType.Prefab = (GameObject)EditorGUILayout.ObjectField(SelectedFoliageType.Prefab, typeof(GameObject), false);
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.Space(5);
        #region Painting area
        GUILayout.Box("Painting", FTStyles.Title, GUILayout.ExpandWidth(true));
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
        GUILayout.Box("Placement", FTStyles.Title, GUILayout.ExpandWidth(true));
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
        GUILayout.Box("Instance settings", FTStyles.Title, GUILayout.ExpandWidth(true));
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
            Vector3 startRayPosition = points[i] + Brush.Normal;
            float rayDistance = 2f;

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
        if (ComponentsManager == null || ComponentsManager.SceneData == null) return;

        ComponentsManager.SceneData.RemoveFoliagesInRange(center: Brush.Position, radius: Brush.Radius, foliageType: SelectedFoliageType);

        return;
    }

    // ...
    private void Fill()
    {
        if (ComponentsManager == null || ComponentsManager.SceneData == null) return;

        FTComponentData activeComponent = ComponentsManager.SceneData.GetComponentDataAtPosition(Brush.Position);

        if (activeComponent == null)
        {
            activeComponent = ComponentsManager.SceneData.AddComponentData(Brush.Position);
        }

        Vector3[] gridPoints = FTUtils.GetGridPoints(
            bounds: activeComponent.Bounds, 
            density: SelectedFoliageType.Density, 
            disorder: SelectedFoliageType.Disorder,
            keepOutOfZone: false
            );

        if (gridPoints.Length == 0) return;

        NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(gridPoints.Length, Allocator.TempJob);
        NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(gridPoints.Length, Allocator.TempJob);

        QueryParameters parameters = new QueryParameters(layerMask: SelectedFoliageType.LayerMask);

        for (int i=0; i<gridPoints.Length; i++)
        {
            commands[i] = new RaycastCommand(from: gridPoints[i], direction: Vector3.down, queryParameters: parameters);
        }
        
        JobHandle jobHandle = RaycastCommand.ScheduleBatch(commands: commands, results: results, minCommandsPerJob: results.Length);
        jobHandle.Complete();

        RaycastHit[] hits = results.Where(hit => hit.collider != null).ToArray();

        results.Dispose();
        commands.Dispose();

        for (int i=0; i< hits.Length; i++)
        {
            Matrix4x4 matrice = CalculateMatrix(hits[i].point, hits[i].normal);
            ComponentsManager.SceneData.AddFoliage(SelectedFoliageType, matrice, updateVisualization: i == hits.Length-1);

        }



        return;
    }

    private void EraseFill()
    {
        // Check component at position
        FTComponentData activeComponent = ComponentsManager.SceneData.GetComponentDataAtPosition(Brush.Position);

        if (activeComponent == null) return;

        ComponentsManager.SceneData.RemoveFoliageDataInComponentData(componentData: activeComponent, SelectedFoliageType);

        return;
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
            Quaternion yRotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up);
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
                        Brush.DrawSquare(position: position, size: new Vector3(componentSize, 0, componentSize), colorPreset: Brush.ErasePreset);
                    }
                    else
                    {
                        Brush.DrawSquare(position: position, size: new Vector3(componentSize, 0, componentSize), colorPreset: Brush.PaintPreset);
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