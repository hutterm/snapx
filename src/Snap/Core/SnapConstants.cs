using System;
using System.Reflection;
using System.Runtime.InteropServices;
using NuGet.Frameworks;

namespace Snap.Core
{
    internal static class SnapConstants
    {
        public static OSPlatform OsPlatform => GetOsPlatform();

        public static readonly string SnapAppLibraryName = "Snap.App";
        public static readonly string SnapDllFilename = "Snap.dll";
        public static string SnapAppDllFilename => SnapAppLibraryName + ".dll";
        
        public static readonly string SnapUniqueTargetPathFolderName = BuildSnapNuspecUniqueFolderName();
        public static readonly string NuspecTargetFrameworkMoniker = NuGetFramework.AnyFramework.Framework;
        public static readonly string NuspecRootTargetPath = $"lib/{NuspecTargetFrameworkMoniker}";
        public static readonly string SnapNuspecTargetPath = $"{NuspecRootTargetPath}/{SnapUniqueTargetPathFolderName}";
        public const string ChecksumManifestFilename = "Snap.Checksum.Manifest";
        public const string ReleasesFilename = "Snap.Releases";

        static OSPlatform GetOsPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSPlatform.Windows;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OSPlatform.Linux;
            }

            throw new PlatformNotSupportedException();
        }

        static string BuildSnapNuspecUniqueFolderName()
        {
            var guidStr = typeof(SnapConstants).Assembly.GetCustomAttribute<GuidAttribute>()?.Value;
            Guid.TryParse(guidStr, out var assemblyGuid);
            if (assemblyGuid == Guid.Empty)
            {
                throw new Exception("Fatal error! Assembly guid is empty");
            }

            return assemblyGuid.ToString("N");
        }
    }
}