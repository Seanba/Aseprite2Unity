using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aseprite2Unity.Editor
{
    public class AseFrameTagEntry
    {
        public ushort FromFrame { get; private set; }
        public ushort ToFrame { get; private set; }
        public LoopAnimationDirection LoopAnimationDirection { get; private set; }
        public byte[] ColorRGB { get; private set; }
        public string Name { get; private set; }
        public bool IsOneShot { get; private set; }

        public AseFrameTagEntry(AseReader reader)
        {
            FromFrame = reader.ReadWORD();
            ToFrame = reader.ReadWORD();
            LoopAnimationDirection = (LoopAnimationDirection)reader.ReadBYTE();

            // Ignore next 8 bytes
            reader.ReadBYTEs(8);

            ColorRGB = reader.ReadBYTEs(3);

            // Ignore a byte
            reader.ReadBYTE();

            Name = reader.ReadSTRING();

            // "OneShot" loop hack
            if (Name.StartsWith("[") && Name.EndsWith("]"))
            {
                Name = Name.Remove(0, 1);
                Name = Name.Remove(Name.Length - 1, 1);
                IsOneShot = true;
            }
            else
            {
                IsOneShot = false;
            }
        }
    }
}
