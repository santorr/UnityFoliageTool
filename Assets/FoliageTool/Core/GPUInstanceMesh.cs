using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Profiling;

public class Args
{
    public uint[] args;

    public Args(uint meshIndexCount, uint instanceCount, uint meshIndexStart, uint meshBaseVertex) 
    {
        args = new uint[5] { meshIndexCount, instanceCount, meshIndexStart, meshBaseVertex, 0 };
    }
}


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

    int kernel;
    private ComputeShader _computeShader;
    private ComputeBuffer _appendInstanceBuffer;
    private Args[] _args;

    private ComputeBuffer[] _argsBuffers = new ComputeBuffer[0];
    private ComputeBuffer _foliageBuffer;

    private Camera _camera;

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

        // Initialize compute shader
        _computeShader = Resources.Load<ComputeShader>("FTFrustrumCulling");
        kernel = _computeShader.FindKernel("CSMain");

        // Camera
        _camera = Camera.main;

        CreateBuffers();
    }

    /// <summary>
    /// Create all buffers
    /// </summary>
    private void CreateBuffers()
    {
        _appendInstanceBuffer = new ComputeBuffer(InstanceCount, sizeof(float) * 16, ComputeBufferType.Append);

        // Initialize args buffer, need one buffer per submesh
        _args = new Args[Materials.Length];
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
            _materialProperties[i].SetBuffer("foliageMatrices", _appendInstanceBuffer);

            // Create args buffer
            _argsBuffers[i] = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            _args[i] = new Args(Mesh.GetIndexCount(i), (uint)InstanceCount, Mesh.GetIndexStart(i), Mesh.GetBaseVertex(i));
            _argsBuffers[i].SetData(_args[i].args);
        }

        return;
    }

    /// <summary>
    /// Render instance
    /// </summary>
    public void Render()
    {
        Profiler.BeginSample("ComputeShader.Setup");
        _appendInstanceBuffer.SetCounterValue(0);
        _computeShader.SetFloats("_CameraFrustumPlanes", GetFrustumPlanes(_camera));
        _computeShader.SetBuffer(0, "_AppendInstanceBuffer", _appendInstanceBuffer);
        _computeShader.SetBuffer(0, "_InstanceBuffer", _foliageBuffer);
        _computeShader.SetVector("_CameraPosition", _camera.transform.position);
        Profiler.EndSample();

        Profiler.BeginSample("ComputeShader.Dispatch");
        _computeShader.Dispatch(0, InstanceCount, 1, 1);
        Profiler.EndSample();

        for (int i = 0; i < Materials.Length; i++)
        {
            Profiler.BeginSample("ComputeBuffer.CopyCount");
            ComputeBuffer.CopyCount(_appendInstanceBuffer, _argsBuffers[i], 4 * 1);
            Profiler.EndSample();

            Profiler.BeginSample("ComputeBuffer.GetData");
            // _argsBuffers[i].GetData(_args[i].args);
            Profiler.EndSample();

            Profiler.BeginSample("Graphics.DrawMeshInstancedIndirect");
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
            Profiler.EndSample();
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

        if (_appendInstanceBuffer != null)
        {
            _appendInstanceBuffer.Release();
            _appendInstanceBuffer = null;
        }

        return;
    }

    /// <summary>
    /// Calculate planes for camera frustrum
    /// </summary>
    /// <param name="camera"></param>
    /// <returns></returns>
    private float[] GetFrustumPlanes(Camera camera)
    {
        const int floatPerNormal = 4;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);

        float[] planeNormals = new float[planes.Length * floatPerNormal];
        for (int i = 0; i < planes.Length; ++i)
        {
            planeNormals[i * floatPerNormal + 0] = planes[i].normal.x;
            planeNormals[i * floatPerNormal + 1] = planes[i].normal.y;
            planeNormals[i * floatPerNormal + 2] = planes[i].normal.z;
            planeNormals[i * floatPerNormal + 3] = planes[i].distance;
        }
        return planeNormals;
    }
}