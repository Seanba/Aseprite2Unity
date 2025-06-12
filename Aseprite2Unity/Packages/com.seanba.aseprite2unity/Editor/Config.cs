using System.Reflection;

namespace Aseprite2Unity.Editor
{
    public static class Config
    {
        public static readonly string Version;

        static Config()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(Assembly.GetExecutingAssembly());
            Version = info?.version ?? "unknown";
        }
    }
}
