Shader "Hidden/Aseprite2Unity/AsepriteCelBlitter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Background ("Texture", 2D) = "white" {}
        _Opacity ("Opacity", Float) = 1.0
        _BlendMode ("BlenMode", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Blend Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _Background;
            float4 _Background_ST;

            float _Opacity;
            int _BlendMode;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionHCS = UnityObjectToClipPos(input.positionOS);
                return output;
            }

            // fixit - need different blend modes
            // Blend mode functions
            // Taken from aseprite/src/doc/blend_funcs.cpp
            float4 blend_mode_normal(float4 backdrop, float4 src)
            {
                if (backdrop.a == 0)
                {
                    src.a *= _Opacity;
                    return src;
                }
                else if (src.a == 0)
                {
                    return backdrop;
                }

                // Note: Variable names are chosen to match source C++
                float4 B = backdrop;
                float4 S = src;

                float Ba = B.a;
                float Sa = S.a * _Opacity;
                float Ra = Sa + Ba - (Ba * Sa);

                float3 R = B.rgb + (S.rgb - B.rgb) * (Sa / Ra);
                return float4(R, Ra);
            }


            float4 frag(Varyings input) : SV_Target
            {
                float4 background = tex2D(_Background, input.uv);
                float4 source = tex2D(_MainTex, input.uv);
                return blend_mode_normal(background, source);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}