#if !UNITY_2019_2_OR_NEWER
#define USE_SPRITE_ATLAS_HACK
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Aseprite2Unity.Editor
{
    // Asset to help us manage sprite atlases which are kind of a pain for scripting
    public class SpriteAtlasUserAsset : ScriptableObject
    {
        // We need to use a "marker" to trick-out the sprite atlas into updating itself
        private const string MarkerFileNameWithoutExtension = "aseprite2unity-atlas-marker";

        public SpriteAtlas m_SpriteAtlas;

        public static SpriteAtlasUserAsset CreateSpriteAtlasUserAsset(SpriteAtlas atlas)
        {
            var spriteAtlasUser = ScriptableObject.CreateInstance<SpriteAtlasUserAsset>();
            spriteAtlasUser.name = "SpriteAtlasUser";
            spriteAtlasUser.m_SpriteAtlas = atlas;

            return spriteAtlasUser;
        }

        public static SpriteAtlasUserAsset GetAsset(string path)
        {
            return AssetDatabase.LoadAssetAtPath<SpriteAtlasUserAsset>(path);
        }

        public static bool RemoveSpritesFromAtlas(string path)
        {
            var asset = GetAsset(path);
            if (asset != null)
            {
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                asset.RemoveSprites(sprites);
                return true;
            }

            return false;
        }

        public static void AddSpritesToAtlas(string path)
        {
            var asset = GetAsset(path);
            if (asset != null)
            {
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                asset.AddSprites(sprites);
            }
        }

        public void RemoveSprites(IEnumerable<Sprite> sprites)
        {
            if (m_SpriteAtlas != null)
            {
                m_SpriteAtlas.Remove(sprites.ToArray());

                // Remove our atlas marker
                m_SpriteAtlas.Remove(new Sprite[1] { FindSpriteAtlasMarker() });
            }
        }

        public void AddSprites(IEnumerable<Sprite> sprites)
        {
            if (m_SpriteAtlas != null && sprites.Any())
            {
                m_SpriteAtlas.Add(sprites.ToArray());

                // Add our marker and force it to be updated
                var marker = FindSpriteAtlasMarker();
                if (marker != null)
                {
                    m_SpriteAtlas.Add(new Sprite[1] { FindSpriteAtlasMarker() });

                    // This sucks but we have to force our marker to be re-imported
                    // This causes the sprite atlas to be updated
                    var markerPath = AssetDatabase.GetAssetPath(marker);
                    AssetDatabase.ImportAsset(markerPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        private static Sprite FindSpriteAtlasMarker()
        {
            foreach (var guid in AssetDatabase.FindAssets(MarkerFileNameWithoutExtension))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var marker = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                if (marker != null)
                {
                    return marker;
                }
            }

            Debug.LogErrorFormat("Could not find sprite atlas marker '{0}'. Make sure Asesprite2Unity was installed correctly.", MarkerFileNameWithoutExtension);
            return null;
        }

        // Helper postprocessor
        private class Postprocessor : AssetPostprocessor
        {
#if USE_SPRITE_ATLAS_HACK
            private void OnPreprocessAsset()
            {
                // Older versions of Unity have to trick out our atlas marker to force it (and the sprite atlases) to be re-imported correctly
                if (assetPath.ToLower().Contains(MarkerFileNameWithoutExtension))
                {
                    // Simply add some unique user data to the marker.
                    // Use current time as context to a human combined with a unique GUID.
                    assetImporter.userData = string.Format("{{ {0}, {1} }}", Guid.NewGuid(), DateTime.Now.ToString());
                }
            }
#endif

            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                foreach (var assetPath in importedAssets)
                {
                    if (assetPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                    {
                        // Have we imported a sprite atlas user?
                        AddSpritesToAtlas(assetPath);
                    }
                }
            }
        }

        // Helper commands
        [MenuItem("Assets/Aseprite2Unity/Create Sprite Atlas (for pixels)")]
        public static void CreateSpriteAtlas()
        {
            string folder = GetCurrentFolder();
            var path = folder + "/" + "SpriteAtlas.spriteAtlas";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var atlas = new SpriteAtlas();

            SpriteAtlasPackingSettings packing = new SpriteAtlasPackingSettings();
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            packing.padding = 4;
            packing.blockOffset = 1;
            atlas.SetPackingSettings(packing);

            SpriteAtlasTextureSettings tex = new SpriteAtlasTextureSettings();
            tex.generateMipMaps = false;
            tex.filterMode = FilterMode.Point;
            tex.sRGB = true;
            atlas.SetTextureSettings(tex);

            TextureImporterPlatformSettings platform = new TextureImporterPlatformSettings();
            platform.maxTextureSize = 2048;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            platform.crunchedCompression = false;
            atlas.SetPlatformSettings(platform);

            AssetDatabase.CreateAsset(atlas, path);
        }

        [MenuItem("Assets/Aseprite2Unity/Clear Sprite Atlas", true)]
        private static bool ClearSpriteAtlasValidate()
        {
            return Selection.activeObject.GetType() == typeof(SpriteAtlas);
        }

        [MenuItem("Assets/Aseprite2Unity/Clear Sprite Atlas")]
        private static void ClearSpriteAtlas()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path))
            {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                atlas.Remove(atlas.GetPackables());
            }
        }

        private static string GetCurrentFolder()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (path == "")
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }

            path.TrimEnd('/', '\\');
            return path;
        }
    }
}
