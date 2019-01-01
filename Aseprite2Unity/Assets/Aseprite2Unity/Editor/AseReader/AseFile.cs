using System;
using System.Collections.Generic;
using System.Linq;

namespace Aseprite2Unity.Editor
{
    public class AseFile
    {
        public AseHeader Header { get; private set; }
        public List<AseFrame> Frames { get; private set; }

        public AseFile(AseReader reader)
        {
            Header = new AseHeader(reader);

            Frames = Enumerable.Repeat<AseFrame>(null, Header.NumFrames).ToList();
            for (int i = 0; i < Header.NumFrames; i++)
            {
                Frames[i] = new AseFrame(this, reader);
            }
        }

        public void VisitContents(IAseVisitor visitor)
        {
            visitor.BeginFileVisit(this);

            foreach (var frame in Frames)
            {
                visitor.BeginFrameVisit(frame);

                foreach (var chunk in frame.Chunks)
                {
                    chunk.Visit(visitor);
                }

                visitor.EndFrameVisit(frame);
            }

            visitor.EndFileVisit(this);
        }

    }
}
