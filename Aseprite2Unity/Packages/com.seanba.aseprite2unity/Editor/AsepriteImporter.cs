using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    [ScriptedImporter(6, new string[] { "aseprite", "ase" }, 5000)]
    public class AsepriteImporter : ScriptedImporter, IAseVisitor
    {
        // Editor fields
        public float m_PixelsPerUnit = 100.0f;
        public float m_FrameRate = 60.0f;
        public GameObject m_InstantiatedPrefab;
        public string m_SortingLayerName;
        public int m_SortingOrder;
        public AnimatorCullingMode m_AnimatorCullingMode = AnimatorCullingMode.AlwaysAnimate;
        public AnimatorController m_AnimatorController;

        // Properties based on file header
        public int CanvasWidth => m_AseFile.Header.Width;
        public int CanvasHeight => m_AseFile.Header.Height;
        public ColorDepth ColorDepth => m_AseFile.Header.ColorDepth;
        public int TransparentIndex => m_AseFile.Header.TransparentIndex;

        private readonly List<AseLayerChunk> m_LayerChunks = new List<AseLayerChunk>();
        private readonly List<AseTilesetChunk> m_TilesetChunks = new List<AseTilesetChunk>();
        private readonly List<AseFrame> m_Frames = new List<AseFrame>();
        private readonly List<Sprite> m_Sprites = new List<Sprite>();
        private readonly List<AnimationClip> m_AnimationClips = new List<AnimationClip>();

        private GameObject m_GameObject;

        private AssetImportContext m_Context;
        private AseFile m_AseFile;

        private AseFrameTagsChunk m_AseFrameTagsChunk;
        private Vector2? m_Pivot;

        private AseCanvas m_FrameCanvas;
        private readonly AseGraphics.GetPixelArgs m_GetPixelArgs = new AseGraphics.GetPixelArgs();

        private readonly UniqueNameifier m_UniqueNameifierAnimations = new UniqueNameifier();

        [SerializeField]
        private List<string> m_Errors = new List<string>();
        public IEnumerable<string> Errors { get { return m_Errors; } }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            m_Errors.Clear();

#if UNITY_2020_3_OR_NEWER
            m_Context = ctx;

            using (var reader = new AseReader(m_Context.assetPath))
            {
                m_AseFile = new AseFile(reader);
                m_AseFile.VisitContents(this);
            }
#else
            string msg = string.Format("Aesprite2Unity requires Unity 2020.3 or later. You are using {0}", Application.unityVersion);
            m_Errors.Add(msg);
            Debug.LogError(msg);
#endif
        }

        public void BeginFileVisit(AseFile file)
        {
            m_GetPixelArgs.ColorDepth = ColorDepth;
            m_Pivot = null;

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
                animator.cullingMode = m_AnimatorCullingMode;

                // Make a default animator controller if needed
                if (m_AnimatorController == null)
                {
                    var controller = new AnimatorController();
                    controller.name = Path.GetFileNameWithoutExtension(assetPath);
                    controller.AddLayer("Base Layer");

                    foreach (var clip in m_AnimationClips)
                    {
                        controller.AddMotion(clip);
                    }

                    m_Context.AddObjectToAsset(controller.name + "_Controller", controller);
                    
                    foreach (var layer in controller.layers)
                    {
                        var stateMachine = layer.stateMachine;
                        m_Context.AddObjectToAsset(stateMachine.name + "_StateMachine", stateMachine);

                        foreach (var state in stateMachine.states)
                        {
                            m_Context.AddObjectToAsset(state.state.name + "_State", state.state);
                        }
                    }

                    AnimatorController.SetAnimatorController(animator, controller);
                }
                else
                {
                    AnimatorController.SetAnimatorController(animator, m_AnimatorController);
                }
            }

            m_LayerChunks.Clear();
            m_Frames.Clear();
            m_Sprites.Clear();
            m_AnimationClips.Clear();
            m_AseFrameTagsChunk = null;
            m_UniqueNameifierAnimations.Clear();
            m_GameObject = null;
        }

        public void BeginFrameVisit(AseFrame frame)
        {
            m_FrameCanvas = new AseCanvas(CanvasWidth, CanvasHeight);
            m_Frames.Add(frame);
        }

        public void EndFrameVisit(AseFrame frame)
        {
            // Commit the frame by copying it to a Texture2D resource
            var texture2d = m_FrameCanvas.ToTexture2D();
            m_FrameCanvas.Dispose();
            m_FrameCanvas = null;

            // We should have everything we need to make a sprite and add it to our asset
            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var textureId = $"Textures._{m_Frames.Count - 1}";
            var textureName = $"{assetName}.{textureId}";

            // The texture should be ready to be added to our asset
            texture2d.name = textureName;
            m_Context.AddObjectToAsset(textureId, texture2d);

            // Make a sprite out of the texture
            var pivot = m_Pivot ?? new Vector2(0.5f, 0.5f);
            var sprite = Sprite.Create(texture2d, new Rect(0, 0, CanvasWidth, CanvasHeight), pivot, m_PixelsPerUnit);
            m_Sprites.Add(sprite);

            var spriteId = $"Sprites._{m_Sprites.Count - 1}";
            var spriteName =  $"{assetName}.{spriteId}";

            sprite.name = spriteName;
            m_Context.AddObjectToAsset(spriteId, sprite);
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

            if (cel.CelType == CelType.CompressedImage)
            {
                // Get the pixels from this cel and blend them into the canvas for this frame
                unsafe
                {
                    var canvas = m_FrameCanvas;
                    var canvasPixels = (Color32*)canvas.Pixels.GetUnsafePtr();

                    m_GetPixelArgs.PixelBytes = cel.PixelBytes;
                    m_GetPixelArgs.Stride = cel.Width;

                    for (int x = 0; x < cel.Width; x++)
                    {
                        for (int y = 0; y < cel.Height; y++)
                        {
                            Color32 celPixel = AseGraphics.GetPixel(x, y, m_GetPixelArgs);
                            celPixel.a = AseGraphics.CalculateOpacity(celPixel.a, layer.Opacity, cel.Opacity);
                            if (celPixel.a > 0)
                            {
                                int cx = cel.PositionX + x;
                                int cy = cel.PositionY + y;
                                int index = cx + (cy * canvas.Width);

                                Color32 basePixel = canvasPixels[index];
                                Color32 blendedPixel = AseGraphics.BlendColors(layer.BlendMode, basePixel, celPixel);
                                canvasPixels[index] = blendedPixel;
                            }
                        }
                    }
                }
            }
            else if (cel.CelType == CelType.CompressedTilemap)
            {
                // Find layer that is a Tilemap type and has a matching Tileset Index
                var tileset = m_TilesetChunks.FirstOrDefault(ts => ts.TilesetId == layer.TilesetIndex);
                if (tileset != null)
                {
                    unsafe
                    {
                        var canvas = m_FrameCanvas;
                        var canvasPixels = (Color32*)canvas.Pixels.GetUnsafePtr();

                        m_GetPixelArgs.PixelBytes = tileset.PixelBytes;
                        m_GetPixelArgs.Stride = tileset.TileWidth;

                        for (int t = 0; t < cel.TileData32.Length; t++)
                        {
                            // A tileId of zero means an empty tile
                            int tileId = (int)cel.TileData32[t];
                            if (tileId != 0)
                            {
                                int tile_i = t % cel.NumberOfTilesWide;
                                int tile_j = t / cel.NumberOfTilesWide;

                                // What are the start and end coordinates for the tile?
                                int txmin = 0;
                                int txmax = txmin + tileset.TileWidth;
                                int tymin = tileId * tileset.TileHeight;
                                int tymax = tymin + tileset.TileHeight;

                                // What are the start and end coordinates for the canvas we are copying tile pixels to?
                                int cxmin = cel.PositionX + (tile_i * tileset.TileWidth);
                                int cxmax = Math.Min(canvas.Width, cxmin + tileset.TileWidth);
                                int cymin = cel.PositionY + (tile_j * tileset.TileHeight);
                                int cymax = Math.Min(canvas.Height, cymin + tileset.TileHeight);

                                for (int tx = txmin, cx = cxmin; tx < txmax && cx < cxmax; tx++, cx++)
                                {
                                    for (int ty = tymin, cy = cymin; ty < tymax && cy < cymax; ty++, cy++)
                                    {
                                        Color32 tilePixel = AseGraphics.GetPixel(tx, ty, m_GetPixelArgs);
                                        tilePixel.a = AseGraphics.CalculateOpacity(tilePixel.a, layer.Opacity, cel.Opacity);
                                        if (tilePixel.a > 0)
                                        {
                                            int canvasPixelIndex = cx + (cy * canvas.Width);
                                            Color32 basePixel = canvasPixels[canvasPixelIndex];
                                            Color32 blendedPixel = AseGraphics.BlendColors(layer.BlendMode, basePixel, tilePixel);
                                            canvasPixels[canvasPixelIndex] = blendedPixel;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Cannot find tileset {layer.TilesetIndex} for layer {layer.Name}");
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
            m_LayerChunks.Add(layer);
        }

        public void VisitOldPaletteChunk(AseOldPaletteChunk palette)
        {
            m_GetPixelArgs.Palette.Clear();
            m_GetPixelArgs.Palette.AddRange(palette.Colors.Select(c => new Color32(c.red, c.green, c.blue, 255)));
            m_GetPixelArgs.Palette[TransparentIndex] = Color.clear;
        }

        public void VisitPaletteChunk(AsePaletteChunk palette)
        {
            m_GetPixelArgs.Palette.Clear();
            m_GetPixelArgs.Palette.AddRange(palette.Entries.Select(e => new Color32(e.Red, e.Green, e.Blue, e.Alpha)));
            m_GetPixelArgs.Palette[TransparentIndex] = Color.clear;
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

                m_Pivot = new Vector2(px / CanvasWidth, 1.0f - py / CanvasHeight);
            }
        }

        public void VisitUserDataChunk(AseUserDataChunk userData)
        {
        }

        public void VisitTilesetChunk(AseTilesetChunk tileset)
        {
            m_TilesetChunks.Add(tileset);
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
            animationName = animationName.Replace('.', '_');
            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var clipName = $"{assetName}_Clip_{animationName}";
            var clipId = $"Clip_{animationName}";

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
                    if (m_LayerChunks[celData.LayerIndex].IsVisible && !string.IsNullOrEmpty(celData.UserDataText))
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
            m_AnimationClips.Add(clip);
        }
    }
}
