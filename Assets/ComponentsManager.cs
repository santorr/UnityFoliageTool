using System.Collections.Generic;
using UnityEngine;

public class ComponentsManager : MonoBehaviour
{
    public int ComponentSize = 10;
    public FTSceneData SceneManager;

    private List<Component> _components = new List<Component>();
    private Camera _camera;

    
    public GameObject Prefab; // TEST

    private void Start()
    {
        // Initialize the camera
        _camera = Camera.main;

        // Instantiate cubes at random position for testing
        for(int i=0; i<200; i++)
        {
            int randomDistance = 100;
            Vector3 newPosition = new Vector3(
                Random.Range(-randomDistance, randomDistance), 
                Random.Range(-0, 0), 
                Random.Range(-randomDistance, randomDistance));

            Component targetComponent = GetComponentAtPosition(WorldPositionToComponentPosition(newPosition));

            // If no component exists at cube position, create a new one
            if (targetComponent == null)
            {
                targetComponent = CreateComponent(WorldPositionToComponentPosition(newPosition));
            }

            targetComponent.GameObjects.Add(Instantiate(Prefab, newPosition, Quaternion.identity, transform));
        }
    }

    private void Update()
    {
        // Calculate the frustrum of the camera
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_camera);
        // For each components, set the distance and set the visibility
        for (int i=0;i<_components.Count; i++)
        {
            _components[i].SetDistanceFromCamera(_camera.transform.position);
            _components[i].UpdateVisibility(planes);
            _components[i].Render();
        }
    }

    // Create and return a new chunk at position
    private Component CreateComponent(Vector3 componentPosition)
    {
        Component newComponent = new Component(worldPosition: componentPosition, size: ComponentSize, displayDistance: 50f);
        _components.Add(newComponent);
        return newComponent;
    }

    // Return a chunk position where must be the desired position
    private Vector3 WorldPositionToComponentPosition(Vector3 worldPosition)
    {
        return new Vector3(
            Mathf.Round(worldPosition.x / ComponentSize) * ComponentSize,
            Mathf.Round(worldPosition.y / ComponentSize) * ComponentSize,
            Mathf.Round(worldPosition.z / ComponentSize) * ComponentSize
            );
    }

    // Return chunk that match with position
    public Component GetComponentAtPosition(Vector3 position)
    {
        for (int i=0; i< ComponentCount; i++)
        {
            if (_components[i].WorldPosition == position)
            {
                return _components[i];
            }
        }
        return null;
    }

    // Return the number of chunks
    public int ComponentCount
    {
        get { return _components.Count; }
    }

    // Draw debug gizmos on each chunks
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < _components.Count; i++)
        {
            if (_components[i].Visible)
            {
                Gizmos.DrawWireCube(_components[i].Bounds.center, _components[i].Bounds.size);
            }
        }
    }
}

public class Component
{
    public bool Visible { get; private set; }
    public int Size { get; private set; } 
    public Vector3 WorldPosition { get; private set; }
    public float DistanceFromCamera { get; private set; }
    public float DisplayDistance { get; private set; }

    public List<GameObject> GameObjects = new List<GameObject>();

    // Constructor
    public Component(Vector3 worldPosition, int size, float displayDistance)
    {
        Size = size;
        WorldPosition = worldPosition;
        DisplayDistance = displayDistance;
    }

    // Return chunk bounding box
    public Bounds Bounds
    {
        get
        {
            return new Bounds(WorldPosition, new Vector3(Size, Size, Size));
        }
    }

    // Update the component visibility depending on frustrum and distance
    public void UpdateVisibility(Plane[] planes)
    {
        if (GeometryUtility.TestPlanesAABB(planes, Bounds))
        {
            if (DistanceFromCamera < DisplayDistance)
            {
                Visible = true;
                return;
            }
        }
        Visible = false;
    }

    public void SetDistanceFromCamera(Vector3 cameraPosition)
    {
        DistanceFromCamera = Vector3.Distance(cameraPosition, WorldPosition);
    }

    public void Render()
    {
        for (int i=0; i<GameObjects.Count; i++)
        {
            GameObjects[i].SetActive(Visible);
        }
    }
}