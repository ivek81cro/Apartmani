using System;
using System.IO;
using System.Linq;

namespace Apartmani.Utilities
{
    public static class ImageStorage
    {
        public const long MaxUploadBytes = 10 * 1024 * 1024;

        public static string GetExtension(byte[] data)
        {
            byte[] png = { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (data.Length >= 20 && data.AsSpan(0, 8).SequenceEqual(png) &&
                data.AsSpan(data.Length - 12, 12).SequenceEqual(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 }))
            {
                return ".png";
            }
            if (data.Length >= 4 && data[0] == 0xff && data[1] == 0xd8 &&
                data[2] == 0xff && data[data.Length - 2] == 0xff && data[data.Length - 1] == 0xd9)
            {
                return ".jpg";
            }
            return null;
        }

        public static string ResolveImagePath(string webRoot, string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return null;
            var relative = storedPath.Replace('\\', '/').TrimStart('/');
            if (!relative.StartsWith("img/", StringComparison.OrdinalIgnoreCase)) return null;
            var root = Path.GetFullPath(Path.Combine(webRoot, "img")) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(webRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return fullPath.StartsWith(root, comparison) ? fullPath : null;
        }
    }
}
