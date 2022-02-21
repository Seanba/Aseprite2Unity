using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    internal class AseAssetPostprocessor : AssetPostprocessor
    {
        private static readonly string[] OurAssetExtensions = new string[] { ".ase", ".aseprite" };

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var assetsToSerialize = new HashSet<string>();

            // Unity has an odd bug with animation controllers that requires them to be dirtied and force-serialized
            // Otherwise animations may not work until Unity is restarted, which is not ideal
            foreach (var ase in importedAssets.Where(a => IsAsepriteAsset(a)))
            {
                // Find animation controllers that depend on this asesprite asset and mark them dirty
                foreach (var dep in AssetDatabase.GetDependencies(ase))
                {
                    foreach (var controller in AssetDatabase.LoadAllAssetsAtPath(dep).OfType<RuntimeAnimatorController>())
                    {
                        EditorUtility.SetDirty(controller);
                        assetsToSerialize.Add(dep);
                    }
                }
            }

            AssetDatabase.ForceReserializeAssets(assetsToSerialize, ForceReserializeAssetsOptions.ReserializeAssets);
        }

        private static bool IsAsepriteAsset(string path)
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
    }
}