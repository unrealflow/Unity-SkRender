#ifndef _SHADER_BRDF_
#define _SHADER_BRDF_

#ifndef  PI
#define PI 3.1415926535898
#endif

struct SkMat
{
	float3 baseColor;
	float roughness;
	float metallic;
};
float pw5(float x)
{
	float x2 = x * x;
	return x2 * x2 * x;
}
float D_GGX(float NoH, float roughness)
{
	float a = roughness;
	float a2 = a * a;
	float k = NoH * NoH * (a2 - 1.0) + 1.0;
	return a2 / (PI * k * k);
}
float G_SchlickGGX(float NoV, float k)
{
	return NoV / (NoV + k - NoV * k);
}
float G_Smith(float NoV, float NoL, float roughness)
{
	float r = roughness + 1.0;
	float k = r * r * 0.125;
	float ggx1 = G_SchlickGGX(NoV, k);
	float ggx2 = G_SchlickGGX(NoL, k);
	return ggx1 * ggx2;
}
float3 F_Schlick(float HoV, float3 F0)
{
	float p = pw5(1 - HoV);
	return F0 + p - F0 * p;
}

float3 BRDF(SkMat mat, float3 L, float3 V, float3 N)
{
	float NoL = dot(N, L);
	float NoV = dot(N, V);
	float3 F0 = 0.04;
	F0 = lerp(F0, mat.baseColor,mat.metallic);
	if (NoL < 0 || NoV < 0)
		return float3(0.0, 0.0, 0.0);

	float3 H = normalize(V + L);
	float NoH = dot(N, H);
	float HoV = dot(H, V);

	float D = D_GGX(NoH, mat.roughness);
	float G = G_Smith(NoV, NoL, mat.roughness);
	float3 F = F_Schlick(HoV, F0);

	float3 kD = 1.0 - F;
	kD *= 1.0 - mat.metallic;

	float3 numerator = D * G * F;
	float denominator = 4.0 * NoL * NoV + 1e-5;
	float3 specular = numerator / denominator;
	float3 lo = (kD * mat.baseColor / PI + specular) * NoL;
	return lo;
}
#endif