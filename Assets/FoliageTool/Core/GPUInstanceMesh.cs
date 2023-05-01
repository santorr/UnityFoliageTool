using UnityEngine.Rendering;
using UnityEngine;

public class GPUInstanceMesh
{
    private bool IsDebug = false;
    private Mesh _mesh;
    private Material[] _materials;
    private ShadowCastingMode _renderShadows;
    private bool _receiveShadows;
    private Matrix4x4[] _matrix;
    private int _instanceCount;
    private Bounds _bounds;

    private ComputeBuffer[] _argsBuffers = new ComputeBuffer[0];
    private ComputeBuffer[] _foliageBuffers = new ComputeBuffer[0];

    // Constructor
    public GPUInstanceMesh(FoliageType foliageType, Matrix4x4[] matrix, Bounds bounds)
    {
        _mesh = foliageType.Mesh;
        _materials = foliageType.Materials;
        _instanceCount = matrix.Length;
        _matrix = matrix;
        _renderShadows = foliageType.RenderShadows;
        _receiveShadows = foliageType.ReceiveShadows;
        _bounds = bounds;

        _foliageBuffers = new ComputeBuffer[_materials.Length];
        _argsBuffers = new ComputeBuffer[_materials.Length];

        // Create materials
        for (int i=0; i<_materials.Length; i++)
        {
            _materials[i] = new Material(_materials[i]);
        }

        // Create buffers
        for (int i = 0; i < _materials.Length; i++)
        {
            // Create a new foliage buffer
            _foliageBuffers[i] = new ComputeBuffer(_instanceCount, sizeof(float) * 16);
            _argsBuffers[i] = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

            _foliageBuffers[i].SetData(_matrix);
            _materials[i].SetBuffer("grassData", _foliageBuffers[i]);

            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

            args[0] = (uint)_mesh.GetIndexCount(i);
            args[1] = (uint)_instanceCount;
            args[2] = (uint)_mesh.GetIndexStart(i);
            args[3] = (uint)_mesh.GetBaseVertex(i);
            _argsBuffers[i].SetData(args);
        }
    }

    // Call in update method to render this mesh
    public void Render()
    {
        for (int i = 0; i < _materials.Length; i++)
        {
            Graphics.DrawMeshInstancedIndirect(
                mesh: _mesh,
                submeshIndex: i,
                material: _materials[i],
                bounds: _bounds,
                bufferWithArgs: _argsBuffers[i],
                argsOffset: 0,
                properties: null,
                castShadows: _renderShadows,
                receiveShadows: _receiveShadows
                );
        }

        FTUtils.Message(IsDebug, this, "Draw : " + _matrix.Length + " of " + _mesh + "\n" +
            "Foliage buffer count : " + _foliageBuffers.Length + "\n" +
            "Args buffer count : " + _argsBuffers.Length + "\n" +
            "Matrix length : " + _matrix.Length);
    }

    // Clear buffers
    public void ClearBuffers()
    {
        if (_argsBuffers != null)
        {
            for (int i = 0; i < _argsBuffers.Length; i++)
            {
                _argsBuffers[i].Release();
            }
            _argsBuffers = new ComputeBuffer[0];
        }

        if (_foliageBuffers != null)
        {
            for (int i = 0; i < _foliageBuffers.Length; i++)
            {
                _foliageBuffers[i].Release();
            }
            _foliageBuffers = new ComputeBuffer[0];
        }
    }
}