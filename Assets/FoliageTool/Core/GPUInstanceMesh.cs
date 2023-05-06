using UnityEngine.Rendering;
using UnityEngine;

public class GPUInstanceMesh
{
    private bool _isDebug = false;

    public readonly Mesh Mesh;
    public readonly Material[] Materials;
    public readonly ShadowCastingMode RenderShadows;
    public readonly bool ReceiveShadows;
    public readonly Matrix4x4[] Matrices;
    public readonly int InstanceCount;
    public readonly Bounds Bounds;
    public readonly int CullingDistance;

    private MaterialPropertyBlock[] _materialProperties = new MaterialPropertyBlock[0];
    private ComputeBuffer[] _argsBuffers = new ComputeBuffer[0];
    private ComputeBuffer _foliageBuffer;

    // Constructor
    public GPUInstanceMesh(FoliageType foliageType, Matrix4x4[] matrices, Bounds bounds)
    {
        // Initialize all variables
        Mesh = foliageType.Mesh;
        Materials = foliageType.Materials;
        InstanceCount = matrices.Length;
        Matrices = matrices;
        RenderShadows = foliageType.RenderShadows;
        ReceiveShadows = foliageType.ReceiveShadows;
        Bounds = bounds;
        CullingDistance = foliageType.CullingDistance;

        CreateBuffers();
    }

    /// <summary>
    /// Create all buffers
    /// </summary>
    private void CreateBuffers()
    {
        // Initialize args buffer, need one buffer per submesh
        _argsBuffers = new ComputeBuffer[Materials.Length];
        _materialProperties = new MaterialPropertyBlock[Materials.Length];

        // Create foliage buffer, this buffer is the same foreach submesh
        _foliageBuffer = new ComputeBuffer(InstanceCount, sizeof(float) * 16);
        _foliageBuffer.SetData(Matrices);

        // Create instance materials and buffers
        for (int i = 0; i < Materials.Length; i++)
        {
            // Create material properties
            _materialProperties[i] = new MaterialPropertyBlock();
            _materialProperties[i].SetBuffer("grassData", _foliageBuffer);

            // Create args buffer
            _argsBuffers[i] = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = (uint)Mesh.GetIndexCount(i);
            args[1] = (uint)InstanceCount;
            args[2] = (uint)Mesh.GetIndexStart(i);
            args[3] = (uint)Mesh.GetBaseVertex(i);
            _argsBuffers[i].SetData(args);
        }

        return;
    }

    /// <summary>
    /// Render instance
    /// </summary>
    public void Render()
    {
        for (int i = 0; i < Materials.Length; i++)
        {
            Graphics.DrawMeshInstancedIndirect(
                mesh: Mesh,
                submeshIndex: i,
                material: Materials[i],
                bounds: Bounds,
                bufferWithArgs: _argsBuffers[i],
                argsOffset: 0,
                properties: _materialProperties[i],
                castShadows: RenderShadows,
                receiveShadows: ReceiveShadows
                );
        }

        FTUtils.Message(_isDebug, this, "Draw : " + Matrices.Length + " of " + Mesh + "\n" +
            "Args buffer count : " + _argsBuffers.Length + "\n" +
            "Matrix length : " + Matrices.Length);

        return;
    }

    /// <summary>
    /// Clear all buffers
    /// </summary>
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

        if (_foliageBuffer != null)
        {
            _foliageBuffer.Release();
            _foliageBuffer = null;
        }

        return;
    }
}