using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aseprite2Unity.Editor
{
    public class AseFrameTagsChunk : AseChunk
    {
        public override ChunkType ChunkType => ChunkType.FrameTags;

        public ushort NumTags { get; private set; }
        public List<AseFrameTagEntry> Entries { get; private set; }

        public AseFrameTagsChunk(AseFrame frame, AseReader reader)
            : base(frame)
        {
            NumTags = reader.ReadWORD();

            // Ignore next 8 bytes
            reader.ReadBYTEs(8);

            Entries = Enumerable.Repeat<AseFrameTagEntry>(null, NumTags).ToList();
            for (int i = 0; i < (int)NumTags; i++)
            {
                Entries[i] = new AseFrameTagEntry(reader);
            }
        }

        public override void Visit(IAseVisitor visitor)
        {
            visitor.VisitFrameTagsChunk(this);
        }
    }
}
