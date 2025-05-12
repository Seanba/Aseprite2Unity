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

/*
public static uint8_t blend_difference(uint8_t b, uint8_t s) => (uint8_t)Math.Abs(b - s);

public static uint8_t blend_exclusion(uint8_t b, uint8_t s)
{
    int t = pc.MUL_UN8(b, s);
    return (uint8_t)(b + s - 2 * t);
}

public static uint8_t blend_divide(uint8_t b, uint8_t s)
{
    if (b == 0)
        return 0;
    else if (b >= s)
        return 255;
    else
        return pc.DIV_UN8(b, s); // return b / s
}
*/

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

/*
public static uint8_t blend_soft_light(uint32_t _b, uint32_t _s)
{
    double b = _b / 255.0;
    double s = _s / 255.0;
    double r, d;

    if (b <= 0.25)
        d = ((16 * b - 12) * b + 4) * b;
    else
        d = Math.Sqrt(b);

    if (s <= 0.5)
        r = b - (1.0 - 2.0 * s) * b * (1.0 - b);
    else
        r = b + (2.0 * s - 1.0) * (d - b);

    return (uint8_t)(r * 255 + 0.5);
}
*/

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

/*
public static color_t rgba_blender_hard_light(color_t backdrop, color_t src, int opacity)
{
    uint8_t r = blend_hard_light(dc.rgba_getr(backdrop), dc.rgba_getr(src));
    uint8_t g = blend_hard_light(dc.rgba_getg(backdrop), dc.rgba_getg(src));
    uint8_t b = blend_hard_light(dc.rgba_getb(backdrop), dc.rgba_getb(src));
    src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
    return rgba_blender_normal(backdrop, src, opacity);
}

public static color_t rgba_blender_soft_light(color_t backdrop, color_t src, int opacity)
{
    uint8_t r = blend_soft_light(dc.rgba_getr(backdrop), dc.rgba_getr(src));
    uint8_t g = blend_soft_light(dc.rgba_getg(backdrop), dc.rgba_getg(src));
    uint8_t b = blend_soft_light(dc.rgba_getb(backdrop), dc.rgba_getb(src));
    src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
    return rgba_blender_normal(backdrop, src, opacity);
}

public static color_t rgba_blender_difference(color_t backdrop, color_t src, int opacity)
{
    uint8_t r = blend_difference(dc.rgba_getr(backdrop), dc.rgba_getr(src));
    uint8_t g = blend_difference(dc.rgba_getg(backdrop), dc.rgba_getg(src));
    uint8_t b = blend_difference(dc.rgba_getb(backdrop), dc.rgba_getb(src));
    src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
    return rgba_blender_normal(backdrop, src, opacity);
}

public static color_t rgba_blender_exclusion(color_t backdrop, color_t src, int opacity)
{
    uint8_t r = blend_exclusion(dc.rgba_getr(backdrop), dc.rgba_getr(src));
    uint8_t g = blend_exclusion(dc.rgba_getg(backdrop), dc.rgba_getg(src));
    uint8_t b = blend_exclusion(dc.rgba_getb(backdrop), dc.rgba_getb(src));
    src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
    return rgba_blender_normal(backdrop, src, opacity);
}

// HSV blenders

private static double lum(double r, double g, double b)
{
    return (0.3 * r) + (0.59 * g) + (0.11 * b);
}

private static double sat(double r, double g, double b)
{
    return Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
}

private static void clip_color(ref double r, ref double g, ref double b)
{
    double l = lum(r, g, b);
    double n = Math.Min(r, Math.Min(g, b));
    double x = Math.Max(r, Math.Max(g, b));

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

private static void set_lum(ref double r, ref double g, ref double b, double l)
{
    double d = l - lum(r, g, b);
    r = r + d;
    g = g + d;
    b = b + d;
    clip_color(ref r, ref g, ref b);
}

// This stuff is such a dirty hack for the set_sat function
private class DoubleRef
{
    public double Value { get; set; }
}

private static DoubleRef REFMIN(DoubleRef x, DoubleRef y)
{
    return x.Value < y.Value ? x : y;
}

private static DoubleRef REFMAX(DoubleRef x, DoubleRef y)
{
    return x.Value > y.Value ? x : y;
}

private static DoubleRef REFMID(DoubleRef x, DoubleRef y, DoubleRef z)
{
    return REFMAX(x, REFMIN(y, z));
}

private static void set_sat(ref double _r, ref double _g, ref double _b, double s)
{
    DoubleRef r = new DoubleRef { Value = _r };
    DoubleRef g = new DoubleRef { Value = _g };
    DoubleRef b = new DoubleRef { Value = _b };

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

public static color_t rgba_blender_hsl_hue(color_t backdrop, color_t src, int opacity)
{
    double r = dc.rgba_getr(backdrop) / 255.0;
    double g = dc.rgba_getg(backdrop) / 255.0;
    double b = dc.rgba_getb(backdrop) / 255.0;
    double s = sat(r, g, b);
    double l = lum(r, g, b);

    r = dc.rgba_getr(src) / 255.0;
    g = dc.rgba_getg(src) / 255.0;
    b = dc.rgba_getb(src) / 255.0;

    set_sat(ref r, ref g, ref b, s);
    set_lum(ref r, ref g, ref b, l);

    src = dc.rgba((uint32_t)(255.0 * r), (uint32_t)(255.0 * g), (uint32_t)(255.0 * b), 0) | (src & dc.rgba_a_mask);
    return rgba_blender_normal(backdrop, src, opacity);
}

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

/*
public static color_t rgba_blender_subtract(color_t backdrop, color_t src, int opacity)
{
    int r = dc.rgba_getr(backdrop) - dc.rgba_getr(src);
    int g = dc.rgba_getg(backdrop) - dc.rgba_getg(src);
    int b = dc.rgba_getb(backdrop) - dc.rgba_getb(src);
    src = dc.rgba((uint8_t)Math.Max(r, 0), (uint8_t)Math.Max(g, 0), (uint8_t)Math.Max(b, 0), 0) | (src & dc.rgba_a_mask);
    return rgba_blender_normal(backdrop, src, opacity);
}

public static color_t rgba_blender_divide(color_t backdrop, color_t src, int opacity)
{
    uint8_t r = blend_divide(dc.rgba_getr(backdrop), dc.rgba_getr(src));
    uint8_t g = blend_divide(dc.rgba_getg(backdrop), dc.rgba_getg(src));
    uint8_t b = blend_divide(dc.rgba_getb(backdrop), dc.rgba_getb(src));
    src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
    return rgba_blender_normal(backdrop, src, opacity);
}
*/

#endif