namespace Aseprite2Unity.Editor
{
    public class AseLayerChunk : AseChunk
    {
        public override ChunkType ChunkType => ChunkType.Layer;

        public LayerChunkFlags Flags { get; }
        public LayerType Type { get; }
        public ushort ChildLevel { get; }
        public BlendMode BlendMode { get; }
        public byte Opacity { get; }
        public string Name { get; }
        public uint TilesetIndex { get; }

        public bool IsVisible => (Flags & LayerChunkFlags.Visible) != 0;
        public bool IsLockMovement => (Flags & LayerChunkFlags.LockMovement) != 0;

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

            if (Type == LayerType.Tilemap)
            {
                TilesetIndex = reader.ReadDWORD();
            }

            if (IsLockMovement)
            {
                // Todo Seanba: How should we treat UUIDs?
                reader.ReadBYTEs(16);
            }
        }

        public override void Visit(IAseVisitor visitor)
        {
            visitor.VisitLayerChunk(this);
        }
    }
}
