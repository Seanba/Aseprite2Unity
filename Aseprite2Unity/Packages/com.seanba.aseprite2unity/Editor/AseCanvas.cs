using System;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    // Drawing board for aseprite pixels, blend operations, etc.
    internal class AseCanvas : IDisposable
    {
        public int Width { get; }
        public int Height { get; }
        public NativeArray<Color32> Pixels { get; }

        public AseCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new NativeArray<Color32>(Width * Height, Allocator.Persistent);
        }

        public Texture2D ToTexture2D()
        {
            var texture2d = new Texture2D(Width, Height, TextureFormat.ARGB32, false);
            texture2d.wrapMode = TextureWrapMode.Clamp;
            texture2d.filterMode = FilterMode.Point;
            texture2d.alphaIsTransparency = true;

            texture2d.SetPixels32(Pixels.ToArray(), 0);
            texture2d.Apply(false, true);

            return texture2d;
        }

        public void Dispose()
        {
            Pixels.Dispose();
        }
    }
}
