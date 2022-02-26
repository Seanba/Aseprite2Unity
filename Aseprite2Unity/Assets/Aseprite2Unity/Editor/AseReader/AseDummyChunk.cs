namespace Aseprite2Unity.Editor
{
    // For chunks that are ignored (but still advance read pointer)
    public class AseDummyChunk : AseChunk
    {
        private ChunkType m_ChunkType;

        public override ChunkType ChunkType => m_ChunkType;

        public int ChunkSize { get; }
        public byte[] Bytes { get; }

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
