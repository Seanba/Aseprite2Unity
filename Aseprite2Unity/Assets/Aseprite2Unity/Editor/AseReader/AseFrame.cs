using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Aseprite2Unity.Editor
{
    public class AseFrame
    {
        public AseFile AseFile { get; private set; }

        public uint NumBytesInFrame { get; private set; }
        public ushort MagicNumber { get; private set; }
        public ushort NumChunksOld { get; private set; }
        public ushort FrameDurationMs { get; private set; }
        public uint NumChunksNew { get; private set; }

        public int NumChunksTotal {  get { return (int)(NumChunksOld + NumChunksNew); } }

        public List<AseChunk> Chunks { get; private set; }

        public AseFrame(AseFile file, AseReader reader)
        {
            AseFile = file;

            NumBytesInFrame = reader.ReadDWORD();
            MagicNumber = reader.ReadWORD();
            NumChunksOld = reader.ReadWORD();
            FrameDurationMs = reader.ReadWORD();

            // Ingore next two bytes
            reader.ReadBYTEs(2);

            NumChunksNew = reader.ReadDWORD();

            // Read in old and new chunks
            Chunks = Enumerable.Repeat<AseChunk>(null, NumChunksTotal).ToList();
            for (int i = 0; i < NumChunksTotal; i++)
            {
                Chunks[i] = ReadChunk(reader);
            }

            Debug.Assert(MagicNumber == 0xF1FA);
        }

        private AseChunk ReadChunk(AseReader reader)
        {
            uint size = reader.ReadDWORD();
            ChunkType type = (ChunkType)reader.ReadWORD();
            return ChunkFactory.ReadChunk(this, type, (int)(size - 6), reader);
        }
    }
}
