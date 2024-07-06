﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aseprite2Unity.Editor
{
    public enum ChunkType : ushort
    {
        OldPalette = 0x0004,
        Layer = 0x2004,
        Cel = 0x2005,
        ColorProfile = 0x2007,
        FrameTags = 0x2018,
        Palette = 0x2019,
        UserData = 0x2020,
        Slice = 0x2022,
    }
}
