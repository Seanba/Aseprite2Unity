using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace Aseprite2Unity.Editor
{
    internal static class Deploy
    {
        private static void DeployAseprite2Unity()
        {
            var path = string.Format("{0}/../../deploy/Aseprite2Unity.{1}.unitypackage", Application.dataPath, AsepriteImporter.Version);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.ExportPackage("Assets/Aseprite2Unity", path, ExportPackageOptions.Recurse);
        }
    }
}
