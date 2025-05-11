#ifndef ASE_BLENDER_FUNCS_INCLUDED
#define ASE_BLENDER_FUNCS_INCLUDED

#define ASE_BLEND_MODE_Normal       0
#define ASE_BLEND_MODE_Multiply     1
#define ASE_BLEND_MODE_Screen       2
#define ASE_BLEND_MODE_Overlay      3
#define ASE_BLEND_MODE_Darken       4
#define ASE_BLEND_MODE_Lighten      5
#define ASE_BLEND_MODE_ColorDodge   6
#define ASE_BLEND_MODE_ColorBurn    7
#define ASE_BLEND_MODE_HardLight    8
#define ASE_BLEND_MODE_SoftLight    9
#define ASE_BLEND_MODE_Difference   10
#define ASE_BLEND_MODE_Exclusion    11
#define ASE_BLEND_MODE_Hue          12
#define ASE_BLEND_MODE_Saturation   13
#define ASE_BLEND_MODE_Color        14
#define ASE_BLEND_MODE_Luminosity   15
#define ASE_BLEND_MODE_Addition     16
#define ASE_BLEND_MODE_Subtract     17
#define ASE_BLEND_MODE_Divide       18


// fixit - need different blend modes
// Blend mode functions
// Taken from aseprite/src/doc/blend_funcs.cpp
float4 blend_mode_normal(float4 backdrop, float4 src, float opacity)
{
    if (backdrop.a == 0)
    {
        src.a *= opacity;
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
    float Sa = S.a * opacity;
    float Ra = Sa + Ba - (Ba * Sa);

    float3 R = B.rgb + (S.rgb - B.rgb) * (Sa / Ra);
    return float4(R, Ra);
}

#endif