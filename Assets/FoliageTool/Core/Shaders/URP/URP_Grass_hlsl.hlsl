#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
StructuredBuffer<float4x4> foliageMatrices;
#endif

void instancingSetup()
{
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    unity_ObjectToWorld = foliageMatrices[unity_InstanceID];
	unity_WorldToObject = transpose(unity_ObjectToWorld);
    #endif
}

void Instancing_float(float3 Position, out float3 Out)
{
    Out = Position;
}