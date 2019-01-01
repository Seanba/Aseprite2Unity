// Conversion of aseprite/src/doc/blend_funcs.cpp
using System;
using color_t = System.UInt32;
using uint8_t = System.Byte;
using uint16_t = System.UInt16;
using uint32_t = System.UInt32;

using dc = Aseprite2Unity.Editor.DocColor;
using pc = Aseprite2Unity.Editor.PixmanCombine;

namespace Aseprite2Unity.Editor
{
    public static class Blender
    {
        public static uint8_t blend_multiply(uint8_t b, uint8_t s) => pc.MUL_UN8(b, s);
        public static uint8_t blend_screen(uint8_t b, uint8_t s) => (uint8_t)(b + s - pc.MUL_UN8(b, s));
        public static uint8_t blend_overlay(uint8_t b, uint8_t s) => blend_hard_light(s, b);
        public static uint8_t blend_darken(uint8_t b, uint8_t s) => Math.Min(b, s);
        public static uint8_t blend_lighten(uint8_t b, uint8_t s) => Math.Max(b, s);

        public static uint8_t blend_hard_light(uint8_t b, uint8_t s)
        {
            return s < 128 ? blend_multiply(b, (uint8_t)(s << 1)) : blend_screen(b, (uint8_t)((s << 1) - 255));
        }

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

        public static uint8_t blend_color_dodge(uint8_t b, uint8_t s)
        {
            if (b == 0)
                return 0;

            s = (uint8_t)(255 - s);
            if (b >= s)
                return 255;
            else
                return pc.DIV_UN8(b, s); // return b / (1-s)
        }

        public static uint8_t blend_color_burn(uint32_t b, uint32_t s)
        {
            if (b == 255)
                return 255;

            b = (255 - b);
            if (b >= s)
                return 0;
            else
                return (uint8_t)(255 - pc.DIV_UN8((uint8_t)b, (uint8_t)s)); // return 1 - ((1-b)/s)
        }

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

        // RGB blenders

        public static color_t rgba_blender_normal(color_t backdrop, color_t src, int opacity)
        {
            if ((backdrop & dc.rgba_a_mask) == 0)
            {
                uint32_t a = dc.rgba_geta(src);
                a = pc.MUL_UN8((uint8_t)a, (uint8_t)opacity);
                a <<= (int)dc.rgba_a_shift;
                return (src & dc.rgba_rgb_mask) | a;
            }
            else if ((src & dc.rgba_a_mask) == 0)
            {
                return backdrop;
            }

            int Br = dc.rgba_getr(backdrop);
            int Bg = dc.rgba_getg(backdrop);
            int Bb = dc.rgba_getb(backdrop);
            int Ba = dc.rgba_geta(backdrop);

            int Sr = dc.rgba_getr(src);
            int Sg = dc.rgba_getg(src);
            int Sb = dc.rgba_getb(src);
            int Sa = dc.rgba_geta(src);
            Sa = pc.MUL_UN8((byte)Sa, (byte)opacity);

            // Ra = Sa + Ba*(1-Sa)
            //    = Sa + Ba - Ba*Sa
            int Ra = Sa + Ba - pc.MUL_UN8((byte)Ba, (byte)Sa);

            // Ra = Sa + Ba*(1-Sa)
            // Ba = (Ra-Sa) / (1-Sa)
            // Rc = (Sc*Sa + Bc*Ba*(1-Sa)) / Ra                Replacing Ba with (Ra-Sa) / (1-Sa)...
            //    = (Sc*Sa + Bc*(Ra-Sa)/(1-Sa)*(1-Sa)) / Ra
            //    = (Sc*Sa + Bc*(Ra-Sa)) / Ra
            //    = Sc*Sa/Ra + Bc*Ra/Ra - Bc*Sa/Ra
            //    = Sc*Sa/Ra + Bc - Bc*Sa/Ra
            //    = Bc + (Sc-Bc)*Sa/Ra
            int Rr = Br + (Sr - Br) * Sa / Ra;
            int Rg = Bg + (Sg - Bg) * Sa / Ra;
            int Rb = Bb + (Sb - Bb) * Sa / Ra;

            return dc.rgba((uint32_t)Rr, (uint32_t)Rg, (uint32_t)Rb, (uint32_t)Ra);
        }

        public static color_t rgba_blender_multiply(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_multiply(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_multiply(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_multiply(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_screen(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_screen(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_screen(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_screen(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_overlay(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_overlay(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_overlay(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_overlay(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_darken(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_darken(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_darken(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_darken(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_lighten(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_lighten(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_lighten(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_lighten(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_color_dodge(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_color_dodge(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_color_dodge(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_color_dodge(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_color_burn(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_color_burn(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_color_burn(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_color_burn(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

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

        public static color_t rgba_blender_addition(color_t backdrop, color_t src, int opacity)
        {
            int r = dc.rgba_getr(backdrop) + dc.rgba_getr(src);
            int g = dc.rgba_getg(backdrop) + dc.rgba_getg(src);
            int b = dc.rgba_getb(backdrop) + dc.rgba_getb(src);
            src = dc.rgba((uint8_t)Math.Min(r, 255), (uint8_t)Math.Min(g, 255), (uint8_t)Math.Min(b, 255), 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

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
    }
}
