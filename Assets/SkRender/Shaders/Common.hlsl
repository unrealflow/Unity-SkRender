#ifndef _SHADER_COMMON_
#define _SHADER_COMMON_

#define MAX_LIGHTS 4

cbuffer CameraBuffer {
	float4x4	_View;
	float4x4	_Proj;
	float4x4	_JitterProj;
	float4x4	_PreView;
	float4x4	_PreProj;
	float4x4	_InvView;
	float4x4	_InvProj;
	float		_FarClip;
	float4 _LightColors[MAX_LIGHTS];
	float4 _LightPositions[MAX_LIGHTS];
	float4 _RTSize;
}
cbuffer WorldBuffer
{
	int _FrameIndex;
}

struct Light
{
	float3 color;
	float3 dir;
	int tag;
};

Light GetLight(int index,float3 pos,float3 noise)
{
	Light l;
	l.tag=1;
	l.color=_LightColors[index].rgb/30000.0;
	float4 lightPosition = _LightPositions[index];
	if(lightPosition.w<0)
	{
		l.tag=-1;
		return l;
	}else if(lightPosition.w>0)
	{
		float3 d=lightPosition.xyz-pos;
		l.dir = normalize(d+_LightColors[index].w*noise);
		l.color *= 1.3/(0.3+length(d));
	}
	else{
		l.dir=normalize(lightPosition.xyz);
	}
	return l;
}

#endif