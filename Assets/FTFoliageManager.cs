using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class FTFoliageManager : MonoBehaviour
{
    // Contains all foliage to render
    public FoliageDataContainer DataContainer;

    private ComputeBuffer[] _argsBuffers;
    private ComputeBuffer[] _foliageBuffers;

    private List<ComputeBuffer> _buffersToRelease = new List<ComputeBuffer>();

    private struct GrassData
    {
        public Vector3 Position;
        public Vector3 Scale;
    }

    private void Awake()
    {
        UpdateFoliage();
    }

    public void UpdateFoliage()
    {
        // ClearBuffers();

        // Initialize buffer arrays with the right number of desired foliage
        _argsBuffers = new ComputeBuffer[DataContainer.FoliageData.Count];
        _foliageBuffers = new ComputeBuffer[DataContainer.FoliageData.Count];

        // Loop over all foliage to create buffers
        for (int i = 0; i < DataContainer.FoliageData.Count; i++)
        {
            if (DataContainer.FoliageData[i].Matrice.Count > 0)
            {
                _foliageBuffers[i] = new ComputeBuffer(DataContainer.FoliageData[i].Matrice.Count, sizeof(float) * 4);
                _argsBuffers[i] = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

                _buffersToRelease.Add(_foliageBuffers[i]);
                _buffersToRelease.Add(_argsBuffers[i]);

                CreateBuffers(foliageBuffer: _foliageBuffers[i], argsBuffer: _argsBuffers[i], foliageData: DataContainer.FoliageData[i]);
            }
        }
    }

    private void Update()
    {
        for (int i = 0; i < DataContainer.FoliageData.Count; i++)
        {
            if (DataContainer.FoliageData[i] != null && _argsBuffers[i] != null)
            {
                Graphics.DrawMeshInstancedIndirect(
                DataContainer.FoliageData[i].Mesh,                  // Mesh
                0,                                                  // Submesh index
                DataContainer.FoliageData[i].Material,              // Material
                new Bounds(Vector3.zero, Vector3.one * 1000f),      // World bounds
                _argsBuffers[i],                                    // Buffer with args
                0,                                                  // Args offset
                null,                                               // Property block
                DataContainer.FoliageData[i].RenderShadows,         // Cast shadows
                DataContainer.FoliageData[i].ReceiveShadows         // Receive shadows
                );
            }
        }
    }

    private void CreateBuffers(ComputeBuffer foliageBuffer, ComputeBuffer argsBuffer, FoliageData foliageData)
    {
        if (foliageBuffer != null) foliageBuffer.Release();

        int instanceCount = foliageData.Matrice.Count;

        foliageBuffer = new ComputeBuffer(instanceCount, 24);
        _buffersToRelease.Add(foliageBuffer);

        GrassData[] data = new GrassData[instanceCount];

        for (int i = 0; i < instanceCount; i++)
        {
            data[i].Position = foliageData.Position(i);
            data[i].Scale = foliageData.Scale(i);
        }

        foliageBuffer.SetData(data);
        foliageData.Material.SetBuffer("grassData", foliageBuffer);

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        uint numIndices = (foliageData.Mesh != null) ? (uint)foliageData.Mesh.GetIndexCount(0) : 0;

        args[0] = numIndices;
        args[1] = (uint)instanceCount;
        argsBuffer.SetData(args);
    }

    void OnDisable()
    {
        ClearBuffers();
    }

    private void ClearBuffers()
    {
        foreach (ComputeBuffer buffer in _buffersToRelease)
        {
            if (buffer != null)
            {
                buffer.Release();
            }
        }
        _buffersToRelease.Clear();
    }
}
