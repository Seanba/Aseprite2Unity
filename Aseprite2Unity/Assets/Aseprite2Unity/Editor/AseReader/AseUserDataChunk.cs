using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aseprite2Unity.Editor
{
    public class AseUserDataChunk : AseChunk
    {
        public override ChunkType ChunkType => ChunkType.UserData;

        public UserDataFlags Flags { get; private set; }
        public string Text { get; private set; }
        public byte[] ColorRGBA { get; private set; }

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
                reader.LastChunk.UserText = Text;
                reader.LastChunk.UserColor = ColorRGBA;
            }
        }

        public override void Visit(IAseVisitor visitor)
        {
            visitor.VisitUserDataChunk(this);
        }
    }
}
