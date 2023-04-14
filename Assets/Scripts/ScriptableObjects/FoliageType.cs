using UnityEngine;

/// <summary>
/// This class allow you to setup all parameters to procedurally generate foliage.
/// </summary>
[CreateAssetMenu(fileName = "FoliageType", menuName = "Foliage/Foliage Type", order = 1)]
public class FoliageType : ScriptableObject
{
    [Header("Base")]
    public Mesh Mesh;
    public Material Material;
    public LayerMask LayerMask;

    [Header("Rendering")]
    public UnityEngine.Rendering.ShadowCastingMode RenderShadows;
    public bool ReceiveShadows = true;

    [Header("Rotation")]
    public bool AlignToNormal = false;
    public bool RandomRotation = true;

    [Header("Position")]
    public float Offset = 0;

    [Header("Scale")]
    [Range(0, 5)] public float MinimumScale = 0.75f;
    [Range(0, 5)] public float MaximumScale = 1f;

    [Header("Settings")]
    [Min(0)] public float SplatterDistance = 1f;
    [Min(0)] public float RandomizeDistance = 1f;
}