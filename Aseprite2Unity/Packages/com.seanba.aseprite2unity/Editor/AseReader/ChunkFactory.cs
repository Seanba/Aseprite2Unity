﻿using UnityEngine;
using UnityEngine.Assertions;

namespace Aseprite2Unity.Editor
{
    public static class ChunkFactory
    {
        public static AseChunk ReadChunk(AseFrame frame, ChunkType type, int size, AseReader reader)
        {
            var pos = reader.Position;
            AseChunk chunk;

            switch (type)
            {
                // Chunk types we care about
                case ChunkType.Palette:
                    chunk = new AsePaletteChunk(frame, reader);
                    break;

                case ChunkType.Layer:
                    chunk = new AseLayerChunk(frame, reader);
                    break;

                case ChunkType.Cel:
                    chunk = new AseCelChunk(frame, reader, size);
                    break;

                case ChunkType.ColorProfile:
                    chunk = new AseColorProfileChunk(frame, reader);
                    break;

                case ChunkType.FrameTags:
                    chunk = new AseFrameTagsChunk(frame, reader);
                    break;

                case ChunkType.Slice:
                    chunk = new AseSliceChunk(frame, reader);
                    break;

                case ChunkType.UserData:
                    chunk = new AseUserDataChunk(frame, reader);
                    break;

                // Chunk types we don't care about
                case ChunkType.OldPalette:
                    chunk = new AseDummyChunk(frame, reader, type, size);
                    break;

                // Chunk types we haven't handled yet. Indicates a bug that should be fixed.
                default:
                    chunk = new AseDummyChunk(frame, reader, type, size);
                    Debug.LogErrorFormat("Unhandled chunk type: {0}", ((ushort)type).ToString("X4"));
                    break;
            }

            // Check that we read the right amount of bytes
            Assert.IsTrue((reader.Position - pos) == size, string.Format("Chunk {0} read {1} bytes but we were expected {2} bytes read", type, reader.Position - pos, size));
            reader.LastChunk = chunk;

            return chunk;
        }
    }
}
