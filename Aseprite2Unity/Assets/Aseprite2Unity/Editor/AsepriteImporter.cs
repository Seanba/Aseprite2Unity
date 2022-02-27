using System;
 using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.U2D;

namespace Aseprite2Unity.Editor
{
    [ScriptedImporter(4, new string[] { "aseprite", "ase" })]
    public class AsepriteImporter : ScriptedImporter, IAseVisitor
    {
        public const string Version = "1.2.0";

        private readonly static Color m_TransparentColor = new Color32(0, 0, 0, 0);

        // Editor fields
        public float m_PixelsPerUnit = 100.0f;
        public SpriteAtlas m_SpriteAtlas;
        public float m_FrameRate = 60.0f;
        public GameObject m_InstantiatedPrefab;
        public string m_SortingLayerName;
        public int m_SortingOrder;
        public AnimatorCullingMode m_AnimatorCullingMode = AnimatorCullingMode.AlwaysAnimate;
        public RuntimeAnimatorController m_AnimatorController;

        private readonly List<AseLayerChunk> m_Layers = new List<AseLayerChunk>();
        private readonly List<AseFrame> m_Frames = new List<AseFrame>();
        private readonly List<Sprite> m_Sprites = new List<Sprite>();
        private readonly List<AnimationClip> m_Clips = new List<AnimationClip>();

        private GameObject m_GameObject;

        private List<Color> m_Palette;
        private AssetImportContext m_Context;
        private AseFile m_AseFile;

        private Color[] m_ClearPixels;
        private Texture2D m_Texture2D;
        private AseFrameTagsChunk m_AseFrameTagsChunk;
        private Vector2? m_Pivot;

        private UniqueNameifier m_UniqueNameifier = new UniqueNameifier();

        [SerializeField]
        private List<string> m_Errors = new List<string>();
        public IEnumerable<string> Errors { get { return m_Errors; } }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            m_Errors.Clear();

#if UNITY_2018_3_OR_NEWER
            m_Context = ctx;

            using (var reader = new AseReader(ctx.assetPath))
            {
                m_AseFile = new AseFile(reader);
                m_AseFile.VisitContents(this);
            }
#else
            string msg = string.Format("Aesprite2Unity requires Unity 2018.3 or later. You are using {0}", Application.unityVersion);
            m_Errors.Add(msg);
            Debug.LogError(msg);
#endif
        }

        public void BeginFileVisit(AseFile file)
        {
            SpriteAtlasUserAsset.RemoveSpritesFromAtlas(assetPath);

            m_AseFile = file;
            m_Pivot = null;

            // Start off with a an empty 256 palette
            m_Palette = Enumerable.Repeat(m_TransparentColor, 256).ToList();

            // Create the array of clear pixels we'll use to begin each frame
            m_ClearPixels = Enumerable.Repeat(Color.clear, m_AseFile.Header.Width * m_AseFile.Header.Height).ToArray();

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

            m_GameObject.name = Path.GetFileNameWithoutExtension(assetPath);
            m_Context.AddObjectToAsset("_main", m_GameObject, icon);
            m_Context.SetMainObject(m_GameObject);
        }

        public void EndFileVisit(AseFile file)
        {
            // Add sprites to sprite atlas (or more correctly, add the scritable object that will add the sprites when import completes)
            var spriteAtlasUser = SpriteAtlasUserAsset.CreateSpriteAtlasUserAsset(m_SpriteAtlas);
            m_Context.AddObjectToAsset("__atlas", spriteAtlasUser);

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

            m_AseFile = null;
            m_Context = null;
            m_Palette.Clear();
            m_Layers.Clear();
            m_Frames.Clear();
            m_Sprites.Clear();
            m_Clips.Clear();
            m_AseFrameTagsChunk = null;
            m_UniqueNameifier.Clear();
            m_GameObject = null;
        }

        public void BeginFrameVisit(AseFrame frame)
        {
            var width = m_AseFile.Header.Width;
            var height = m_AseFile.Header.Height;
            m_Texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
            m_Texture2D.wrapMode = TextureWrapMode.Clamp;
            m_Texture2D.filterMode = FilterMode.Point;
            m_Texture2D.name = string.Format("{0}.Textures._{1}", Path.GetFileNameWithoutExtension(assetPath), m_Frames.Count);

            // Texture starts off blank
            m_Texture2D.SetPixels(0, 0, width, height, m_ClearPixels);
            m_Texture2D.Apply();

            m_Frames.Add(frame);
        }

        public void EndFrameVisit(AseFrame frame)
        {
            // We should have everything we need to make a sprite and add it to our asset

            // The texture should be ready to be added to our asset
            m_Texture2D.Apply();
            m_Context.AddObjectToAsset(m_Texture2D.name, m_Texture2D);

            // Make a sprite out of the texture
            var pivot = m_Pivot ?? new Vector2(0.5f, 0.5f);
            var sprite = Sprite.Create(m_Texture2D, new Rect(0, 0, m_Texture2D.width, m_Texture2D.height), pivot, m_PixelsPerUnit);
            sprite.name = string.Format("{0}.Sprites._{1}", Path.GetFileNameWithoutExtension(assetPath), m_Sprites.Count);
            m_Sprites.Add(sprite);
            m_Context.AddObjectToAsset(sprite.name, sprite);
        }

        public void VisitCelChunk(AseCelChunk cel)
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

            byte opacity = PixmanCombine.MUL_UN8(cel.Opacity, layer.Opacity);

            var blendfunc = GetBlendFunc(layer);

            for (int i = 0; i < cel.Width; i++)
            {
                for (int j = 0; j < cel.Height; j++)
                {
                    var x = cel.PositionX + i;
                    var y = FlipY(cel.PositionY + j);

                    Color32 colorBackdrop = m_Texture2D.GetPixel(x, y);
                    Color32 colorSrc = GetPixelFromBytes(i, j, cel.Width, cel.PixelBytes);

                    uint backdrop = Color32ToRGBA(colorBackdrop);
                    uint src = Color32ToRGBA(colorSrc);

                    uint result = blendfunc(backdrop, src, opacity);
                    Color32 colorResult = RGBAToColor32(result);

                    m_Texture2D.SetPixel(x, y, colorResult);
                }
            }

            m_Texture2D.Apply();
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

                m_Pivot = new Vector2(px / m_Texture2D.width, 1.0f - py / m_Texture2D.height);
            }
        }

        public void VisitUserDataChunk(AseUserDataChunk userData)
        {
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

        private static uint Color32ToRGBA(Color32 color)
        {
            return DocColor.rgba(color.r, color.g, color.b, color.a);
        }

        private static Color32 RGBAToColor32(uint color)
        {
            byte red = DocColor.rgba_getr(color);
            byte green = DocColor.rgba_getg(color);
            byte blue = DocColor.rgba_getb(color);
            byte alpha = DocColor.rgba_geta(color);
            return new Color32(red, green, blue, alpha);
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

        private int FlipY(int y)
        {
            return (m_Texture2D.height - y) - 1;
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
                    string animName = string.Format("{0}.Animations.{1}", Path.GetFileNameWithoutExtension(assetPath), entry.Name);
                    MakeAnimationClip(animName, !entry.IsOneShot, animIndices);

                    // Remove the indices from the pool of animation frames
                    frameIndices.RemoveAll(i => i >= animIndices.First() && i <= animIndices.Last());
                }
            }

            // Make an animation out of any left over (untagged) frames
            string untaggedName = string.Format("{0}.Animations.Untagged", Path.GetFileNameWithoutExtension(assetPath));
            MakeAnimationClip(untaggedName, true, frameIndices);
        }

        private void MakeAnimationClip(string name, bool isLooping, List<int> frameIndices)
        {
            var clip = new AnimationClip();
            clip.name = m_UniqueNameifier.MakeUniqueName(name);
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

            m_Context.AddObjectToAsset(clip.name, clip);
            m_Clips.Add(clip);
        }
    }
}
