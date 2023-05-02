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
        // Get component data at foliage location
        FTComponentData componentData = GetComponentDataAtPosition(matrix.GetPosition());
        // If the component doesn't exists, create it
        if (componentData == null)
        {
            componentData = AddComponentData(matrix.GetPosition());
        }

        FTComponentData.FoliageData foliageData = componentData.ContainsFoliageType(foliageType);

        if (foliageData == null)
        {
            foliageData = componentData.AddFoliageData(foliageType);
        }

        foliageData.Matrice.Add(matrix);

        OnComponentDataUpdated?.Invoke(componentData);
    }

    public void RemoveFoliageData(FTComponentData componentData, FoliageType foliageType)
    {
        for (int i=0; i<componentData.FoliagesData.Count; i++)
        {
            FTComponentData.FoliageData foliageData = componentData.ContainsFoliageType(foliageType);

            if (foliageData != null)
            {
                componentData.FoliagesData.Remove(foliageData);
                OnComponentDataUpdated?.Invoke(componentData);
                return;
            }
        }
    }

    public void CleanComponents()
    {
        for (int i=0; i< ComponentsData.Count; i++)
        {
            CleanComponent(ComponentsData[i]);
        }
    }

    public void CleanComponent(FTComponentData componentData)
    {
        for (int i = 0; i < componentData.FoliagesData.Count; i++)
        {
            if (componentData.FoliagesData[i].Matrice.Count == 0)
            {
                componentData.FoliagesData.RemoveAt(i);
            }
        }

        // If this component has no foliage data, destroy component
        if (componentData.FoliagesData.Count == 0)
        {
            OnComponentDataDeleted?.Invoke(componentData.ID);
            ComponentsData.Remove(componentData);
            return;
        }
        OnComponentDataUpdated?.Invoke(componentData);
    }
}

[System.Serializable]
public class FTComponentData
{
    public string ID;
    public Vector3 ComponentPosition;
    [SerializeField] public List<FoliageData> FoliagesData = new List<FoliageData>();

    public FTComponentData(Vector3 componentPosition)
    {
        ID = Guid.NewGuid().ToString("N");
        ComponentPosition = componentPosition;
    }

    // Check if this component data contains a specific foliage type
    public FoliageData ContainsFoliageType(FoliageType foliageType)
    {
        for (int i=0; i<FoliagesData.Count; i++)
        {
            if (FoliagesData[i].FoliageType == foliageType)
            {
                return FoliagesData[i];
            }
        }
        return null;
    }

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