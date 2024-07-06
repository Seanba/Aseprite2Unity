// This is a rough C# wrapper of Pixman's blending functions
// Pixman is provided by the MIT license

using uint8_t = System.Byte;
using uint16_t = System.UInt16;

namespace Aseprite2Unity.Editor
{
    public static class PixmanCombine
    {
        public const uint COMPONENT_SIZE = 8;
        public const byte MASK = 0xff;
        public const byte ONE_HALF = 0x80;

        public const byte A_SHIFT = 8 * 3;
        public const byte R_SHIFT = 8 * 2;
        public const byte G_SHIFT = 8;
        public const uint A_MASK = 0xff000000;
        public const uint R_MASK = 0xff0000;
        public const uint G_MASK = 0xff00;

        public const uint RB_MASK = 0xff00ff;
        public const uint AG_MASK = 0xff00ff00;
        public const uint RB_ONE_HALF = 0x800080;
        public const uint RB_MASK_PLUS_ONE = 0x10000100;

        static uint ALPHA_8(uint x) => ((x) >> A_SHIFT);
        static uint RED_8(uint x) => (((x) >> R_SHIFT) & MASK);
        static uint GREEN_8(uint x) => (((x) >> G_SHIFT) & MASK);
        static uint BLUE_8(uint x) => ((x) & MASK);

        // Helper "macros"
        public static byte MUL_UN8(uint8_t a, uint8_t b)
        {
            int t = a * b + ONE_HALF;
            return (uint8_t)(((t >> G_SHIFT) + (t)) >> G_SHIFT);
        }

        public static uint8_t DIV_UN8(uint8_t a, uint8_t b)
        {
            return (uint8_t)(((uint16_t)(a) * MASK + ((b) / 2)) / (b));
        }
    }
}
