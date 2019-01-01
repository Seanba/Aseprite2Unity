using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Aseprite2Unity.Editor
{
    public class AseHeader
    {
        public uint FileSize { get; private set; }
        public ushort MagicNumber { get; private set; }
        public ushort NumFrames { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public ColorDepth ColorDepth { get; private set; }
        public HeaderFlags Flags { get; private set; }
        public ushort Speed { get; private set; }
        public byte TransparentIndex { get; private set; }
        public ushort NumColors { get; private set; }
        public byte PixelWidth { get; private set; }
        public byte PixelHeight { get; private set; }

        public AseHeader(AseReader reader)
        {
            FileSize = reader.ReadDWORD();
            MagicNumber = reader.ReadWORD();
            NumFrames = reader.ReadWORD();
            Width = reader.ReadWORD();
            Height = reader.ReadWORD();
            ColorDepth = reader.ReadColorDepth();
            Flags = (HeaderFlags)reader.ReadDWORD();
            Speed = reader.ReadWORD();

            // Next two dwords are ignored
            reader.ReadDWORD();
            reader.ReadDWORD();

            TransparentIndex = reader.ReadBYTE();

            // Next 3 bytes are ignored
            reader.ReadBYTEs(3);

            NumColors = reader.ReadWORD();
            PixelWidth = reader.ReadBYTE();
            PixelHeight = reader.ReadBYTE();

            // Last 92 bytes are reserved for future use
            reader.ReadBYTEs(92);

            Debug.Assert(MagicNumber == 0xA5E0);
        }
    }
}
