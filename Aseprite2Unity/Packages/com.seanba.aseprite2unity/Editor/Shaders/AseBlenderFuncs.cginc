#ifndef ASE_BLENDER_FUNCS_INCLUDED
#define ASE_BLENDER_FUNCS_INCLUDED


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