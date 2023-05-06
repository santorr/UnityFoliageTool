using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FTSceneData", menuName = "Foliage/Scene data")]
public class FTSceneData : ScriptableObject
{
    [SerializeField] public int ComponentSize = 25;
    [SerializeField] public List<FTComponentData> ComponentsData = new List<FTComponentData>();

    public static Action<FTComponentData> OnComponentDataCreated;
    public static Action<FTComponentData> OnComponentDataUpdated;
    public static Action<string> OnComponentDataDeleted;

    public FTComponentData[] GetClosestComponentsData(Vector3 worldPosition)
    {
        // 0, 1 ,0
        Vector3 gridPosition = FTUtils.TransformWorldToGrid(worldPosition) / 25f;

        List<FTComponentData> result = new List<FTComponentData>();

        // L'objectif est d'avoir les composants qui sont autour du composant actuel
        for (int i=0; i< ComponentsData.Count; i++)
        {
            Vector3 testPosition = FTUtils.TransformWorldToGrid(ComponentsData[i].ComponentPosition) / 25f;

            if (MathF.Abs(testPosition.x - gridPosition.x) <= 1 && MathF.Abs(testPosition.y - gridPosition.y) <= 1 && MathF.Abs(testPosition.z - gridPosition.z) <= 1)
            {
                result.Add(ComponentsData[i]);
            }
        }

        return result.ToArray();
    }

    public FTComponentData GetComponentDataAtPosition(Vector3 worldPosition)
    {
        // Transform world to grid point
        Vector3 gridPosition = FTUtils.TransformWorldToGrid(worldPosition: worldPosition);

        for (int i=0; i< ComponentsData.Count; i++)
        {
            if (ComponentsData[i].ComponentPosition == gridPosition)
            {
                return ComponentsData[i];
            }
        }
        return null;
    }

    // Create new component data
    public FTComponentData AddComponentData(Vector3 worldPosition)
    {
        // Transform world to grid point
        Vector3 gridPosition = FTUtils.TransformWorldToGrid(worldPosition: worldPosition);
        FTComponentData newComponentData = new FTComponentData(gridPosition);
        ComponentsData.Add(newComponentData);

        OnComponentDataCreated?.Invoke(newComponentData);

        return newComponentData;
    }

    public void AddFoliage(FoliageType foliageType, Matrix4x4 matrix)
    {
        // Get the component data at location, if null, create a new component data
        FTComponentData componentData = GetComponentDataAtPosition(matrix.GetPosition()) ?? AddComponentData(matrix.GetPosition());

        // Get an existing foliage data in component data, if null, create a new foliage data
        FTComponentData.FoliageData foliageData = componentData.GetFoliageDataFromFoliageType(foliageType) ?? componentData.AddFoliageData(foliageType);

        foliageData.Matrice.Add(matrix);

        OnComponentDataUpdated?.Invoke(componentData);
    }

    // Remove foliage data from a specific component data
    public void RemoveFoliageDataInComponentData(FTComponentData componentData, FoliageType foliageType)
    {
        FTComponentData.FoliageData foliageData = componentData.GetFoliageDataFromFoliageType(foliageType);

        if (foliageData == null) return;

        componentData.FoliagesData.Remove(foliageData);
        OnComponentDataUpdated?.Invoke(componentData);

        return;
    }

    // Remove foliage data for all component data
    public void RemoveFoliageData(FoliageType foliageType)
    {
        for (int i=0; i<ComponentsData.Count; i++)
        {
            FTComponentData.FoliageData foliageData = ComponentsData[i].GetFoliageDataFromFoliageType(foliageType);

            if (foliageData == null) continue;

            ComponentsData[i].FoliagesData.Remove(foliageData);
            OnComponentDataUpdated?.Invoke(ComponentsData[i]);
        }

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
        componentData.FoliagesData.RemoveAll(foliageData => foliageData.Matrice.Count == 0);

        if (componentData.FoliagesData.Count == 0)
        {
            OnComponentDataDeleted?.Invoke(componentData.ID);
            ComponentsData.Remove(componentData);

            return;
        }

        OnComponentDataUpdated?.Invoke(componentData);

        return;
    }
}

[System.Serializable]
public class FTComponentData
{
    public string ID;
    public Vector3 ComponentPosition;
    [SerializeField] public List<FoliageData> FoliagesData = new List<FoliageData>();

    public Bounds Bounds
    {
        get
        {
            return new Bounds(ComponentPosition, new Vector3(25f, 25f, 25f));
        }
    }

    public FTComponentData(Vector3 componentPosition)
    {
        ID = Guid.NewGuid().ToString("N");
        ComponentPosition = componentPosition;
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
        public List<Matrix4x4> Matrice = new List<Matrix4x4>();

        public FoliageData(FoliageType foliageType)
        {
            FoliageType = foliageType;
        }

        public Vector3 GetInstancePosition(int matriceIndex)
        {
            return Matrice[matriceIndex].GetPosition();
        }
    }
}