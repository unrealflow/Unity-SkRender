Shader "SkRender/Unlit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Intensity("Emissive Intensity", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Common.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };
            float4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Intensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = _Color*tex2D(_MainTex, i.uv)*_Intensity;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDHLSL
        }
    }
    SubShader
    {
        Pass
        {
            Name "RayTracing"
            Tags {"LightMode"="RayTracing"}

            HLSLPROGRAM
            #pragma raytracing test
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            //#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            //#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "RayCommon.hlsl"

            float4 _Color;
            Texture2D _MainTex;
            float _Intensity;

            [shader("closesthit")]
            void ClosestHitShader(inout RP rp : SV_RayPayload, AttributeData attribs: SV_IntersectionAttributes)
            {
                Vertex v=FetchFrag(PrimitiveIndex(),attribs);
                rp.color=_Color*_MainTex.SampleLevel(s_linear_clamp_sampler, v.uv,0.0)*_Intensity;
                rp.kS=-1.0;
            }
            ENDHLSL
        }
    }
}
