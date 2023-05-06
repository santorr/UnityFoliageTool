using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

[ExecuteAlways]
public class FTManager : MonoBehaviour
{
    public bool IsDebug = false;
    public FTSceneData SceneData;
    private List<FTComponent> Components = new List<FTComponent>();

    private void OnEnable()
    {
        FTUtils.Message(IsDebug, this, "Enabled");

        if (!Application.isPlaying)
        {
            FTSceneData.OnComponentDataCreated += CreateComponent;
            FTSceneData.OnComponentDataUpdated += UpdateComponent;
            FTSceneData.OnComponentDataDeleted += DeleteComponent;
        }
        Initialize();
    }

    private void OnDisable()
    {
        FTUtils.Message(IsDebug, this, "Disabled");

        FTSceneData.OnComponentDataCreated -= CreateComponent;
        FTSceneData.OnComponentDataUpdated -= UpdateComponent;
        FTSceneData.OnComponentDataDeleted -= DeleteComponent;

        DestroyComponents();
    }

    // From FTSceneData create components
    private void Initialize()
    {
        FTUtils.Message(IsDebug, this, "Initialize");

        DestroyComponents();

        // Loop over all components
        for (int i=0; i<SceneData.ComponentsData.Count; i++)
        {
            CreateComponent(SceneData.ComponentsData[i]);
        }
    }

    private void Update()
    {
        for (int i = 0; i < Components.Count; i++)
        {
            float distanceFromCamera = Application.isPlaying ? Vector3.Distance(Camera.main.transform.position, Components[i].Bounds.center) : 0f;

            Components[i].DrawInstances(distanceFromCamera: distanceFromCamera);
        }
    }

    private void CreateComponent(FTComponentData componentData)
    {
        FTComponent newComponent = new FTComponent(
            id: componentData.ID,
            worldPosition: componentData.ComponentPosition,
            size: SceneData.ComponentSize,
            data: componentData
            );
        Components.Add(newComponent);
    }

    public void UpdateComponent(FTComponentData componentData)
    {
        FTComponent component = GetComponentFromID(componentData.ID);

        if (component == null) return;

        component.UpdateInstances(componentData);

        return;
    }

    // Delete a component, clear his instances and remove from components list
    public void DeleteComponent(string componentID)
    {
        FTComponent component = GetComponentFromID(componentID);

        if (component == null) return;

        component.ClearAllInstances();
        Components.Remove(component);

        return;
    }

    // Return a component corresponding to ID
    private FTComponent GetComponentFromID(string componentID)
    {
        return Components.Find(component => component.ID == componentID);
    }

    // Clear references to components
    private void DestroyComponents()
    {
        for (int i=0; i<Components.Count; i++)
        {
            if (Components[i] != null)
            {
                Components[i].ClearAllInstances();
                Components[i] = null;
            }
        }
        Components.Clear();
    }

    // Draw debug gizmos on each chunks
    private void OnDrawGizmos()
    {
        if (IsDebug)
        {
            Gizmos.color = new Color(0.27f, 0.38f, 0.49f, 0.3f);

            for (int i = 0; i < Components.Count; i++)
            {
                Gizmos.DrawCube(Components[i].Bounds.center, Components[i].Bounds.size);
            }
        }
    }
}