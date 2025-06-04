using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    // Traverse an Aseprite file creating Unity objects as you go
    internal class AseUnityObjects : IAseVisitor, IDisposable
    {
        public int CanvasWidth => m_AseFile.Header.Width;
        public int CanvasHeight => m_AseFile.Header.Height;
        public ColorDepth ColorDepth => m_AseFile.Header.ColorDepth;
        public int TransparentIndex => m_AseFile.Header.TransparentIndex;

        private AseFile m_AseFile;
        private readonly Stack<AseCanvas> m_FrameCanvases = new Stack<AseCanvas>();
        private AseCanvas m_TilesetCanvas; // fixit - testing this out
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

        public Texture2D FetchTilesetTexture() // fixit - just for testing
        {
            if (m_TilesetCanvas != null)
            {
                return m_TilesetCanvas.ToTexture2D();
            }

            return null;
        }

        public void Dispose()
        {
            foreach (var canvas in m_FrameCanvases)
            {
                canvas.Dispose();
            }

            if (m_TilesetCanvas != null)
            {
                m_TilesetCanvas.Dispose();
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
            m_LayerChunks.Add(layer);
        }

        public void VisitCelChunk(AseCelChunk cel)
        {
            var layer = m_LayerChunks[cel.LayerIndex];
            if (!layer.IsVisible)
            {
                // Ignore cels from invisible layers
                return;
            }

            if (cel.LinkedCel != null)
            {
                cel = cel.LinkedCel;
            }

            // fixit - test tiles
            if (cel.CelType == CelType.CompressedImage)
            {
                // Get the pixels from this cel and blend them into the canvas for this frame
                unsafe
                {
                    var canvas = m_FrameCanvases.Peek();
                    var canvasPixels = (Color32*)canvas.Pixels.GetUnsafePtr();

                    for (int x = 0; x < cel.Width; x++)
                    {
                        for (int y = 0; y < cel.Height; y++)
                        {
                            Color32 celPixel = GetPixel(x, y, cel.PixelBytes, cel.Width);
                            celPixel.a = CalculateOpacity(celPixel.a, layer.Opacity, cel.Opacity);
                            if (celPixel.a > 0)
                            {
                                int cx = cel.PositionX + x;
                                int cy = cel.PositionY + y;
                                int index = cx + (cy * canvas.Width);

                                Color32 basePixel = canvasPixels[index];
                                Color32 blendedPixel = BlendColors(layer.BlendMode, basePixel, celPixel);
                                canvasPixels[index] = blendedPixel;
                            }
                        }
                    }
                }
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
            m_Palette[TransparentIndex] = Color.clear;
        }

        public void VisitPaletteChunk(AsePaletteChunk palette)
        {
            m_Palette.Clear();
            m_Palette.AddRange(palette.Entries.Select(e => new Color32(e.Red, e.Green, e.Blue, e.Alpha)));
            m_Palette[TransparentIndex] = Color.clear;
        }

        public void VisitSliceChunk(AseSliceChunk slice)
        {
            ReportCallerMemberName();
        }

        public void VisitTilesetChunk(AseTilesetChunk tileset)
        {
            // (Tile Width) x (Tile Height x Number of Tiles) (from the docs)
            int tWidth = tileset.TileWidth;
            int tHeight = tileset.TileHeight * tileset.NumberOfTiles;
            m_TilesetCanvas = new AseCanvas(tWidth, tHeight);

            unsafe
            {
                var tilesetPixels = (Color32*)m_TilesetCanvas.Pixels.GetUnsafePtr();
                for (int n = 0; n < tileset.NumberOfTiles; n++)
                {
                    for (int x = 0; x < tWidth; x++)
                    {
                        int ymin = n * tileset.TileHeight;
                        int ymax = ymin + tileset.TileHeight;

                        for (int y = ymin; y < ymax; y++)
                        {
                            Color32 tilePixel = GetPixel(x, y, tileset.PixelBytes, tWidth);
                            //celPixel.a = CalculateOpacity(celPixel.a, layer.Opacity, cel.Opacity); // fixit - opacity here?
                            if (tilePixel.a > 0)
                            {
                                int index = x + (y * tWidth);

                                tilesetPixels[index] = tilePixel;

                                //Color32 basePixel = tilesetPixels[index];
                                //Color32 blendedPixel = BlendColors(layer.BlendMode, basePixel, celPixel); // fixit - blend here?
                                //canvasPixels[index] = blendedPixel;
                            }
                        }
                    }
                }
            }

        }

        public void VisitUserDataChunk(AseUserDataChunk userData)
        {
        }

        private Color32 GetPixel(int x, int y, byte[] pixelBytes, int stride)
        {
            if (ColorDepth == ColorDepth.Indexed8)
            {
                var index = x + (y * stride);
                int paletteIndex = pixelBytes[index];
                var color = m_Palette[paletteIndex];
                return color;
            }
            else if (ColorDepth == ColorDepth.Grayscale16)
            {
                var index = 2 * (x + (y * stride));
                var value = pixelBytes[index];
                var alpha = pixelBytes[index + 1];
                return new Color32(value, value, value, alpha);
            }
            else if (ColorDepth == ColorDepth.RGBA32)
            {
                var index = 4 * (x + (y * stride));
                var red = pixelBytes[index];
                var green = pixelBytes[index + 1];
                var blue = pixelBytes[index + 2];
                var alpha = pixelBytes[index + 3];
                return new Color32(red, green, blue, alpha);
            }

            // Unsupported color depth
            return Color.magenta;
        }

        private static Color32 BlendColors(BlendMode blend, Color32 prevColor, Color32 thisColor)
        {
            Color32 outColor;
            switch (blend)
            {
                case BlendMode.Darken:
                    PixelBlends.Darken(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Multiply:
                    PixelBlends.Multiply(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.ColorBurn:
                    PixelBlends.ColorBurn(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Lighten:
                    PixelBlends.Lighten(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Screen:
                    PixelBlends.Screen(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.ColorDodge:
                    PixelBlends.ColorDodge(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Addition:
                    PixelBlends.Addition(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Overlay:
                    PixelBlends.Overlay(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.SoftLight:
                    PixelBlends.SoftLight(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.HardLight:
                    PixelBlends.HardLight(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Difference:
                    PixelBlends.Difference(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Exclusion:
                    PixelBlends.Exclusion(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Subtract:
                    PixelBlends.Subtract(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Divide:
                    PixelBlends.Divide(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Hue:
                    PixelBlends.Hue(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Saturation:
                    PixelBlends.Saturation(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Color:
                    PixelBlends.ColorBlend(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Luminosity:
                    PixelBlends.Luminosity(in prevColor, in thisColor, out outColor);
                    break;
                case BlendMode.Normal:
                default:
                    PixelBlends.Normal(in prevColor, in thisColor, out outColor);
                    break;
            }

            return outColor;
        }

        private static byte CalculateOpacity(params byte[] opacities)
        {
            float opacity = 1.0f;
            foreach (var opByte in opacities)
            {
                opacity *= (float)(opByte / 255.0f);
            }

            return (byte)(opacity * 255);
        }

        private static void ReportCallerMemberName([CallerMemberName] string caller = null)
        {
            Debug.LogError($"Unhanlded method: {caller}");
        }
    }
}
