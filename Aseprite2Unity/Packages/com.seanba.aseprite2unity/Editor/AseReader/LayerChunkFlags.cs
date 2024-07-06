using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aseprite2Unity.Editor
{
    public enum LayerChunkFlags : ushort
    {
        Visible = 1,
        Editable = 2,
        LockMovement = 4,
        Background = 8,
        PreferLinkedCels = 16,
        DisplayCollapsed = 32,
        ReferenceLayer = 64,
    }
}
