using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    // Traverse an Aseprite file creating Unity objects as you go
    internal class AseUnityObjects : IAseVisitor, IDisposable
    {
        public int CanvasWidth => m_AseFile.Header.Width;
        public int CanvasHeight => m_AseFile.Header.Height;
        public ColorDepth ColorDepth => m_AseFile.Header.ColorDepth;

        private AseFile m_AseFile;
        private readonly Stack<AseCanvas> m_FrameCanvases = new Stack<AseCanvas>();
        private readonly List<AseLayerChunk> m_LayerChunks = new List<AseLayerChunk>();
        private readonly List<Color32> m_Palette = new List<Color32>();

        // It is the responsibility of the caller to manage these textures
        public IEnumerable<Texture2D> FetchFrameTextures()
        {
            foreach (var canvas in m_FrameCanvases)
            {
                yield return canvas.ToTexture2D();
            }
        }

        public void Dispose()
        {
            foreach (var canvas in m_FrameCanvases)
            {
                canvas.Dispose();
            }
        }

        public void BeginFileVisit(AseFile file)
        {
            m_AseFile = file;
        }

        public void EndFileVisit(AseFile file)
        {
            m_AseFile = null;
        }

        public void BeginFrameVisit(AseFrame frame)
        {
            // Create a new blank canvas to be written to for the frame
            m_FrameCanvases.Push(new AseCanvas(CanvasWidth, CanvasHeight));
        }

        public void EndFrameVisit(AseFrame frame)
        {
            // Any cleanup needed?
        }

        public void VisitLayerChunk(AseLayerChunk layer)
        {
            ReportCallerMemberName();

            m_LayerChunks.Add(layer);
        }

        public void VisitCelChunk(AseCelChunk cel)
        {
            ReportCallerMemberName();

            int layerIndex = cel.LayerIndex;

            if (cel.LinkedCel != null)
            {
                cel = cel.LinkedCel;
            }

            // fixit - test grayscale images
            // fixit - test palette images
            // fixit - test raw image data
            // fixit - test tiles
            if (cel.CelType == CelType.CompressedImage)
            {
                // fixit:left off here
                //cel.PositionX;
                //cel.PositionY;
                //cel.Width
                //cel.Height;
                //cel.PixelBytes
            }
        }

        public void VisitDummyChunk(AseDummyChunk dummy)
        {
            // Ignore dummy chunks
        }

        public void VisitFrameTagsChunk(AseFrameTagsChunk frameTags)
        {
            ReportCallerMemberName();
        }

        public void VisitOldPaletteChunk(AseOldPaletteChunk palette)
        {
            m_Palette.Clear();
            m_Palette.AddRange(palette.Colors.Select(c => new Color32(c.red, c.green, c.blue, 255)));
        }

        public void VisitPaletteChunk(AsePaletteChunk palette)
        {
            // fixit - test this. Need 256+ colors or a palette with alpha
        }

        public void VisitSliceChunk(AseSliceChunk slice)
        {
            ReportCallerMemberName();
        }

        public void VisitTilesetChunk(AseTilesetChunk tilset)
        {
            // fixit - keep track of the tiles
            ReportCallerMemberName();
        }

        public void VisitUserDataChunk(AseUserDataChunk userData)
        {
            ReportCallerMemberName();
        }

        private static void ReportCallerMemberName([CallerMemberName] string caller = null)
        {
            Debug.LogError($"Unhanlded method: {caller}"); // fixit - disable for now
        }
    }
}
