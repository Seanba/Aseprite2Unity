using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Aseprite2Unity.Editor
{
    public abstract class AseChunk
    {
        public abstract ChunkType ChunkType { get; }

        public AseFrame Frame { get; private set; }
        public string UserText { get; set; }
        public byte[] UserColor { get; set; }

        protected AseChunk(AseFrame frame)
        {
            Frame = frame;
        }

        public abstract void Visit(IAseVisitor visitor);
    }
}
