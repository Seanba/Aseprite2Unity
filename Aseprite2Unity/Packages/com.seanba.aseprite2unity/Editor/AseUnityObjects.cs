using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    // Traverse an Aseprite file creating Unity objects as you go
    internal class AseUnityObjects : IAseVisitor // fixit - any disposable stuff?
    {
        public void BeginFileVisit(AseFile file)
        {
            ReportCallerMemberName();
        }

        public void EndFileVisit(AseFile file)
        {
            ReportCallerMemberName();
        }

        public void BeginFrameVisit(AseFrame frame)
        {
            // fixit - we build a texture each frame
            ReportCallerMemberName();
        }

        public void EndFrameVisit(AseFrame frame)
        {
            // fixit - Take all the layers and merge them (with blends) into a final texture for the frame
            ReportCallerMemberName();
        }

        public void VisitLayerChunk(AseLayerChunk layer)
        {
            // fixit - add layers to an array. Cel chunks write to a given layer
            ReportCallerMemberName();
        }

        public void VisitCelChunk(AseCelChunk cel)
        {
            // fixit - write to a given layer in the current frame
            ReportCallerMemberName();
        }

        public void VisitDummyChunk(AseDummyChunk dummy)
        {
            // Ignore dummy chunks
        }

        public void VisitFrameTagsChunk(AseFrameTagsChunk frameTags)
        {
            ReportCallerMemberName();
        }

        public void VisitPaletteChunk(AsePaletteChunk palette)
        {
            ReportCallerMemberName();
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
            Debug.LogError($"Unhanlded method: {caller}");
        }
    }
}
