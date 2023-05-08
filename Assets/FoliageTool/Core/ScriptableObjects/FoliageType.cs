using UnityEditor;
using UnityEngine;

/// <summary>
/// This class allow you to setup all parameters to procedurally generate foliage.
/// </summary>
[CreateAssetMenu(fileName = "FoliageType", menuName = "Foliage/Foliage Type", order = 1)]
public class FoliageType : ScriptableObject
{
    [Header("Base")]
    public GameObject Prefab;
    public LayerMask LayerMask;

    [Header("Rendering")]
    [Range(0, 100)] public int CullingDistance = 100;
    public UnityEngine.Rendering.ShadowCastingMode RenderShadows;
    public bool ReceiveShadows = true;

    [Header("Rotation")]
    public bool AlignToNormal = false;
    public bool RandomRotation = true;

    [Header("Painting")]
    [Min(0)] public float DensityPerSquareMeter = 5.5f;
    [Min(0)] public float Disorder = 0.1f;
    
    [Header("Position")]
    public float Offset = 0;

    [Header("Scale")]
    public float MinimumScale = 0.75f;
    public float MaximumScale = 1f;

    public string GetID
    {
        get
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out string guid, out long localId))
            {
                return guid;
            }
            else
            {
                return null;
            }
        }
    }

    // Return mesh of Prefab object
    public Mesh Mesh
    {
        get
        {
            if (Prefab != null)
            {
                if (Prefab.GetComponent<MeshFilter>() != null)
                {
                    return Prefab.GetComponent<MeshFilter>().sharedMesh;
                }
            }
            return null;
        }
    }

    // Return array of materials of Prefab object
    public Material[] Materials
    {
        get
        {
            if (Prefab != null)
            {
                if (Prefab.GetComponent<MeshRenderer>() != null)
                {
                    return Prefab.GetComponent<MeshRenderer>().sharedMaterials;
                }
            }
            return null;
        }
    }
}