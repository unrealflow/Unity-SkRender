#ifndef _SHADER_RAY_COMMON_
#define _SHADER_RAY_COMMON_

#include "Common.hlsl"
#include "UnityRaytracingMeshUtils.cginc"

struct RP
{
  float3 color;
  float3 pos;
  float3 dir;
  float3 kS;
  uint4 states;
};
struct ShadowRP
{
  bool shadowed;
};
struct AttributeData
{
  float2 data;
};
struct Vertex
{
  float3 position;
  float3 normal;
  float2 uv;
};

RaytracingAccelerationStructure _RTAS;

// d:float3 barycentricCoords
float3 FetchVertex3(uint3 indices, float3 d, uint type)
{
  float3 p0 = UnityRayTracingFetchVertexAttribute3(indices.x, type);
  float3 p1 = UnityRayTracingFetchVertexAttribute3(indices.y, type);
  float3 p2 = UnityRayTracingFetchVertexAttribute3(indices.z, type);
  float3 o = p0 * d.x + p1 * d.y + p2 * d.z;
  return o;
}
// d:float3 barycentricCoords
float2 FetchVertex2(uint3 indices, float3 d, uint type)
{
  float2 p0 = UnityRayTracingFetchVertexAttribute2(indices.x, type);
  float2 p1 = UnityRayTracingFetchVertexAttribute2(indices.y, type);
  float2 p2 = UnityRayTracingFetchVertexAttribute2(indices.z, type);
  float2 o = p0 * d.x + p1 * d.y + p2 * d.z;
  return o;
}

Vertex FetchFrag(uint primIndex, AttributeData d)
{
  float3 barycentricCoords = float3(1.0f - d.data.x - d.data.y, d.data.x, d.data.y);
  uint3 indices = UnityRayTracingFetchTriangleIndices(primIndex);
  Vertex v;
  v.position=mul(ObjectToWorld3x4(),float4(FetchVertex3(indices,barycentricCoords,kVertexAttributePosition),1.0)).xyz;
  v.normal=mul(ObjectToWorld3x4(),float4(FetchVertex3(indices,barycentricCoords,kVertexAttributeNormal),0.0)).xyz;
  v.normal=normalize(v.normal);
  v.uv=FetchVertex2(indices,barycentricCoords,kVertexAttributeTexCoord0);
  return v;
}

#endif