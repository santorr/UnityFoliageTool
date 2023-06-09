using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

[ExecuteAlways]
public class FTManager : MonoBehaviour
{
    public bool IsDebug = false;
    public FTSceneData SceneData;
    private List<FTComponent> Components = new List<FTComponent>();

    private Camera _camera;

    Thread _thread;


    /// <summary>
    /// Listen for events coming from scene data and initialize the manager.
    /// </summary>
    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            FTSceneData.OnComponentDataCreated += CreateComponent;
            FTSceneData.OnComponentDataUpdated += UpdateComponent;
            FTSceneData.OnComponentDataDeleted += DestroyComponent;
        }

        _camera = Camera.main;

        Initialize();
    }

    /// <summary>
    /// Remove all listening and destroy all components.
    /// </summary>
    private void OnDisable()
    {
        FTUtils.Message(IsDebug, this, "Disabled");

        FTSceneData.OnComponentDataCreated -= CreateComponent;
        FTSceneData.OnComponentDataUpdated -= UpdateComponent;
        FTSceneData.OnComponentDataDeleted -= DestroyComponent;

        DestroyAllComponents();

        return;
    }

    /// <summary>
    /// Initialize the manager, destroy all components and create new ones.
    /// </summary>
    private void Initialize()
    {
        FTUtils.Message(IsDebug, this, "Initialize");

        DestroyAllComponents();

        if (SceneData == null) return;

        SceneData.ComponentsData.ForEach(componentData => CreateComponent(componentData));

        return;
    }

    /// <summary>
    /// Draw instances
    /// </summary>
    private void Update()
    {
        Profiler.BeginSample("Manager draw components instances");
        Plane[] frustrumPlanes = GeometryUtility.CalculateFrustumPlanes(_camera);
        
        for (int i = 0; i < Components.Count; i++)
        {
            if (GeometryUtility.TestPlanesAABB(frustrumPlanes, Components[i].Bounds))
            {
                    Components[i].DrawInstances();
            }
        }
        Profiler.EndSample();
    }

    /// <summary>
    /// Create a new component with component data
    /// </summary>
    /// <param name="componentData"></param>
    private void CreateComponent(FTComponentData componentData)
    {
        FTComponent newComponent = new FTComponent( id: componentData.ID, bounds: componentData.Bounds, componentData: componentData);

        Components.Add(newComponent);

        return;
    }

    /// <summary>
    /// Upodate component instances from new component data
    /// </summary>
    /// <param name="componentData"></param>
    public void UpdateComponent(FTComponentData componentData)
    {
        FTComponent component = GetComponentFromID(componentData.ID);

        if (component == null) return;

        component.UpdateInstances(componentData);

        return;
    }

    /// <summary>
    /// Get a component from ID
    /// </summary>
    /// <param name="componentID"></param>
    /// <returns></returns>
    private FTComponent GetComponentFromID(string componentID)
    {
        return Components.Find(component => component.ID == componentID);
    }

    /// <summary>
    /// Delete a component from ID
    /// </summary>
    /// <param name="componentID"></param>
    public void DestroyComponent(string componentID)
    {
        FTComponent component = GetComponentFromID(componentID);

        if (component == null) return;

        component.ClearAllInstances();
        Components.Remove(component);

        return;
    }

    /// <summary>
    /// Destroy all components
    /// </summary>
    private void DestroyAllComponents()
    {
        for (int i=0; i<Components.Count; i++)
        {
            if (Components[i] == null) continue;

            Components[i].ClearAllInstances();
        }

        Components.Clear();

        return;
    }


#if UNITY_EDITOR
    /// <summary>
    /// Draw debug gizmos
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!IsDebug) return;

        Handles.zTest = CompareFunction.LessEqual;
        Handles.color = new Color(0f, 0.75f, 1f, 1f);

        for (int i = 0; i < Components.Count; i++)
        {
            Handles.DrawWireCube(Components[i].Bounds.center, Components[i].Bounds.size);
        }

        return;
    }
#endif
}