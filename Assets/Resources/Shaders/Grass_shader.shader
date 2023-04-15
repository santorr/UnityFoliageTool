Shader "Foliage/Grass_shader" 
{
	Properties{
		[NoScaleOffset] _MainTex("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}
		_NormalIntensity("Normal intensity", Range(0, 10)) = 1
		[NoScaleOffset] _RMO("Roughness / Metallic / Occlusion", 2D) = "white" {}
		_RoughnessIntensity("Roughness intensity", Range(0, 1)) = 0.75
		_SpecularIntensity("Specular intensity", Range(0, 1)) = 0.5
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
		_CullingDistance("Culling distance", Range(0, 100)) = 15
		_CullingAngle("Culling angle", Range(0, 180)) = 90
	}
		SubShader{
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

		Blend Off
        Cull Off
        ZWrite On
        ZTest LEqual

		CGPROGRAM
		#include "UnityCG.cginc"
		#pragma target 5.0
		#pragma surface surf StandardSpecular addshadow
		#pragma multi_compile_instancing
		#pragma instancing_options procedural:setup

		sampler2D _MainTex;
		sampler2D _NormalMap;
		float _NormalIntensity;
		sampler2D _RMO;
		float _RoughnessIntensity;
		float _SpecularIntensity;
		fixed _Cutoff;
		fixed _CullingDistance;
		fixed _CullingAngle;

		struct Input {
			float2 uv_MainTex : TEXCOORD0;
			float2 uv_NormalMap : TEXCOORD1;
			float2 uv_RMO : TEXCOORD2;
		};

		struct GrassData {
			float3 position;
			float3 scale;
		};

		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			StructuredBuffer<GrassData> grassData;
		#endif

	// Function to flatten normal
	float3 FlattenNormal(float3 normal, float intensity)
	{
		return lerp(normal, float3(0, 0, 1), intensity);
	}

	// Function is visible
	int IsVisible(float4 position)
	{
		// Hide with distance
		if(distance(_WorldSpaceCameraPos, position) > _CullingDistance)
		{
			return 0;
		}
		// Hide with angle
		float3 camDir = normalize(_WorldSpaceCameraPos - position);
		float3 viewDir = mul((float3x3)UNITY_MATRIX_V,float3(0,0,1));

		float angle = acos(dot(camDir, viewDir));

		if (angle > radians(_CullingAngle)) 
		{
			return 0;
		}
		return 1;
	}

	void setup()
	{
		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

		float4 position = float4(grassData[unity_InstanceID].position, 1); // Position

		float scale = grassData[unity_InstanceID].scale; // Scale


		if(_CullingDistance > 0)
		{
			// On calcul la distance
			scale = mul(scale, IsVisible(position));
		}

		unity_ObjectToWorld._11_21_31_41 = float4(scale, 0, 0, 0);
		unity_ObjectToWorld._12_22_32_42 = float4(0, scale, 0, 0);
		unity_ObjectToWorld._13_23_33_43 = float4(0, 0, scale, 0);
		unity_ObjectToWorld._14_24_34_44 = float4(position.xyz, 1);

		unity_WorldToObject = unity_ObjectToWorld;
		unity_WorldToObject._14_24_34 *= -1;
		unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;

		#endif
	}

	// Surface
	void surf(Input IN, inout SurfaceOutputStandardSpecular o) 
	{
		float4 albedoMap = tex2D(_MainTex, IN.uv_MainTex);
		float3 normalMap = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
		float4 rmoMap = tex2D(_RMO, IN.uv_RMO);
		clip(albedoMap.a - _Cutoff);
		o.Albedo = albedoMap.rgb;
		o.Emission = float4(0, 0, 0, 1);
		o.Normal = FlattenNormal(normalMap, _NormalIntensity);
		o.Specular = _SpecularIntensity;
		o.Smoothness = 1 - clamp(mul(rmoMap.r, _RoughnessIntensity),0 , 1);
		o.Alpha = albedoMap.a;
	}
	ENDCG
	}
	FallBack "Diffuse"
}