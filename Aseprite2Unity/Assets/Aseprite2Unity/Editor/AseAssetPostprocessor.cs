using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;
using System.IO;

namespace Aseprite2Unity.Editor
{
    public class AseAssetPostprocessor : AssetPostprocessor
    {
        // We need to use a "marker" to trick-out the sprite atlas into updating itself
        // Note: Unity has a fix for, reportedly coming in 2019.2.0a11

        private const string MarkerFileNameWithoutExtension = "aseprite2unity-atlas-marker";
        private static string[] OurAssetExtensions = new string[] { ".ase", ".aseprite" };

        private void OnPreprocessAsset()
        {
            // Sprite atlases will not update themselves trough script unless we "dirty" our atlas marker
            // (Remove this hack when Unity releases a better fix)
            if (assetPath.ToLower().Contains(MarkerFileNameWithoutExtension))
            {
                // Simply add some unique user data to the marker.
                // Use current time as context to a human combined with a unique GUID.
                assetImporter.userData = string.Format("{{ {0}, {1} }}", Guid.NewGuid(), DateTime.Now.ToString());
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
#if UNITY_2018_3_OR_NEWER
            foreach (var ase in importedAssets.Where(a => IsOurAsset(a)))
            {
                // Does our aseprite asset use sprite atlases?
                var atlases = AssetDatabase.GetDependencies(ase).Where(d => IsSpriteAtlasAsset(d));

                if (atlases.Count() > 0)
                {
                    // Add the sprites within our Ase asset to the sprite atlas
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(ase).OfType<Sprite>().ToList();

                    // Make sure our special marker is added to the sprite atlas
                    // Future edits to the aseprite file will not be added to the sprite atlas without this
                    // (This is a bit of a hack that Unity is fixing in future versions)
                    var marker = FindSpriteAtlasMarker();
                    if (marker == null)
                    {
                        Debug.LogErrorFormat("Aseprite2Unity marker '{0}' not found. Check your Aseprite2Unity installation. Sprite Atlases may not work correctly without this marker.", MarkerFileNameWithoutExtension);
                    }
                    else
                    {
                        sprites.Add(marker);
                    }

                    foreach (var path in atlases)
                    {
                        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);

                        // Do not add sprites already in the atlas
                        var alreadedPacked = atlas.GetPackables().OfType<Sprite>();
                        var spritesToAdd = sprites.Except(alreadedPacked).ToArray();
                        atlas.Add(spritesToAdd.ToArray());
                    }

                    // The marker needs to be re-imported so that the sprite atlas is updated
                    if (marker != null)
                    {
                        var markerPath = AssetDatabase.GetAssetPath(marker);
                        AssetDatabase.ImportAsset(markerPath, ImportAssetOptions.ForceUpdate);
                    }
                }
            }
#endif
        }

        private static bool IsOurAsset(string path)
        {
            foreach (var ext in OurAssetExtensions)
            {
                if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSpriteAtlasAsset(string path)
        {
            return path.EndsWith(".spriteatlas", StringComparison.OrdinalIgnoreCase);
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

            return null;
        }
    }
}
