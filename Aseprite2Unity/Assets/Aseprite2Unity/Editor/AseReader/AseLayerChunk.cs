using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aseprite2Unity.Editor
{
    public class AseLayerChunk : AseChunk
    {
        public override ChunkType ChunkType => ChunkType.Layer;

        public LayerChunkFlags Flags { get; private set; }
        public LayerType Type { get; private set; }
        public ushort ChildLevel { get; private set; }
        public BlendMode BlendMode { get; private set; }
        public byte Opacity { get; private set; }
        public string Name { get; private set; }

        public bool IsVisible => (Flags & LayerChunkFlags.Visible) != 0;

        public AseLayerChunk(AseFrame frame, AseReader reader)
            : base(frame)
        {
            Flags = (LayerChunkFlags)reader.ReadWORD();
            Type = (LayerType)reader.ReadWORD();
            ChildLevel = reader.ReadWORD();

            // Ignore next two words
            reader.ReadWORD();
            reader.ReadWORD();

            BlendMode = (BlendMode)reader.ReadWORD();
            Opacity = reader.ReadBYTE();

            if ((frame.AseFile.Header.Flags & HeaderFlags.HasLayerOpacity) == 0)
            {
                // Assume full opacity
                Opacity = 255;
            }

            // Ignore next three bytes
            reader.ReadBYTEs(3);

            Name = reader.ReadSTRING();
        }

        public override void Visit(IAseVisitor visitor)
        {
            visitor.VisitLayerChunk(this);
        }
    }
}
