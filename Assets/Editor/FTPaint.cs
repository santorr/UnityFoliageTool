using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Linq;

public enum EPaintMode
{
    Paint,
    Erase
}

public class FTPaint : EditorWindow
{
    private FTFoliageManager _foliageManager;

    private EPaintMode _paintMode;

    // Paint settings
    private Brush _brush = new Brush();
    private bool _validBrushPosition;

    // List of foliage types
    private int _selectedIndex;
    private List<FoliageType> _foliageTypes = new List<FoliageType>();

    // Scrolls
    private Vector2 _foliageTypesSectionScrollPosition;
    private Vector2 _parametersSectionScrollPosition;

    [MenuItem("Tools/FT Paint")]
    private static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(FTPaint));
    }

    private void OnGUI()
    {
        #region Setup style
        GUIStyle titleStyle = new GUIStyle();
        titleStyle.normal = new GUIStyleState();
        titleStyle.normal.textColor = new Color(0.86f, 0.86f, 0.86f);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 14;
        titleStyle.padding = new RectOffset(5, 5, 5, 5);
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, new Color(0.17f, 0.17f, 0.17f));
        tex.Apply();
        titleStyle.normal.background = tex;
        #endregion

        EPaintMode newPaintMode = (EPaintMode)EditorGUILayout.EnumPopup("Mode", _paintMode, "Button");
        if (newPaintMode != _paintMode) { HandlePaintMode(newPaintMode); }

        #region BRUSH
        GUILayout.Label("Brush", titleStyle);
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Brush size", EditorStyles.boldLabel, GUILayout.Width(150));
        _brush.Size = (float)GUILayout.HorizontalSlider(_brush.Size, 0.5f, 20f);
        GUILayout.Label(_brush.Size.ToString("F1"), GUILayout.Width(50));
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Brush density", EditorStyles.boldLabel, GUILayout.Width(150));
        _brush.Density = (float)GUILayout.HorizontalSlider(_brush.Density, 0f, 1f);
        GUILayout.Label(_brush.Density.ToString("F2"), GUILayout.Width(50));
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        #endregion

        #region FOLIAGE TYPES
        int numberColumn = 2;
        GUILayout.Label("Foliage types", titleStyle);
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

        #region INSTANCE SETTINGS
        _parametersSectionScrollPosition = GUILayout.BeginScrollView(_parametersSectionScrollPosition);
        #region Mesh area
        GUILayout.Label("Mesh", titleStyle);
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Mesh", EditorStyles.boldLabel, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].Mesh = (Mesh)EditorGUILayout.ObjectField(_foliageTypes[_selectedIndex].Mesh, typeof(Mesh), false);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Material", EditorStyles.boldLabel, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].Material = (Material)EditorGUILayout.ObjectField(_foliageTypes[_selectedIndex].Material, typeof(Material), false);
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.Space(5);
        #region Painting area
        GUILayout.Label("Painting", titleStyle);
        GUILayout.Space(5);
        // Splatter distance
        GUILayout.BeginHorizontal();
        GUILayout.Label("Splatter distance", EditorStyles.boldLabel, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].SplatterDistance = EditorGUILayout.FloatField(_foliageTypes[_selectedIndex].SplatterDistance);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Layer mask
        GUILayout.BeginHorizontal();
        GUILayout.Label("Layer mask", EditorStyles.boldLabel, GUILayout.Width(150));
        int flags = _foliageTypes[_selectedIndex].LayerMask.value;
        string[] options = Enumerable.Range(0, 32).Select(index => LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l)).ToArray();
        _foliageTypes[_selectedIndex].LayerMask = EditorGUILayout.MaskField(flags, options);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Random scale
        GUILayout.BeginHorizontal();
        GUILayout.Label("Min/Max scale", EditorStyles.boldLabel, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].MinimumScale = EditorGUILayout.FloatField(_foliageTypes[_selectedIndex].MinimumScale);
        _foliageTypes[_selectedIndex].MaximumScale = EditorGUILayout.FloatField(_foliageTypes[_selectedIndex].MaximumScale);
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.Space(5);
        #region Placement area
        GUILayout.Label("Placement", titleStyle);
        GUILayout.Space(5);
        // Align to normal
        GUILayout.BeginHorizontal();
        GUILayout.Label("Align to normal", EditorStyles.boldLabel, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].AlignToNormal = EditorGUILayout.Toggle(_foliageTypes[_selectedIndex].AlignToNormal);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Random rotation
        GUILayout.BeginHorizontal();
        GUILayout.Label("Random rotation", EditorStyles.boldLabel, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].RandomRotation = EditorGUILayout.Toggle(_foliageTypes[_selectedIndex].RandomRotation);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Add offset
        GUILayout.BeginHorizontal();
        GUILayout.Label("Z offset", EditorStyles.boldLabel, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].Offset = EditorGUILayout.FloatField(_foliageTypes[_selectedIndex].Offset);
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.Space(5);
        #region Instance settings area
        GUILayout.Label("Instance settings", titleStyle);
        GUILayout.Space(5);
        // Cast shadows
        GUILayout.BeginHorizontal();
        GUILayout.Label("Cast shadows", EditorStyles.boldLabel, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].RenderShadows = (ShadowCastingMode)EditorGUILayout.EnumFlagsField(_foliageTypes[_selectedIndex].RenderShadows);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        // Receive shadows
        GUILayout.BeginHorizontal();
        GUILayout.Label("Receive shadows", EditorStyles.boldLabel, GUILayout.Width(150));
        _foliageTypes[_selectedIndex].ReceiveShadows = EditorGUILayout.Toggle(_foliageTypes[_selectedIndex].ReceiveShadows);
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.EndScrollView();
        #endregion
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        DisplayBrushGizmos();
        if (_validBrushPosition)
        {
            HandleSceneViewInputs();
        }
    }

    private void HandleSceneViewInputs()
    {
        Event current = Event.current;
        int controlId = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

        #region PAINT
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
        #endregion

        if (Event.current.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(controlId);
        }
        SceneView.RepaintAll();
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
        Vector3 randomScale = RandomUniformScale(minimum: currentFoliageType.MinimumScale, maximum: currentFoliageType.MaximumScale);
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

        FoliageData foliageData = FoliageManager.DataContainer.GetFoliageDataFromId(currentFoliageType.GetID);

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
            FoliageManager.DataContainer.FoliageData.Add(newFoliageData);
            foliageData = newFoliageData;
        }

        Matrix4x4 matrice = Matrix4x4.TRS(spawnPosition, finalRotation, randomScale);
        foliageData.Matrice.Add(matrice);

        // Update visualisation
        FoliageManager.UpdateFoliage();

        EditorUtility.SetDirty(FoliageManager.DataContainer);
    }

    // The foliage manager contain Foliage data container and is able to show foliage data
    private FTFoliageManager FoliageManager
    {
        get
        {
            _foliageManager = (FTFoliageManager)FindObjectOfType(typeof(FTFoliageManager));
            if (_foliageManager == null)
            {
                GameObject foliageManager = new GameObject("FT_FoliageManager");
                FTFoliageManager test = foliageManager.AddComponent<FTFoliageManager>();
                _foliageManager = test;
            }
            return _foliageManager;
        }
    }

    private void Erase()
    {
        FoliageData foliageToRemove = FoliageManager.DataContainer.GetFoliageDataFromId(_foliageTypes[_selectedIndex].GetID);

        for (int i = 0; i < foliageToRemove.Matrice.Count; i++)
        {
            if (Vector3.Distance(_brush.Position, foliageToRemove.Position(i)) < _brush.Size)
            {
                foliageToRemove.Matrice.RemoveAt(i);
                FoliageManager.UpdateFoliage();
                EditorUtility.SetDirty(FoliageManager.DataContainer);
            }
        }

    }

    void OnFocus()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
        SceneView.duringSceneGui += this.OnSceneGUI;

        RefreshFoliageTypes();
        Debug.Log(_paintMode);
        // HandlePaintMode(EPaintMode.Paint);
    }

    void OnDestroy()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
    }

    private void DisplayBrushGizmos()
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
            
            _validBrushPosition = true;
        }
        else
        {
            _validBrushPosition = false;
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

    private Vector3 RandomUniformScale(float minimum, float maximum)
    {
        float randomValue = Random.Range(minimum, maximum);
        return new Vector3(randomValue, randomValue, randomValue);
    }

    private Vector3 RandomNonUniformScale(float minimum, float maximum)
    {
        float randomX = Random.Range(minimum, maximum);
        float randomY = Random.Range(minimum, maximum);
        float randomZ = Random.Range(minimum, maximum);
        return new Vector3(randomX, randomY, randomZ);
    }

    private void HandlePaintMode(EPaintMode newPaintMode)
    {
        _paintMode = newPaintMode;
        switch (_paintMode)
        {
            case EPaintMode.Paint:
                _brush.Preset = new BrushPreset(innerColor: new Color(0.27f, 0.38f, 0.49f, 0.25f), outerColor: new Color(0.27f, 0.38f, 0.49f, 1));
                break;
            case EPaintMode.Erase:
                _brush.Preset = new BrushPreset(innerColor: new Color(1f, 0f, 0f, 0.25f), outerColor: new Color(1f, 0f, 0f, 1));
                break;
            default: break;
        }
    }
}

public class Brush
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector3 RayDirection;
    public float Size = 1f;
    public float Density = 1f;

    public BrushPreset Preset = new BrushPreset(innerColor: Color.white, outerColor: Color.white);
}

public class BrushPreset
{
    public Color InnerColor;
    public Color OuterColor;

    public BrushPreset(Color innerColor, Color outerColor)
    {
        InnerColor = innerColor;
        OuterColor = outerColor;
    }
}

public class LayerMaskField
{
    public static LayerMask CustomLayerMaskField(GUIContent label, LayerMask layerMask)
    {
        List<string> layerNames = new List<string>();
        List<int> layerValues = new List<int>();

        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
            {
                layerNames.Add(layerName);
                layerValues.Add(i);
            }
        }

        layerNames.Insert(0, "Nothing");
        layerValues.Insert(0, 0);

        layerNames.Insert(1, "Everything");
        layerValues.Insert(1, -1);

        int selectedValue = 0;

        for (int i = 0; i < layerValues.Count; i++)
        {
            if ((layerMask.value & (1 << layerValues[i])) != 0)
            {
                selectedValue |= 1 << i;
            }
        }

        selectedValue = EditorGUILayout.MaskField(label, selectedValue, layerNames.ToArray());

        int newValue = 0;

        for (int i = 0; i < layerValues.Count; i++)
        {
            if ((selectedValue & (1 << i)) != 0)
            {
                newValue |= 1 << layerValues[i];
            }
        }

        layerMask.value = newValue;

        return layerMask;
    }
}