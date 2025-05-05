namespace Aseprite2Unity.Editor
{
    public class AseTilesetChunk : AseChunk
    {
        public override ChunkType ChunkType => ChunkType.Tileset;

        public uint TilesetId { get; }
        public uint TilesetFlags { get; }
        public uint NumberOfTiles { get; }
        public ushort TileWidth { get; }
        public ushort TileHeight { get; }
        public short BaseIndex { get; }
        public string TilesetName { get; }

        public byte[] PixelData { get; }

        public AseTilesetChunk(AseFrame frame, AseReader reader) : base(frame)
        {
            TilesetId = reader.ReadDWORD();
            TilesetFlags = reader.ReadDWORD();
            NumberOfTiles = reader.ReadDWORD();
            TileWidth = reader.ReadWORD();
            TileHeight = reader.ReadWORD();
            BaseIndex = reader.ReadSHORT();

            // Reserved
            reader.ReadBYTEs(14);

            TilesetName = reader.ReadSTRING();

            if (IsBitSet(TilesetFlags, 0))
            {
                reader.ReadDWORD(); // Id of external file
                reader.ReadDWORD(); // tileset id in the external file
            }

            if (IsBitSet(TilesetFlags, 1))
            {
                var dataLength = reader.ReadDWORD();
                var compressed = reader.ReadBYTEs((int)dataLength);
                PixelData = AseCelChunk.ZlibDeflate(compressed);
            }
        }

        public override void Visit(IAseVisitor visitor)
        {
            visitor.VisitTilesetChunk(this);
        }

        private bool IsBitSet(uint flags, int pos)
        {
            return (flags & (1 << pos)) != 0;
        }
    }
}
