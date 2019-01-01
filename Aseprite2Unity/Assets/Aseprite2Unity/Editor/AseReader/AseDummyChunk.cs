using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aseprite2Unity.Editor
{
    // For chunks that are ignored (but still advance read pointer)
    public class AseDummyChunk : AseChunk
    {
        private ChunkType m_ChunkType;

        public override ChunkType ChunkType => m_ChunkType;

        public int ChunkSize { get; private set; }
        public byte[] Bytes { get; private set; }

        public AseDummyChunk(AseFrame frame, AseReader reader, ChunkType type, int size)
            : base(frame)
        {
            m_ChunkType = type;
            ChunkSize = size;
            Bytes = reader.ReadBYTEs(size);
        }

        public override void Visit(IAseVisitor visitor)
        {
            visitor.VisitDummyChunk(this);
        }
    }
}
