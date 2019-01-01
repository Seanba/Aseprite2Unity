using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;

namespace Aseprite2Unity.Editor
{
    public class AseAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
#if UNITY_2018_3_OR_NEWER
            foreach (var ase in importedAssets.Where(a => IsAseAsset(a)))
            {
                var atlases = AssetDatabase.GetDependencies(ase).Where(d => IsSpriteAtlasAsset(d));

                if (atlases.Count() > 0)
                {
                    // Add the sprites within our Ase asset to each sprite atlas
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(ase).OfType<Sprite>();

                    foreach (var path in atlases)
                    {
                        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);

                        // Do not add sprites already in the atlas
                        var alreadedPacked = atlas.GetPackables().OfType<Sprite>();
                        var spritesToAdd = sprites.Except(alreadedPacked).ToArray();
                        atlas.Add(spritesToAdd);
                    }
                }
            }
#endif
        }

        private static bool IsAseAsset(string path)
        {
            return path.EndsWith(".ase", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".aseprite", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSpriteAtlasAsset(string path)
        {
            return path.EndsWith(".spriteatlas", StringComparison.OrdinalIgnoreCase);
        }
    }
}
