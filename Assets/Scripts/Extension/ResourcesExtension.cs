


using System.IO;
using UnityEngine;

namespace OpenGS
{
    public class ResourcesExtension
    {
        public static string ResourcesPath = Application.dataPath + "/Resources";

        public static UnityEngine.Object Load(string resourceName, System.Type systemTypeInstance)
        {
            if (string.IsNullOrWhiteSpace(resourceName) || systemTypeInstance == null)
            {
                return null;
            }

            string[] directories = Directory.Exists(ResourcesPath)
                ? Directory.GetDirectories(ResourcesPath, "*", SearchOption.AllDirectories)
                : System.Array.Empty<string>();
            foreach (var item in directories)
            {
                string itemPath = item.Substring(ResourcesPath.Length + 1);
                itemPath = itemPath.Replace('\\', '/').Trim('/');
                UnityEngine.Object result = Resources.Load(itemPath + "/" + resourceName.TrimStart('/'), systemTypeInstance);
                if (result != null)
                    return result;
            }

            var directResult = Resources.Load(resourceName.Replace('\\', '/').TrimStart('/'), systemTypeInstance);
            if (directResult != null)
            {
                return directResult;
            }

            Debug.LogWarning($"[ResourcesExtension] Resource not found: {resourceName} ({systemTypeInstance?.Name})");
            return null;
        }
    }
}
