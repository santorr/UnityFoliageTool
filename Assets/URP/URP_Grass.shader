Shader "Foliage/URP_Grass" 
{
    Properties
    { 
        [NoScaleOffset] _MainTex("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}
		_NormalIntensity("Normal intensity", Range(0, 10)) = 1
		[NoScaleOffset] _RMO("Roughness / Metallic / Occlusion", 2D) = "white" {}
		_RoughnessIntensity("Roughness intensity", Range(0, 1)) = 0.75
		_SpecularIntensity("Specular intensity", Range(0, 1)) = 0.5
		// _SubsurfaceAmount("Subsurface amount", Range(0, 5)) = 1
		_CullingDistance("Culling distance", Range(0, 100)) = 15
		_CullingAngle("Culling angle", Range(0, 180)) = 90
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Geometry"
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalRenderPipeline"
            // "LightMode" = "UniversalGBuffer"
            "LightMode" = "UniversalForward"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }

        LOD 300
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Deferred.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 vertex : POSITION;
                float4 color: COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };       

            sampler2D _MainTex;
            float4 _MainTex_ST;
		    sampler2D _NormalMap;
            float4 _NormalMap_ST;
		    float _NormalIntensity;
		    sampler2D _RMO;
            float4 _RMO_ST;
		    float _RoughnessIntensity;
		    float _SpecularIntensity;
		    float _CullingDistance;
		    float _CullingAngle;

            UNITY_INSTANCING_BUFFER_START(MyProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(MyProps)

            // Vertex shader
            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Fragment shader
            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 col = tex2D(_MainTex, i.uv);
                clip(-(0.5 - col.a));
                // float4 color = UNITY_ACCESS_INSTANCED_PROP(MyProps, _Color);
                return col;
            }
            ENDHLSL
        }
    }
}