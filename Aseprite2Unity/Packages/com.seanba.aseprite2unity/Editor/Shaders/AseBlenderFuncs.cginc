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

// Conversion of aseprite/src/doc/blend_funcs.cpp
// https://github.com/aseprite/aseprite/blob/main/src/doc/blend_funcs.cpp
// The methods and variables below are made to closely match source C++
// Order should be similar too. It's not perfect but it does the job.

// Forward declares
float blend_hard_light(float s, float b);

float3 blend_multiply(float3 b, float3 s)
{
    return b * s;
}

float3 blend_screen(float3 b, float3 s)
{
    return b + s - b * s;
}

float blend_overlay(float b, float s)
{
    return blend_hard_light(s, b);
}

float3 blend_darken(float3 b, float3 s)
{
    return min(b, s);
}

float3 blend_lighten(float3 b, float3 s)
{
    return max(b, s);
}

float blend_hard_light(float b, float s)
{
    s = s < 0.5 ? blend_multiply(b, s * 2 ) : blend_screen(b, (2 * s) - 1);
    return saturate(s);
}


float3 blend_difference(float3 b, float3 s)
{
    return abs(b - s);
}

float blend_exclusion(float b, float s)
{
    float t = b * s;
    return b + s - 2 * t;
}

float blend_divide(float b, float s)
{
    if (b == 0)
        return 0;
    else if (b >= s)
        return 1;
    else
        return b / s;
}

float blend_color_dodge(float b, float s)
{
    if (b == 0)
        return 0;

    s = 1 - s;
    if (b >= s)
        return 1;
    else
        return b / s;
}

float blend_color_burn(float b, float s)
{
    if (b == 1)
        return 1;

    b = (1 - b);
    if (b >= s)
        return 0;
    else
        return 1 - b/s;
}

float blend_soft_light(float b, float s)
{
    float r, d;

    if (b <= 0.25)
        d = ((16 * b - 12) * b + 4) * b;
    else
        d = sqrt(b);

    if (s <= 0.5)
        r = b - (1.0 - 2.0 * s) * b * (1.0 - b);
    else
        r = b + (2.0 * s - 1.0) * (d - b);

    return saturate(r);
}

// RGB blenders

float4 rgba_blender_normal(float4 backdrop, float4 src, float opacity)
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

    float4 B = backdrop;
    float4 S = src;

    float Ba = B.a;
    float Sa = S.a * opacity;
    float Ra = Sa + Ba - (Ba * Sa);

    float3 R = B.rgb + (S.rgb - B.rgb) * (Sa / Ra);
    return float4(R, Ra);
}

float4 rgba_blender_multiply(float4 backdrop, float4 src, float opacity)
{
    float3 rgb = blend_multiply(backdrop.rgb, src.rgb);
    src = float4(rgb, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

float4 rgba_blender_screen(float4 backdrop, float4 src, float opacity)
{
    float3 rgb = blend_screen(backdrop.rgb, src.rgb);
    src = float4(rgb, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

float4 rgba_blender_overlay(float4 backdrop, float4 src, float opacity)
{
    float r = blend_overlay(backdrop.r, src.r);
    float g = blend_overlay(backdrop.g, src.g);
    float b = blend_overlay(backdrop.b, src.b);
    src = float4(r, g, b, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

float4 rgba_blender_darken(float4 backdrop, float4 src, float opacity)
{
    float3 rgb = blend_darken(backdrop.rgb, src.rgb);
    src = float4(rgb, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

float4 rgba_blender_lighten(float4 backdrop, float4 src, float opacity)
{
    float3 rgb = blend_lighten(backdrop.rgb, src.rgb);
    src = float4(rgb, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

float4 rgba_blender_color_dodge(float4 backdrop, float4 src, float opacity)
{
    float r = blend_color_dodge(backdrop.r, src.r);
    float g = blend_color_dodge(backdrop.g, src.g);
    float b = blend_color_dodge(backdrop.b, src.b);
    src = float4(r, g, b, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

float4 rgba_blender_color_burn(float4 backdrop, float4 src, float opacity)
{
    float r = blend_color_burn(backdrop.r, src.r);
    float g = blend_color_burn(backdrop.g, src.g);
    float b = blend_color_burn(backdrop.b, src.b);
    src = float4(r, g, b, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

float4 rgba_blender_hard_light(float4 backdrop, float4 src, float opacity)
{
    float r = blend_hard_light(backdrop.r, src.r);
    float g = blend_hard_light(backdrop.g, src.g);
    float b = blend_hard_light(backdrop.b, src.b);
    src = float4(r, g, b, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

float4 rgba_blender_soft_light(float4 backdrop, float4 src, float opacity)
{
    float r = blend_soft_light(backdrop.r, src.r);
    float g = blend_soft_light(backdrop.g, src.g);
    float b = blend_soft_light(backdrop.b, src.b);
    src = float4(r, g, b, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

float4 rgba_blender_difference(float4 backdrop, float4 src, float opacity)
{
    float r = blend_difference(backdrop.r, src.r);
    float g = blend_difference(backdrop.g, src.g);
    float b = blend_difference(backdrop.b, src.b);
    src = float4(r, g, b, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

float4 rgba_blender_exclusion(float4 backdrop, float4 src, float opacity)
{
    float r = blend_exclusion(backdrop.r, src.r);
    float g = blend_exclusion(backdrop.g, src.g);
    float b = blend_exclusion(backdrop.b, src.b);
    src = float4(r, g, b, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

// HSV blenders

double lum(double r, double g, double b)
{
    return (0.3 * r) + (0.59 * g) + (0.11 * b);
}

double sat(double r, double g, double b)
{
    return max(r, max(g, b)) - min(r, min(g, b));
}

void clip_color(inout double r, inout double g, inout double b)
{
    double l = lum(r, g, b);
    double n = min(r, min(g, b));
    double x = max(r, max(g, b));

    if (n < 0)
    {
        r = l + (((r - l) * l) / (l - n));
        g = l + (((g - l) * l) / (l - n));
        b = l + (((b - l) * l) / (l - n));
    }

    if (x > 1)
    {
        r = l + (((r - l) * (1 - l)) / (x - l));
        g = l + (((g - l) * (1 - l)) / (x - l));
        b = l + (((b - l) * (1 - l)) / (x - l));
    }
}

void set_lum(inout double r, inout double g, inout double b, double l)
{
    double d = l - lum(r, g, b);
    r = r + d;
    g = g + d;
    b = b + d;
    clip_color(r, g, b);
}

// This stuff is such a dirty hack for the set_sat function
struct DoubleRef
{
    double Value;
};


DoubleRef REFMIN(DoubleRef x, DoubleRef y)
{
    if (x.Value < y.Value)
    {
        return x;
    }

    return y;
}

DoubleRef REFMAX(DoubleRef x, DoubleRef y)
{
    if (x.Value > y.Value)
    {
        return x;
    }

    return y;
}

DoubleRef REFMID(DoubleRef x, DoubleRef y, DoubleRef z)
{
    return REFMAX(x, REFMIN(y, z));
}

void set_sat(inout double _r, inout double _g, inout double _b, double s)
{
    DoubleRef r;
    r.Value = _r;

    DoubleRef g;
    g.Value = _g;

    DoubleRef b;
    b.Value = _b;

    DoubleRef min = REFMIN(r, REFMIN(g, b));
    DoubleRef mid = REFMID(r, g, b);
    DoubleRef max = REFMAX(r, REFMAX(g, b));

    if (max.Value > min.Value)
    {
        mid.Value = ((mid.Value - min.Value) * s) / (max.Value - min.Value);
        max.Value = s;
    }
    else
    {
        mid.Value = 0;
        max.Value = 0;
    }

    min.Value = 0;

    _r = r.Value;
    _g = g.Value;
    _b = b.Value;
}

float4 rgba_blender_hsl_hue(float4 backdrop, float4 src, float opacity)
{
    double r = backdrop.r;
    double g = backdrop.g;
    double b = backdrop.b;
    double s = sat(r, g, b);
    double l = lum(r, g, b);

    r = src.r;
    g = src.g;
    b = src.b;

    set_sat(r, g, b, s);
    set_lum(r, g, b, l);

    src = float4(r, g, b, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

/*
public static color_t rgba_blender_hsl_saturation(color_t backdrop, color_t src, int opacity)
{
    double r = dc.rgba_getr(src) / 255.0;
    double g = dc.rgba_getg(src) / 255.0;
    double b = dc.rgba_getb(src) / 255.0;
    double s = sat(r, g, b);

    r = dc.rgba_getr(backdrop) / 255.0;
    g = dc.rgba_getg(backdrop) / 255.0;
    b = dc.rgba_getb(backdrop) / 255.0;
    double l = lum(r, g, b);

    set_sat(ref r, ref g, ref b, s);
    set_lum(ref r, ref g, ref b, l);

    src = dc.rgba((uint32_t)(255.0 * r), (uint32_t)(255.0 * g), (uint32_t)(255.0 * b), 0) | (src & dc.rgba_a_mask);
    return rgba_blender_normal(backdrop, src, opacity);
}

public static color_t rgba_blender_hsl_color(color_t backdrop, color_t src, int opacity)
{
    double r = dc.rgba_getr(backdrop) / 255.0;
    double g = dc.rgba_getg(backdrop) / 255.0;
    double b = dc.rgba_getb(backdrop) / 255.0;
    double l = lum(r, g, b);

    r = dc.rgba_getr(src) / 255.0;
    g = dc.rgba_getg(src) / 255.0;
    b = dc.rgba_getb(src) / 255.0;

    set_lum(ref r, ref g, ref b, l);

    src = dc.rgba((uint32_t)(255.0 * r), (uint32_t)(255.0 * g), (uint32_t)(255.0 * b), 0) | (src & dc.rgba_a_mask);
    return rgba_blender_normal(backdrop, src, opacity);
}

public static color_t rgba_blender_hsl_luminosity(color_t backdrop, color_t src, int opacity)
{
    double r = dc.rgba_getr(src) / 255.0;
    double g = dc.rgba_getg(src) / 255.0;
    double b = dc.rgba_getb(src) / 255.0;
    double l = lum(r, g, b);

    r = dc.rgba_getr(backdrop) / 255.0;
    g = dc.rgba_getg(backdrop) / 255.0;
    b = dc.rgba_getb(backdrop) / 255.0;

    set_lum(ref r, ref g, ref b, l);

    src = dc.rgba((uint32_t)(255.0 * r), (uint32_t)(255.0 * g), (uint32_t)(255.0 * b), 0) | (src & dc.rgba_a_mask);
    return rgba_blender_normal(backdrop, src, opacity);
}
*/

float4 rgba_blender_addition(float4 backdrop, float4 src, float opacity)
{
    float3 rgb = saturate(backdrop.rgb + src.rgb);
    src = float4(rgb, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

float4 rgba_blender_subtract(float4 backdrop, float4 src, float opacity)
{
    float3 rgb = saturate(backdrop.rgb - src.rgb);
    src = float4(rgb, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

float4 rgba_blender_divide(float4 backdrop, float4 src, float opacity)
{
    float r = blend_divide(backdrop.r, src.r);
    float g = blend_divide(backdrop.g, src.g);
    float b = blend_divide(backdrop.b, src.b);
    src = float4(r, g, b, src.a);
    return rgba_blender_normal(backdrop, src, opacity);
}

#endif