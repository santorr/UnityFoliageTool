using System.Collections.Generic;
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
            float distanceFromCamera = Vector3.Distance(Camera.main.transform.position, Components[i].Bounds.center);
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
        for (int i=0; i<Components.Count; i++)
        {
            if (Components[i].ID == componentData.ID)
            {
                Components[i].UpdateInstances(componentData);
                return;
            }
        }
    }

    public void DeleteComponent(string componentID)
    {
        for (int i = 0; i < Components.Count; i++)
        {
            if (Components[i].ID == componentID)
            {
                Components[i].ClearAllInstances();
                Components.Remove(Components[i]);
                return;
            }
        }
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
            Gizmos.color = new Color(0.27f, 0.38f, 0.49f, 1);

            for (int i = 0; i < Components.Count; i++)
            {
                Gizmos.DrawWireCube(Components[i].Bounds.center, Components[i].Bounds.size);
            }
        }
    }
}