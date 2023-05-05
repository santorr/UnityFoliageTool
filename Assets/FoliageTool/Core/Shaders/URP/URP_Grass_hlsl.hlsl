#ifndef BELT_SHADER_INCLUDED
#define BELT_SHADER_INCLUDED

StructuredBuffer<float4x4> grassData;

void instancingSetup()
{
    #ifndef SHADERGRAPH_PREVIEW
        unity_ObjectToWorld = grassData[unity_InstanceID];
		unity_WorldToObject = transpose(unity_ObjectToWorld);
    #endif
}

void GetInstanceID_float(out float Out)
{
    Out = 0;
    #ifndef SHADERGRAPH_PREVIEW
    #if UNITY_ANY_INSTANCING_ENABLED
    Out = unity_InstanceID;
    #endif
    #endif
}

void Instancing_float(float3 Position, out float3 Out)
{
    Out = Position;
}

#endif