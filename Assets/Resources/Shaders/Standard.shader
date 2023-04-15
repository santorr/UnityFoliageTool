Shader "Custom/Standard" 
{
	Properties{
		_Tilling("Tilling", float) = 1
		[NoScaleOffset] _MainTex("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}
		_NormalIntensity("Normal intensity", Range(0, 10)) = 1
		[NoScaleOffset] _RMO("Roughness / Metallic / Occlusion", 2D) = "white" {}
		_RoughnessIntensity("Roughness intensity", Range(0, 1)) = 0.75
		_AmbientOcclusionIntensity("Ambient occlusion intensity", Range(0, 1)) = 1
		_SpecularIntensity("Specular intensity", Range(0, 1)) = 0.5
	}
		SubShader{
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        ZWrite On
        ZTest LEqual

		CGPROGRAM
		#include "UnityCG.cginc"
		#pragma target 5.0
		#pragma surface surf StandardSpecular

		sampler2D _MainTex;
		sampler2D _NormalMap;
		float _NormalIntensity;
		sampler2D _RMO;
		float _RoughnessIntensity;
		float _AmbientOcclusionIntensity;
		float _SpecularIntensity;
		float _Tilling;

		struct Input {
			float2 uv_MainTex : TEXCOORD0;
		};

	// Function to flatten normal
	float3 FlattenNormal(float3 normal, float intensity)
	{
		return lerp(normal, float3(0, 0, 1), intensity);
	}

	// Surface
	void surf(Input IN, inout SurfaceOutputStandardSpecular o) 
	{
		float2 tilling = mul(IN.uv_MainTex, _Tilling);

		float4 albedoMap = tex2D(_MainTex, tilling);
		float3 normalMap = UnpackNormal(tex2D(_NormalMap, tilling));
		float4 rmoMap = tex2D(_RMO, tilling);
		
		o.Albedo = albedoMap.rgb;
		o.Emission = float4(0, 0, 0, 1);
		o.Normal = FlattenNormal(normalMap, _NormalIntensity);
		o.Specular = _SpecularIntensity;
		o.Smoothness = 1 - clamp(mul(rmoMap.r, _RoughnessIntensity),0 , 1);
		o.Occlusion = pow(rmoMap.b, _AmbientOcclusionIntensity);
	}
	ENDCG
	}
	FallBack "Diffuse"
}