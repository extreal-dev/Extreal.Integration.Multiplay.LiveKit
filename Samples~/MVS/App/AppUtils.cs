using System.Collections.Generic;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;

namespace Extreal.Integration.Multiplay.Common.MVS.App
{
    public static class AppUtils
    {
        public static bool IsTouchDevice()
#if UNITY_WEBGL && !UNITY_EDITOR
            => bool.Parse(Extreal.Integration.Web.Common.WebGLHelper.CallFunction(nameof(IsTouchDevice)));
#else
            => false;
#endif

        private static readonly HashSet<StageName> SpaceStages = new HashSet<StageName> { StageName.VirtualStage };

        public static bool IsSpace(StageName stageName) => SpaceStages.Contains(stageName);

        private static readonly string[] Unit = new string[] { "Bytes", "KB", "MB", "GB" };

        public static (long, string) GetSizeUnit(long size)
        {
            var count = 0;
            while (size > 1024)
            {
                size /= 1024;
                count++;
            }

            return (size, Unit[count]);
        }

        public static string ConcatUrl(string baseUrl, string relativePath)
        {
            baseUrl = baseUrl.TrimEnd('/');
            relativePath = relativePath.TrimStart('/');
            return $"{baseUrl}/{relativePath}";
        }

        public static long ToMb(long bytes) => bytes >> 20;
    }
}
