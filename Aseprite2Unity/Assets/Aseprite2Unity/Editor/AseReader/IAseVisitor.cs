using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aseprite2Unity.Editor
{
    public interface IAseVisitor
    {
        void BeginFileVisit(AseFile file);
        void EndFileVisit(AseFile file);

        void BeginFrameVisit(AseFrame frame);
        void EndFrameVisit(AseFrame frame);

        void VisitLayerChunk(AseLayerChunk layer);
        void VisitCelChunk(AseCelChunk cel);
        void VisitFrameTagsChunk(AseFrameTagsChunk frameTags);
        void VisitPaletteChunk(AsePaletteChunk palette);
        void VisitUserDataChunk(AseUserDataChunk useData);
        void VisitSliceChunk(AseSliceChunk slice);
        void VisitDummyChunk(AseDummyChunk dummy);
    }
}
