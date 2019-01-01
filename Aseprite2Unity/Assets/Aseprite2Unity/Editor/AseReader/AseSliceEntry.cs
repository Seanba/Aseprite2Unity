using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aseprite2Unity.Editor
{
    public class AseSliceEntry
    {
        public uint FrameNumber { get; private set; }
        public int OriginX { get; private set; }
        public int OriginY { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }

        public int CenterX { get; private set; }
        public int CenterY { get; private set; }
        public uint CenterWidth { get; private set; }
        public uint CenterHeight { get; private set; }

        public int PivotX { get; private set; }
        public int PivotY { get; private set; }

        public AseSliceEntry(AseReader reader, SliceFlags flags)
        {
            FrameNumber = reader.ReadDWORD();
            OriginX = reader.ReadLONG();
            OriginY = reader.ReadLONG();
            Width = reader.ReadDWORD();
            Height = reader.ReadDWORD();

            if ((flags & SliceFlags.Is9PatchSlice) != 0)
            {
                CenterX = reader.ReadLONG();
                CenterY = reader.ReadLONG();
                CenterWidth = reader.ReadDWORD();
                CenterHeight = reader.ReadDWORD();
            }

            if ((flags & SliceFlags.HasPivotInformation) != 0)
            {
                PivotX = reader.ReadLONG();
                PivotY = reader.ReadLONG();
            }
        }
    }
}
