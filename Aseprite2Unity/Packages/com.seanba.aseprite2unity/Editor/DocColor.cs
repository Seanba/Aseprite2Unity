// Conversion of aseprite/src/doc/color.h
using uint8_t = System.Byte;
using uint16_t = System.UInt16;
using uint32_t = System.UInt32;

namespace Aseprite2Unity.Editor
{
    public static class DocColor
    {
        public const uint32_t rgba_r_shift = 0;
        public const uint32_t rgba_g_shift = 8;
        public const uint32_t rgba_b_shift = 16;
        public const uint32_t rgba_a_shift = 24;

        public const uint32_t rgba_r_mask = 0x000000ff;
        public const uint32_t rgba_g_mask = 0x0000ff00;
        public const uint32_t rgba_b_mask = 0x00ff0000;
        public const uint32_t rgba_rgb_mask = 0x00ffffff;
        public const uint32_t rgba_a_mask = 0xff000000;

        public static uint8_t rgba_getr(uint32_t c)
        {
            return (uint8_t)((c >> (int)(rgba_r_shift)) & 0xff);
        }

        public static uint8_t rgba_getg(uint32_t c)
        {
            return (uint8_t)((c >> (int)rgba_g_shift) & 0xff);
        }

        public static uint8_t rgba_getb(uint32_t c)
        {
            return (uint8_t)((c >> (int)rgba_b_shift) & 0xff);
        }

        public static uint8_t rgba_geta(uint32_t c)
        {
            return (uint8_t)((c >> (int)rgba_a_shift) & 0xff);
        }

        public static uint32_t rgba(uint32_t r, uint32_t g, uint32_t b, uint32_t a)
        {
            return ((r << (int)rgba_r_shift) |
                    (g << (int)rgba_g_shift) |
                    (b << (int)rgba_b_shift) |
                    (a << (int)rgba_a_shift));
        }

        public static int rgb_luma(int r, int g, int b)
        {
            return (r * 2126 + g * 7152 + b * 722) / 10000;
        }

        public static uint8_t rgba_luma(uint32_t c)
        {
            return (uint8_t)rgb_luma(rgba_getr(c), rgba_getg(c), rgba_getb(c));
        }

        //////////////////////////////////////////////////////////////////////
        // Grayscale

        const uint16_t graya_v_shift = 0;
        const uint16_t graya_a_shift = 8;

        const uint16_t graya_v_mask = 0x00ff;
        const uint16_t graya_a_mask = 0xff00;

        public static uint8_t graya_getv(uint16_t c)
        {
            return (uint8_t)((c >> graya_v_shift) & 0xff);
        }

        public static uint8_t graya_geta(uint16_t c)
        {
            return (uint8_t)((c >> graya_a_shift) & 0xff);
        }

        public static uint16_t graya(uint8_t v, uint8_t a)
        {
            return (uint16_t)((v << graya_v_shift) | (a << graya_a_shift));
        }

        public static uint16_t gray(uint8_t v)
        {
            return graya(v, 255);
        }
    }
}
