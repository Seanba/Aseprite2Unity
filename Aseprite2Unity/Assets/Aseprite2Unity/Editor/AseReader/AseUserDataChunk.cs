namespace Aseprite2Unity.Editor
{
    public class AseUserDataChunk : AseChunk
    {
        public override ChunkType ChunkType => ChunkType.UserData;

        public UserDataFlags Flags { get; }
        public string Text { get; }
        public byte[] ColorRGBA { get; }

        public AseUserDataChunk(AseFrame frame, AseReader reader)
            : base(frame)
        {
            Flags = (UserDataFlags)reader.ReadDWORD();
            
            if ((Flags & UserDataFlags.HasText) != 0)
            {
                Text = reader.ReadSTRING();
            }

            if ((Flags & UserDataFlags.HasColor) != 0)
            {
                ColorRGBA = reader.ReadBYTEs(4);
            }

            // Place the user data in the last chunk
            if (reader.LastChunk != null)
            {
                reader.LastChunk.UserDataText = Text;
                reader.LastChunk.UserDataColor = ColorRGBA;
            }
        }

        public override void Visit(IAseVisitor visitor)
        {
            visitor.VisitUserDataChunk(this);
        }
    }
}
