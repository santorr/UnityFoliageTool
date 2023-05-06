using UnityEngine;

public class FTComponent
{
    public string ID { get; private set; }
    public Bounds Bounds { get; private set; }

    public GPUInstanceMesh[] Instances = new GPUInstanceMesh[0];

    public FTComponent(string id, Bounds bounds, FTComponentData componentData)
    {
        ID = id;
        Bounds = bounds;

        CreateInstances(componentData);
    }

    // Create all instances based on component data
    private void CreateInstances(FTComponentData data)
    {
        ClearAllInstances();

        // Initialize the length of the array
        Instances = new GPUInstanceMesh[data.FoliagesData.Count];

        // Loop over data to create instances
        for (int i = 0; i < data.FoliagesData.Count; i++)
        {
            Instances[i] = new GPUInstanceMesh(foliageType: data.FoliagesData[i].FoliageType, matrices: data.FoliagesData[i].Matrices.ToArray(), bounds: Bounds);
        }
    }

    // Send new data to this component, then create new instances based on new data
    public void UpdateInstances(FTComponentData data)
    {
        ClearAllInstances();
        CreateInstances(data);
    }

    // Render all instances if distance to camera is less than culling distance
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

    // Remove instances references and clear buffers to prevent memory leaks
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