using UnityEngine.Rendering;
using UnityEngine;

public class GPUInstanceMesh
{
    private bool IsDebug = false;

    public readonly Mesh Mesh;
    public readonly Material[] Materials;
    public readonly ShadowCastingMode RenderShadows;
    public readonly bool ReceiveShadows;
    public readonly Matrix4x4[] Matrix;
    public readonly int InstanceCount;
    public readonly Bounds Bounds;
    public readonly int CullingDistance;

    private ComputeBuffer[] _argsBuffers = new ComputeBuffer[0];
    private ComputeBuffer[] _foliageBuffers = new ComputeBuffer[0];

    // Constructor
    public GPUInstanceMesh(FoliageType foliageType, Matrix4x4[] matrix, Bounds bounds)
    {
        Mesh = foliageType.Mesh;
        Materials = foliageType.Materials;
        InstanceCount = matrix.Length;
        Matrix = matrix;
        RenderShadows = foliageType.RenderShadows;
        ReceiveShadows = foliageType.ReceiveShadows;
        Bounds = bounds;
        CullingDistance = foliageType.CullingDistance;

        _foliageBuffers = new ComputeBuffer[Materials.Length];
        _argsBuffers = new ComputeBuffer[Materials.Length];

        // Create materials
        for (int i=0; i<Materials.Length; i++)
        {
            Materials[i] = new Material(Materials[i]);
        }

        // Create buffers
        for (int i = 0; i < Materials.Length; i++)
        {
            // Create a new foliage buffer
            _foliageBuffers[i] = new ComputeBuffer(InstanceCount, sizeof(float) * 16);
            _argsBuffers[i] = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

            _foliageBuffers[i].SetData(Matrix);
            Materials[i].SetBuffer("grassData", _foliageBuffers[i]);

            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

            args[0] = (uint)Mesh.GetIndexCount(i);
            args[1] = (uint)InstanceCount;
            args[2] = (uint)Mesh.GetIndexStart(i);
            args[3] = (uint)Mesh.GetBaseVertex(i);
            _argsBuffers[i].SetData(args);
        }
    }

    // Call in update method to render this mesh
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
                properties: null,
                castShadows: RenderShadows,
                receiveShadows: ReceiveShadows
                );
        }

        FTUtils.Message(IsDebug, this, "Draw : " + Matrix.Length + " of " + Mesh + "\n" +
            "Foliage buffer count : " + _foliageBuffers.Length + "\n" +
            "Args buffer count : " + _argsBuffers.Length + "\n" +
            "Matrix length : " + Matrix.Length);
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