struct GrassData
{
    float3 position; 
    float3 scale;
};

StructuredBuffer<GrassData> grassData;

void getPos_float(int instanceID, float3 vertexPosition, out float3 Out)
{
    float3 newPosition = vertexPosition + grassData[instanceID].position;
    Out = newPosition;
}