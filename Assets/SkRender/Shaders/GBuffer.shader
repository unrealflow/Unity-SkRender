Shader "SkRender/GBuffer"
{
    Properties
    {
        _BaseColor ("_BaseColor", Color) = (1,1,1,1)
        _BaseColorMap ("_BaseColorMap (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Common.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal :NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normal:COLOR0;
                float3 w_pos:COLOR1;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };
            struct GBuffer
            {
                float4 position : SV_TARGET0;
                float4 normal : SV_TARGET1;
                float4 albedo : SV_TARGET2;
            };
            float4 _BaseColor;
            sampler2D _BaseColorMap;
            float4 _BaseColorMap_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.w_pos=mul(unity_ObjectToWorld,float4(v.vertex.xyz,1.0));
                o.vertex=mul(_View,float4(o.w_pos,1.0));

                o.vertex =mul(_JitterProj,o.vertex);
                
                o.vertex.y=-o.vertex.y;
                // o.vertex =mul(UNITY_MATRIX_VP,float4(o.w_pos,1.0));
                // o.vertex=UnityWorldToClipPos(o.w_pos);
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _BaseColorMap);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            GBuffer frag (v2f i) 
            {
                // sample the texture
                GBuffer g;
                g.albedo = _BaseColor*float4(tex2D(_BaseColorMap, i.uv).xyz,1.0);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, g.albedo);
                g.normal=float4(i.normal,0.0);
                g.position=float4(i.w_pos,1.0);

                return g;
            }
            ENDCG
        }
    }
}
