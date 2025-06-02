using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    [ScriptedImporter(6, new string[] { "aseprite", "ase" }, 5000)]
    public class AsepriteImporter : ScriptedImporter, IAseVisitor
    {
        private readonly static Color m_TransparentColor = new Color32(0, 0, 0, 0);

        // Editor fields
        public float m_PixelsPerUnit = 100.0f;
        public float m_FrameRate = 60.0f;
        public GameObject m_InstantiatedPrefab;
        public string m_SortingLayerName;
        public int m_SortingOrder;
        public AnimatorCullingMode m_AnimatorCullingMode = AnimatorCullingMode.AlwaysAnimate;
        public RuntimeAnimatorController m_AnimatorController;

        // The dimensions of the Aseprite file
        public int AseWidth => m_AseFile.Header.Width;
        public int AseHeight => m_AseFile.Header.Height;

        private readonly List<AseLayerChunk> m_Layers = new List<AseLayerChunk>();
        private readonly List<AseFrame> m_Frames = new List<AseFrame>();
        private readonly List<Sprite> m_Sprites = new List<Sprite>();
        private readonly List<AnimationClip> m_Clips = new List<AnimationClip>();

        private GameObject m_GameObject;

        private List<Color> m_Palette;
        private AssetImportContext m_Context;
        private AseFile m_AseFile;

        private RenderTexture m_FrameRenderTexture;
        private AseFrameTagsChunk m_AseFrameTagsChunk;
        private Vector2? m_Pivot;

        private UniqueNameifier m_UniqueNameifierAnimations = new UniqueNameifier();

        [SerializeField]
        private List<string> m_Errors = new List<string>();
        public IEnumerable<string> Errors { get { return m_Errors; } }

        // Helper classes
        private class ScopedRenderTexture : IDisposable
        {
            private readonly RenderTexture m_OldRenderTexture;

            public ScopedRenderTexture(RenderTexture renderTexture)
            {
                m_OldRenderTexture = RenderTexture.active;
                RenderTexture.active = renderTexture;
            }

            public void Dispose()
            {
                RenderTexture.active = m_OldRenderTexture;
            }
        }

        private class ScopedUnityEngineObject<T> : IDisposable where T : UnityEngine.Object
        {
            public T Obj { get; }

            public ScopedUnityEngineObject(T obj)
            {
                Obj = obj;
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Obj);
            }
        }


        public override void OnImportAsset(AssetImportContext ctx)
        {
            m_Errors.Clear();

#if UNITY_2020_3_OR_NEWER
            m_Context = ctx;

            using (var reader = new AseReader(m_Context.assetPath))
            {
                m_AseFile = new AseFile(reader);
                //m_AseFile.VisitContents(this); // fixit - don't do this while testing

                using (var aseUnityObjects = new AseUnityObjects())
                {
                    m_AseFile.VisitContents(aseUnityObjects);

                    /*
                    // Texture is upside down. Use a Graphics.Blit to fix that instead of fixing all the places where Aseprite touches pixel data.
                    var renderTexture = new RenderTexture(AseWidth, AseHeight, 0, RenderTextureFormat.ARGB32, 0);
                    renderTexture.wrapMode = TextureWrapMode.Clamp;
                    renderTexture.filterMode = FilterMode.Point;
                    RenderTexture oldRenderTexture = RenderTexture.active;
                    RenderTexture.active = renderTexture;

                    var textures = aseUnityObjects.FetchFrameTextures().ToArray();
                    for (int i = 0; i < textures.Length; i++)
                    {
                        var texture = textures[i];

                        Graphics.Blit(texture, renderTexture, new Vector2(1, -1), new Vector2(0, 1));
                        texture.ReadPixels(new Rect(0, 0, AseWidth, AseHeight), 0, 0);
                        texture.Apply(false, true);

                        texture.name = $"AseObjectTexture.{i}";
                        m_Context.AddObjectToAsset(texture.name, texture);
                        m_Context.SetMainObject(texture);
                    }

                    RenderTexture.active = oldRenderTexture;
                    */

                    var textures = aseUnityObjects.FetchFrameTextures().ToArray();
                    for (int i = 0; i < textures.Length; i++)
                    {
                        var texture = textures[i];

                        // Make the texture no longer read/write
                        texture.Apply(false, true);

                        texture.name = $"AseObjectTexture.{i}";
                        m_Context.AddObjectToAsset(texture.name, texture);
                        m_Context.SetMainObject(texture);
                    }
                }
            }
#else
            string msg = string.Format("Aesprite2Unity requires Unity 2020.3 or later. You are using {0}", Application.unityVersion);
            m_Errors.Add(msg);
            Debug.LogError(msg);
#endif
        }

        public void BeginFileVisit(AseFile file)
        {
            m_AseFile = file;
            m_Pivot = null;

            // Start off with a an empty 256 palette
            m_Palette = Enumerable.Repeat(m_TransparentColor, 256).ToList();

            var icon = AssetDatabaseEx.LoadFirstAssetByFilter<Texture2D>("aseprite2unity-icon-0x1badd00d");

            // Use the instantatiated prefab or create a new game object we add components to
            if (m_InstantiatedPrefab != null)
            {
                m_GameObject = Instantiate(m_InstantiatedPrefab);
            }
            else
            {
                m_GameObject = new GameObject();
            }

            m_Context.AddObjectToAsset("_main", m_GameObject, icon);
            m_Context.SetMainObject(m_GameObject);

            if (m_InstantiatedPrefab != null)
            {
                // We want this asset to be reimported when the prefab changes
                var prefabPath = AssetDatabase.GetAssetPath(m_InstantiatedPrefab);
                m_Context.DependsOnArtifact(prefabPath);
                m_Context.DependsOnSourceAsset(prefabPath);
            }
        }

        public void EndFileVisit(AseFile file)
        {
            BuildAnimations();

            // Add a sprite renderer if needed and assign our sprite to it
            var renderer = m_GameObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = m_GameObject.AddComponent<SpriteRenderer>();
                renderer.sortingLayerName = m_SortingLayerName;
                renderer.sortingOrder = m_SortingOrder;
            }

            renderer.sprite = m_Sprites[0];

            // Add an animator if needed
            var animator = m_GameObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = m_GameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = m_AnimatorController;
                animator.cullingMode = m_AnimatorCullingMode;
            }

            m_Palette.Clear();
            m_Layers.Clear();
            m_Frames.Clear();
            m_Sprites.Clear();
            m_Clips.Clear();
            m_AseFrameTagsChunk = null;
            m_UniqueNameifierAnimations.Clear();
            m_GameObject = null;
        }

        public void BeginFrameVisit(AseFrame frame)
        {
            m_FrameRenderTexture ??= new RenderTexture(AseWidth, AseHeight, 0, RenderTextureFormat.ARGB32, 0);
            m_FrameRenderTexture.wrapMode = TextureWrapMode.Clamp;
            m_FrameRenderTexture.filterMode = FilterMode.Point;

            // Clear out the render texture
            using (new ScopedRenderTexture(m_FrameRenderTexture))
            {
                GL.Clear(true, true, Color.clear);
            }

            m_Frames.Add(frame);
        }

        public void EndFrameVisit(AseFrame frame)
        {
            // Commit the frame by copying it to a Texture2D resource
            var texture2d = CreateTexture2D();

            // Copy the frame render texture to our 2D texture
            using (new ScopedRenderTexture(m_FrameRenderTexture))
            {
                texture2d.ReadPixels(new Rect(0, 0, AseWidth, AseHeight), 0, 0);
                texture2d.Apply(false, true);
            }

            // We should have everything we need to make a sprite and add it to our asset
            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var textureId = $"Textures._{m_Frames.Count - 1}";
            var textureName = $"{assetName}.{textureId}";

            // The texture should be ready to be added to our asset
            texture2d.name = textureName;
            m_Context.AddObjectToAsset(textureId, texture2d);

            // Make a sprite out of the texture
            var pivot = m_Pivot ?? new Vector2(0.5f, 0.5f);
            var sprite = Sprite.Create(texture2d, new Rect(0, 0, AseWidth, AseHeight), pivot, m_PixelsPerUnit);
            m_Sprites.Add(sprite);

            var spriteId = $"Sprites._{m_Sprites.Count - 1}";
            var spriteName =  $"{assetName}.{spriteId}";

            sprite.name = spriteName;
            m_Context.AddObjectToAsset(spriteId, sprite);
        }

        public void VisitCelChunk(AseCelChunk cel)
        {
            using (var texture2d = new ScopedUnityEngineObject<Texture2D>(CreateTexture2D()))
            {
                // Is our layer visible?
                var layer = m_Layers[cel.LayerIndex];
                if (!layer.IsVisible)
                {
                    return;
                }

                if (cel.LinkedCel != null)
                {
                    cel = cel.LinkedCel;
                }

                if (cel.CelType == CelType.CompressedTilemap)
                {
                    // Todo seanba: the texture is to be composed of tiles, not pixels
                }
                else if (cel.CelType == CelType.CompressedImage)
                {
                    for (int i = 0; i < cel.Width; i++)
                    {
                        for (int j = 0; j < cel.Height; j++)
                        {
                            var x = cel.PositionX + i;
                            var y = FlipY(cel.PositionY + j, texture2d.Obj.height);

                            Color32 pixel = GetPixelFromBytes(i, j, cel.Width, cel.PixelBytes);
                            texture2d.Obj.SetPixel(x, y, pixel);
                        }
                    }
                }

                texture2d.Obj.Apply();

                // We are done writing to the cel texture now blit it to the frame
                var blitShader = Shader.Find("Hidden/Aseprite2Unity/AsepriteCelBlitter");
                using (var blitMaterial = new ScopedUnityEngineObject<Material>(new Material(blitShader)))
                {
                    // Create the backgournd texture from the current pixels on the frame render texture
                    // Copy the frame render texture to our 2D texture
                    using (var backgoundTexture = new ScopedUnityEngineObject<Texture2D>(CreateTexture2D()))
                    {
                        using (new ScopedRenderTexture(m_FrameRenderTexture))
                        {
                            backgoundTexture.Obj.ReadPixels(new Rect(0, 0, AseWidth, AseHeight), 0, 0);
                            backgoundTexture.Obj.Apply(false);
                        }

                        blitMaterial.Obj.SetTexture("_Background", backgoundTexture.Obj);
                        blitMaterial.Obj.SetFloat("_Opacity", layer.Opacity / 255.0f);
                        blitMaterial.Obj.SetInt("_BlendMode", (int)layer.BlendMode);

                        Graphics.Blit(texture2d.Obj, m_FrameRenderTexture, blitMaterial.Obj);
                    }
                }
            }
        }

        public void VisitDummyChunk(AseDummyChunk dummy)
        {
        }

        public void VisitFrameTagsChunk(AseFrameTagsChunk frameTags)
        {
            m_AseFrameTagsChunk = frameTags;
        }

        public void VisitLayerChunk(AseLayerChunk layer)
        {
            m_Layers.Add(layer);
        }

        public void VisitOldPaletteChunk(AseOldPaletteChunk palette)
        {
        }

        public void VisitPaletteChunk(AsePaletteChunk palette)
        {
            ResizePalette(palette.LastIndex);

            for (int i = 0; i < palette.LastIndex + 1; i++)
            {
                var red = palette.Entries[i].Red;
                var green = palette.Entries[i].Green;
                var blue = palette.Entries[i].Blue;
                var alpha = palette.Entries[i].Alpha;
                var color = new Color32(red, green, blue, alpha);

                m_Palette[i] = color;
            }

            if (m_AseFile.Header.TransparentIndex >= 0)
            {
                m_Palette[m_AseFile.Header.TransparentIndex] = Color.clear;
            }
        }

        public void VisitSliceChunk(AseSliceChunk slice)
        {
            if (string.Equals("unity:pivot", slice.Name, StringComparison.OrdinalIgnoreCase))
            {
                // Assumes the first slice entry under pivot is the pivot for our sprite
                // The center of the slice is our pivot point. This allows for half-pixel pivots.
                var entry = slice.Entries[0];
                float pw = entry.Width;
                float ph = entry.Height;

                float px = entry.OriginX + pw * 0.5f;
                float py = entry.OriginY + ph * 0.5f;

                m_Pivot = new Vector2(px / AseWidth, 1.0f - py / AseHeight);
            }
        }

        public void VisitUserDataChunk(AseUserDataChunk userData)
        {
        }

        public void VisitTilesetChunk(AseTilesetChunk tileset)
        {
            // Todo seanba: Do something with this.
            // Note: The first tile is completely blank (an erase tile?)
            // The tileset should have the pixel data for every tile in it
            for (int t = 0; t < tileset.NumberOfTiles; t++)
            {
                var texture2d = CreateTexture2D();
                for (int x = 0; x < tileset.TileWidth; x++)
                {
                    for (int y = 0; y < tileset.TileHeight; y++)
                    {
                        int i = x + (t * tileset.TileWidth * tileset.TileHeight);
                        int j = FlipY(y, tileset.TileHeight);
                        Color32 color = GetPixelFromBytes(i, j, tileset.TileWidth, tileset.Pixels);
                        texture2d.SetPixel(x, y, color);
                    }
                }

                texture2d.name = $"tileset.{t}";
                texture2d.Apply();
                m_Context.AddObjectToAsset(texture2d.name, texture2d);
            }
        }

        private void ResizePalette(int maxIndex)
        {
            // Make sure we have enough room for palette entries
            int size = maxIndex + 1;
            int count = m_Palette.Count;

            if (size > count)
            {
                m_Palette.AddRange(Enumerable.Repeat(m_TransparentColor, size - count));
            }
        }

        private Func<uint, uint, int, uint> GetBlendFunc(AseLayerChunk layer)
        {
            switch (layer.BlendMode)
            {
                case BlendMode.Normal:
                    return Blender.rgba_blender_normal;

                case BlendMode.Darken:
                    return Blender.rgba_blender_darken;

                case BlendMode.Multiply:
                    return Blender.rgba_blender_multiply;

                case BlendMode.ColorBurn:
                    return Blender.rgba_blender_color_burn;

                case BlendMode.Lighten:
                    return Blender.rgba_blender_lighten;

                case BlendMode.Screen:
                    return Blender.rgba_blender_screen;

                case BlendMode.ColorDodge:
                    return Blender.rgba_blender_color_dodge;

                case BlendMode.Addition:
                    return Blender.rgba_blender_addition;

                case BlendMode.Overlay:
                    return Blender.rgba_blender_overlay;

                case BlendMode.SoftLight:
                    return Blender.rgba_blender_soft_light;

                case BlendMode.HardLight:
                    return Blender.rgba_blender_hard_light;

                case BlendMode.Difference:
                    return Blender.rgba_blender_difference;

                case BlendMode.Exclusion:
                    return Blender.rgba_blender_exclusion;

                case BlendMode.Subtract:
                    return Blender.rgba_blender_subtract;

                case BlendMode.Divide:
                    return Blender.rgba_blender_divide;

                case BlendMode.Hue:
                    return Blender.rgba_blender_hsl_hue;

                case BlendMode.Saturation:
                    return Blender.rgba_blender_hsl_saturation;

                case BlendMode.Color:
                    return Blender.rgba_blender_hsl_color;

                case BlendMode.Luminosity:
                    return Blender.rgba_blender_hsl_luminosity;

                default:
                    Debug.LogErrorFormat("Unsupported blend mode: {0}", layer.BlendMode);
                    return Blender.rgba_blender_normal;
            }
        }

        private static int FlipY(int y, int height)
        {
            return (height - y) - 1;
        }

        private Texture2D CreateTexture2D()
        {
            var texture2d = new Texture2D(AseWidth, AseHeight, TextureFormat.ARGB32, false);
            texture2d.wrapMode = TextureWrapMode.Clamp;
            texture2d.filterMode = FilterMode.Point;

            // Blend functions won't work without our textures starting off cleared
            var clearPixels = Enumerable.Repeat((Color32)Color.clear, AseWidth * AseHeight).ToArray();
            texture2d.SetPixels32(clearPixels);
            texture2d.Apply();

            return texture2d;
        }

        private Color32 GetPixelFromBytes(int x, int y, int stride, byte[] bytes)
        {
            var depth = m_AseFile.Header.ColorDepth;

            if (depth == ColorDepth.Indexed8)
            {
                var index = x + (y * stride);
                var pal = bytes[index];
                var color = m_Palette[pal];
                return color;
            }
            else if (depth == ColorDepth.Grayscale16)
            {
                var index = 2 * (x + (y * stride));
                var value = bytes[index];
                var alpha = bytes[index + 1];
                return new Color32(value, value, value, alpha);
            }
            else if (depth == ColorDepth.RGBA32)
            {
                var index = 4 * (x + (y * stride));
                var red = bytes[index];
                var green = bytes[index + 1];
                var blue = bytes[index + 2];
                var alpha = bytes[index + 3];
                return new Color32(red, green, blue, alpha);
            }

            // Unsupported color depth
            Debug.LogErrorFormat("Unsupported color depth: {0}", depth);
            return Color.magenta;
        }

        private void BuildAnimations()
        {
            // Frame indices to be used in animations
            var frameIndices = Enumerable.Range(0, m_Frames.Count).ToList();

            // If we have any frame tags then make animations out of them
            if (m_AseFrameTagsChunk != null)
            {
                foreach (var entry in m_AseFrameTagsChunk.Entries)
                {
                    var animIndices = Enumerable.Range(entry.FromFrame, entry.ToFrame - entry.FromFrame + 1).ToList();
                    MakeAnimationClip(entry.Name, !entry.IsOneShot, animIndices);

                    // Remove the indices from the pool of animation frames
                    frameIndices.RemoveAll(i => i >= animIndices.First() && i <= animIndices.Last());
                }
            }

            if (frameIndices.Count > 0)
            {
                // Make an animation out of any left over (untagged) frames
                MakeAnimationClip("Untagged", true, frameIndices);
            }
        }

        private void MakeAnimationClip(string animationName, bool isLooping, List<int> frameIndices)
        {
            animationName = m_UniqueNameifierAnimations.MakeUniqueName(animationName);
            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var clipName = $"{assetName}.Animations.{animationName}";
            var clipId = $"Animations.{animationName}";

            var clip = new AnimationClip();
            clip.name = clipName;
            clip.frameRate = m_FrameRate;

            // Black magic for creating a sprite animation curve
            // from: https://answers.unity.com/questions/1080430/create-animation-clip-from-sprites-programmaticall.html
            var binding = new EditorCurveBinding();
            binding.type = typeof(SpriteRenderer);
            binding.path = "";
            binding.propertyName = "m_Sprite";

            var time = 0.0f;
            var keys = new ObjectReferenceKeyframe[frameIndices.Count];

            // Keep track of animation events
            List<AnimationEvent> animationEvents = new List<AnimationEvent>();

            for (int i = 0; i < keys.Length; i++)
            {
                var frameIndex = frameIndices[i];

                var key = new ObjectReferenceKeyframe();
                key.time = time;
                key.value = m_Sprites[frameIndex];
                keys[i] = key;

                // Are there any animation events to add for this frame?
                var frame = m_Frames[frameIndex];
                foreach (var celData in frame.Chunks.OfType<AseCelChunk>())
                {
                    // Cel data on invisible layers is ignored
                    if (m_Layers[celData.LayerIndex].IsVisible && !string.IsNullOrEmpty(celData.UserDataText))
                    {
                        // Is the user data of "event:SomeName" format?
                        const string eventTag = "event:";
                        if (celData.UserDataText.StartsWith(eventTag, StringComparison.OrdinalIgnoreCase))
                        {
                            string eventName = celData.UserDataText.Substring(eventTag.Length);
                            if (!string.IsNullOrEmpty(eventName))
                            {
                                var animationEvent = new AnimationEvent();
                                animationEvent.functionName = eventName;
                                animationEvent.time = time;
                                animationEvents.Add(animationEvent);
                            }
                        }
                    }
                }

                // Advance time for next frame
                time += m_Frames[frameIndex].FrameDurationMs / 1000.0f;
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            // Settings for looping
            AnimationClipSettings settings = new AnimationClipSettings();
            settings.startTime = 0;
            settings.stopTime = time;
            settings.loopTime = isLooping;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Animation events
            if (animationEvents.Any())
            {
                AnimationUtility.SetAnimationEvents(clip, animationEvents.ToArray());
            }

            m_Context.AddObjectToAsset(clipId, clip);
            m_Clips.Add(clip);
        }
    }
}
