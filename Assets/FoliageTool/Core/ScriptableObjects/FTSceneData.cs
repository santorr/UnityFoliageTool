using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "FTSceneData", menuName = "Foliage/Scene data")]
public class FTSceneData : ScriptableObject
{
    public int ComponentSize { get; private set; } = 25;
    [SerializeField] public List<FTComponentData> ComponentsData = new List<FTComponentData>();

    public static Action<FTComponentData> OnComponentDataCreated;
    public static Action<FTComponentData> OnComponentDataUpdated;
    public static Action<string> OnComponentDataDeleted;

    /// <summary>
    /// Get neighbours components data to position
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public FTComponentData[] GetClosestComponentsData(Vector3 worldPosition, int range = 1)
    {
        Vector3 gridCoordinate = FTUtils.TransformWorldToGrid(worldPosition: worldPosition, isGridCoordinate: true);

        List<FTComponentData> result = new List<FTComponentData>();

        for (int i=0; i< ComponentsData.Count; i++)
        {
            if (MathF.Abs(ComponentsData[i].GridCoordinate.x - gridCoordinate.x) <= range && 
                MathF.Abs(ComponentsData[i].GridCoordinate.y - gridCoordinate.y) <= range && 
                MathF.Abs(ComponentsData[i].GridCoordinate.z - gridCoordinate.z) <= range)
            {
                result.Add(ComponentsData[i]);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Get a component data at position, return null if no component were found.
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public FTComponentData GetComponentDataAtPosition(Vector3 worldPosition)
    {
        Vector3 gridPosition = FTUtils.TransformWorldToGrid(worldPosition: worldPosition, isGridCoordinate: true);

        return ComponentsData.Find(component => component.GridCoordinate == gridPosition);
    }

    /// <summary>
    /// Create a new component data at position
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public FTComponentData AddComponentData(Vector3 worldPosition)
    {
        Vector3 gridCoordinate = FTUtils.TransformWorldToGrid(worldPosition: worldPosition, isGridCoordinate: true);
        FTComponentData newComponentData = new FTComponentData(gridCoordinate: gridCoordinate);
        ComponentsData.Add(newComponentData);

        OnComponentDataCreated?.Invoke(newComponentData);

        EditorUtility.SetDirty(this);

        return newComponentData;
    }

    /// <summary>
    /// Add a foliage type anywhere with matrix information
    /// </summary>
    /// <param name="foliageType"></param>
    /// <param name="matrix"></param>
    public void AddFoliage(FoliageType foliageType, Matrix4x4 matrix, bool updateVisualization = true)
    {
        FTComponentData componentData = GetComponentDataAtPosition(matrix.GetPosition()) ?? AddComponentData(matrix.GetPosition());
        FTComponentData.FoliageData foliageData = componentData.GetFoliageDataFromFoliageType(foliageType) ?? componentData.AddFoliageData(foliageType);

        foliageData.Matrices.Add(matrix);

        EditorUtility.SetDirty(this);

        if (!updateVisualization) return;

        OnComponentDataUpdated?.Invoke(componentData);

        return;
    }

    /// <summary>
    /// Remove a foliage data containing foliage type on specific component data
    /// </summary>
    /// <param name="componentData"></param>
    /// <param name="foliageType"></param>
    public void RemoveFoliageDataInComponentData(FTComponentData componentData, FoliageType foliageType)
    {
        FTComponentData.FoliageData foliageData = componentData.GetFoliageDataFromFoliageType(foliageType);

        if (foliageData == null) return;

        componentData.FoliagesData.Remove(foliageData);
        CleanComponent(componentData);
        OnComponentDataUpdated?.Invoke(componentData);

        EditorUtility.SetDirty(this);

        return;
    }

    /// <summary>
    /// Remove all foliage type from a position and radius
    /// </summary>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <param name="foliageType"></param>
    public void RemoveFoliagesInRange(Vector3 center, float radius, FoliageType foliageType)
    {
        FTComponentData[] closestComponents = GetClosestComponentsData(worldPosition: center);

        for (int i = 0; i < closestComponents.Length; i++)
        {
            FTComponentData.FoliageData foliageToRemove = closestComponents[i].GetFoliageDataFromFoliageType(foliageType);

            if (foliageToRemove == null) continue;

            for (int j = 0; j < foliageToRemove.Matrices.Count; j++)
            {
                if (Vector3.Distance(center, foliageToRemove.GetInstancePosition(j)) < radius)
                {
                    foliageToRemove.Matrices.RemoveAt(j);
                }
            }

            CleanComponent(closestComponents[i]);
            OnComponentDataUpdated?.Invoke(closestComponents[i]);
        }

        EditorUtility.SetDirty(this);

        return;
    }

    /// <summary>
    /// Remove foliage data containing foliage type for all components data
    /// </summary>
    /// <param name="foliageType"></param>
    public void RemoveFoliageData(FoliageType foliageType)
    {
        for (int i=0; i<ComponentsData.Count; i++)
        {
            FTComponentData.FoliageData foliageData = ComponentsData[i].GetFoliageDataFromFoliageType(foliageType);

            if (foliageData == null) continue;

            ComponentsData[i].FoliagesData.Remove(foliageData);
            OnComponentDataUpdated?.Invoke(ComponentsData[i]);
        }

        EditorUtility.SetDirty(this);

        return;
    }

    /// <summary>
    /// Make a clean on all components data to remover empty foliage data and empty component data
    /// </summary>
    public void CleanComponents()
    {
        ComponentsData.ForEach(componentData => CleanComponent(componentData));

        return;
    }

    /// <summary>
    /// Clean a specific component data. delete a foliage data that has no matrix, delete component data if has no foliage data.
    /// </summary>
    /// <param name="componentData"></param>
    public void CleanComponent(FTComponentData componentData)
    {
        componentData.FoliagesData.RemoveAll(foliageData => foliageData.Matrices.Count == 0);

        if (componentData.FoliagesData.Count == 0)
        {
            OnComponentDataDeleted?.Invoke(componentData.ID);
            ComponentsData.Remove(componentData);

            EditorUtility.SetDirty(this);

            return;
        }

        OnComponentDataUpdated?.Invoke(componentData);

        EditorUtility.SetDirty(this);

        return;
    }
}

[System.Serializable]
public class FTComponentData
{
    public string ID;
    public Vector3 GridCoordinate;
    public int Size = 25;
    [SerializeField] public List<FoliageData> FoliagesData = new List<FoliageData>();

    /// <summary>
    /// Get the world bounds of the component (position and size)
    /// </summary>
    public Bounds Bounds
    {
        get
        {
            return new Bounds(GridCoordinate * Size, new Vector3(Size, Size, Size));
        }
    }

    public FTComponentData(Vector3 gridCoordinate)
    {
        ID = Guid.NewGuid().ToString("N");
        GridCoordinate = gridCoordinate;
    }

    /// <summary>
    /// Get a foliage data corresponding to foliage type
    /// </summary>
    /// <param name="foliageType"></param>
    /// <returns></returns>
    public FoliageData GetFoliageDataFromFoliageType(FoliageType foliageType)
    {
        return FoliagesData.Find(foliageData => foliageData.FoliageType == foliageType);
    }

    /// <summary>
    /// Create a new foliage data based on folyage type
    /// </summary>
    /// <param name="foliageType"></param>
    /// <returns></returns>
    public FoliageData AddFoliageData(FoliageType foliageType)
    {
        FoliageData newFoliageData = new FoliageData(foliageType);
        FoliagesData.Add(newFoliageData);
        return newFoliageData;
    }


    [System.Serializable]
    public class FoliageData
    {
        public FoliageType FoliageType;
        public List<Matrix4x4> Matrices = new List<Matrix4x4>();

        public FoliageData(FoliageType foliageType)
        {
            FoliageType = foliageType;
        }

        public Vector3 GetInstancePosition(int matriceIndex)
        {
            return Matrices[matriceIndex].GetPosition();
        }
    }
}