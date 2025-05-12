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
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "AseBlenderFuncs.cginc"

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

            float4 frag(Varyings input) : SV_Target
            {
                float4 background = tex2D(_Background, input.uv);
                float4 source = tex2D(_MainTex, input.uv);

                if (background.a == 0)
                {
                    return source * float4(1, 1, 1, _Opacity);
                }

                if (source.a == 0)
                {
                    return background;
                }

                if (_BlendMode == ASE_BLEND_MODE_Normal)
                {
                    return rgba_blender_normal(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Darken)
                {
                    return rgba_blender_darken(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Multiply)
                {
                    return rgba_blender_multiply(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_ColorBurn)
                {
                    return rgba_blender_color_burn(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Lighten)
                {
                    return rgba_blender_lighten(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Screen)
                {
                    return rgba_blender_screen(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_ColorDodge)
                {
                    return rgba_blender_color_dodge(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Addition)
                {
                    return rgba_blender_addition(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Overlay)
                {
                    return rgba_blender_overlay(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_SoftLight)
                {
                    return rgba_blender_soft_light(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_HardLight)
                {
                    return rgba_blender_hard_light(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Difference)
                {
                    return rgba_blender_difference(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Exclusion)
                {
                    return rgba_blender_exclusion(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Subtract)
                {
                    return rgba_blender_subtract(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Divide)
                {
                    return rgba_blender_divide(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Hue)
                {
                    return rgba_blender_hsl_hue(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Saturation)
                {
                    return rgba_blender_hsl_saturation(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Color)
                {
                    return rgba_blender_hsl_color(background, source, _Opacity);
                }
                if (_BlendMode == ASE_BLEND_MODE_Luminosity)
                {
                    return rgba_blender_hsl_luminosity(background, source, _Opacity);
                }

                // Hotpink if we got to here. We must have some unhandled blend mode.
                return float4(1, 0, 1, 1);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}