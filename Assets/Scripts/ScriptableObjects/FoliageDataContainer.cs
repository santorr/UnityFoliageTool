using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FoliageDataContainer", menuName = "Foliage/Data Container")]
public class FoliageDataContainer : ScriptableObject
{
    [SerializeField] public List<FoliageData> FoliageData = new List<FoliageData>();

    public void Clear()
    {
        FoliageData.Clear();
    }

    public FoliageData GetFoliageDataFromId(string id)
    {
        for (int i=0; i<FoliageData.Count; i++)
        {
            if (FoliageData[i].ID == id)
            {
                return FoliageData[i];
            }
        }
        return null;
    }
}