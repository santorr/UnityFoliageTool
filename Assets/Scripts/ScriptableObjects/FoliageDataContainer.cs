using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Foliage data container", menuName = "Foliage tool/Data container")]
public class FoliageDataContainer : ScriptableObject
{
    [SerializeField] public List<FoliageData> FoliageData = new List<FoliageData>();


    public void Clear()
    {
        FoliageData.Clear();
    }
}