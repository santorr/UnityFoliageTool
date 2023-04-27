using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class FTSceneManager : MonoBehaviour
{
    [SerializeField] public FTSceneData SceneData;

    private GPUInstanceMesh[] _instances = new GPUInstanceMesh[0];

    private void OnEnable()
    {
        CreateInstances();
    }

    private void OnDisable()
    {
        DestroyInstances();
    }

    public void CreateInstances()
    {
        DestroyInstances();

        if (SceneData == null) { return; }

        _instances = new GPUInstanceMesh[SceneData.FoliageData.Count];

        // Create all mesh instances
        for (int i=0; i<SceneData.FoliageData.Count; i++)
        {
            if (SceneData.FoliageData[i].FoliageType != null && SceneData.FoliageData[i].Matrice.Count > 0)
            {
                GPUInstanceMesh newInstance = new GPUInstanceMesh(
                    foliageType: SceneData.FoliageData[i].FoliageType,
                    instanceCount: SceneData.FoliageData[i].Matrice.Count,
                    matrix: SceneData.FoliageData[i].Matrice.ToArray()
                    );

                _instances[i] = newInstance;
            } 
        }
    }

    private void Update()
    {
        for (int i=0; i< _instances.Length; i++)
        {
            _instances[i].Render();
        }
    }

    private void DestroyInstances()
    {
        // Clear instances buffers
        for (int i=0; i< _instances.Length; i++)
        {
            _instances[i].ClearBuffers();
        }
        // Clear instances
        _instances = null;
    }
}

public class GPUInstanceMesh
{
    private Mesh _mesh;
    private Material[] _materials;
    private ShadowCastingMode _renderShadows;
    private bool _receiveShadows;
    private Matrix4x4[] _matrix;
    private int _instanceCount;

    private ComputeBuffer[] _argsBuffers = new ComputeBuffer[0];
    private ComputeBuffer[] _foliageBuffers = new ComputeBuffer[0];

    // Constructor
    public GPUInstanceMesh(FoliageType foliageType, int instanceCount, Matrix4x4[] matrix)
    {
        _mesh = foliageType.Mesh;
        _materials = foliageType.Materials;
        _instanceCount = instanceCount;
        _matrix = matrix;
        _renderShadows = foliageType.RenderShadows;
        _receiveShadows = foliageType.ReceiveShadows;

        _foliageBuffers = new ComputeBuffer[_materials.Length];
        _argsBuffers = new ComputeBuffer[_materials.Length];

        // Create buffers
        for (int i=0; i<_materials.Length; i++)
        {
            // Create a new foliage buffer
            _foliageBuffers[i] = new ComputeBuffer(_instanceCount, sizeof(float) * 16);
            _argsBuffers[i] = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

            _foliageBuffers[i].SetData(_matrix);
            _materials[i].SetBuffer("grassData", _foliageBuffers[i]);

            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

            args[0] = (uint)_mesh.GetIndexCount(i);
            args[1] = (uint)instanceCount;
            args[2] = (uint)_mesh.GetIndexStart(i);
            args[3] = (uint)_mesh.GetBaseVertex(i);
            _argsBuffers[i].SetData(args);
        }

        Debug.Log("New instance created");
    }

    ~GPUInstanceMesh()
    {
        ClearBuffers();
    }

    // Call in update method to render this mesh
    public void Render()
    {
        for (int i=0; i<_materials.Length; i++)
        {
            Graphics.DrawMeshInstancedIndirect(
                mesh: _mesh, 
                submeshIndex: i,
                material: _materials[i], 
                bounds: new Bounds(Vector3.zero, Vector3.one * 1000f),    
                bufferWithArgs: _argsBuffers[i],                                  
                argsOffset: 0,                                               
                properties: null,                                             
                castShadows: _renderShadows,        
                receiveShadows: _receiveShadows
                );
        }
    }

    public void ClearBuffers()
    {
        if (_argsBuffers != null)
        {
            for (int i = 0; i < _argsBuffers.Length; i++)
            {
                _argsBuffers[i].Release();
            }
            _argsBuffers = null;
        }

        if (_foliageBuffers != null)
        {
            for (int i = 0; i < _foliageBuffers.Length; i++)
            {
                _foliageBuffers[i].Release();
            }
            _foliageBuffers = null;
        }
    }
}