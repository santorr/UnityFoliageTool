using UnityEngine;

public class FTComponent
{
    public bool IsDebug { get; private set; } = false;
    public string ID { get; private set; }
    public Bounds Bounds { get; private set; }

    public GPUInstanceMesh[] Instances = new GPUInstanceMesh[0];

    // Constructor
    public FTComponent(string id, Vector3 worldPosition, int size, FTComponentData data)
    {
        ID = id;
        Bounds = new Bounds(worldPosition, new Vector3(size, size, size));

        CreateInstances(data);
    }

    // Pass new data to this component, then create new instances based on new data
    public void UpdateInstances(FTComponentData data)
    {
        ClearAllInstances();
        CreateInstances(data);
    }

    private void CreateInstances(FTComponentData data)
    {
        ClearAllInstances();

        Instances = new GPUInstanceMesh[data.FoliagesData.Count];

        for (int i = 0; i < data.FoliagesData.Count; i++)
        {
            GPUInstanceMesh newInstance = new GPUInstanceMesh(foliageType: data.FoliagesData[i].FoliageType, matrix: data.FoliagesData[i].Matrice.ToArray(), bounds: Bounds);
            Instances[i] = newInstance;
        }
    }

    // Render all instances
    public void DrawInstances(float distanceFromCamera)
    {
        for (int i=0; i<Instances.Length; i++)
        {
            if (distanceFromCamera < Instances[i].CullingDistance)
            {
                Instances[i].Render();
            }
        }
    }

    // Remove instances references
    public void ClearAllInstances()
    {
        for (int i=0; i<Instances.Length; i++)
        {
            if (Instances[i] != null)
            {
                Instances[i].ClearBuffers();
                Instances[i] = null;
            }
        }
        Instances = new GPUInstanceMesh[0];
    }
}