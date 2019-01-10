﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Snap.Core;
using Snap.Core.AnyOS;
using Snap.Options;
using Splat;

namespace Snap
{
    internal class Program
    {
        static long _consoleCreated;

        static int Main(string[] args)
        {
            try
            {
                return MainImplAsync(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return -1;
            }
        }

        static int MainImplAsync(IEnumerable<string> args)
        {
            var snapCryptoProvider = new SnapCryptoProvider();
            var snapFilesystem = new SnapFilesystem(snapCryptoProvider);
            var snapOs = new SnapOs(new SnapOsWindows(snapFilesystem));
            var snapExtractor = new SnapExtractor(snapFilesystem);
            var snapInstaller = new SnapInstaller(snapExtractor, snapFilesystem, snapOs);

            using (var logger = new SnapSetupLogLogger(false) {Level = LogLevel.Info})
            {
                Locator.CurrentMutable.Register(() => logger, typeof(ILogger));
                return MainAsync(args, snapExtractor, snapFilesystem, snapInstaller);
            }
        }

        static int MainAsync(IEnumerable<string> args, ISnapExtractor snapExtractor, ISnapFilesystem snapFilesystem, ISnapInstaller snapInstaller)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            
            return Parser.Default.ParseArguments<Sha512Options, ListOptions, InstallNupkgOptions>(args)
                .MapResult(
                    (ListOptions opts) => SnapList(opts, snapFilesystem).Result,
                    (Sha512Options opts) => SnapSha512(opts, snapFilesystem),
                    (InstallNupkgOptions opts) => SnapInstall(opts, snapFilesystem, snapExtractor, snapInstaller).Result,
                    errs =>
                    {
                        EnsureConsole();
                        return 1;
                    });            
        }

        static async Task<int> SnapInstall(InstallNupkgOptions installNupkgOptions, ISnapFilesystem snapFilesystem, ISnapExtractor snapExtractor, ISnapInstaller snapInstaller)
        {
            if (installNupkgOptions.Filename == null)
            {
                return -1;
            }

            var nupkgFilename = installNupkgOptions.Filename;
            if (nupkgFilename == null || !snapFilesystem.FileExists(nupkgFilename))
            {
                Console.Error.WriteLine($"File not found: {nupkgFilename}.");
                return -1;
            }

            var sw = new Stopwatch();
            sw.Reset();
            sw.Restart();
            Console.WriteLine($"Clean install of local nupkg: {nupkgFilename}.");

            try
            {
                var packageArchiveReader = snapExtractor.ReadPackage(nupkgFilename);
                if (packageArchiveReader == null)
                {
                    Console.Error.WriteLine($"Unknown error reading nupkg: {nupkgFilename}.");
                    return -1;
                }

                var packageIdentity = await packageArchiveReader.GetIdentityAsync(CancellationToken.None);
                var rootAppDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), packageIdentity.Id);

                await snapInstaller.CleanInstallFromDiskAsync(nupkgFilename, rootAppDirectory, CancellationToken.None);

                Console.WriteLine($"Succesfully installed local nupkg in {sw.Elapsed.TotalSeconds:F} seconds.");

                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Unknown error while installing local nupkg: {nupkgFilename}. Message: {e.Message}.");
                return -1;
            }

            return -1;
        }

        static int SnapSha512(Sha512Options sha512Options, ISnapFilesystem snapFilesystem)
        {
            if (sha512Options.Filename == null || !snapFilesystem.FileExists(sha512Options.Filename))
            {
                Console.Error.WriteLine($"File not found: {sha512Options.Filename}.");
                return -1;
            }

            try
            {
                using (var fileStream = new FileStream(sha512Options.Filename, FileMode.Open, FileAccess.Read))
                {
                    Console.WriteLine(snapFilesystem.Sha512(fileStream));
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error computing SHA512-checksum for filename: {sha512Options.Filename}. Error: {e.Message}.");
                return -1;
            }
        }

        static async Task<int> SnapList(ListOptions listOptions, ISnapFilesystem snapFilesystem)
        {
            var snapPkgFileName = default(string);

            if (listOptions.Directory != null)
            {
                snapPkgFileName = listOptions.Directory.EndsWith(".snap") ? 
                    Path.GetFullPath(listOptions.Directory) : Path.Combine(Path.GetFullPath(listOptions.Directory), ".snap");
            }            

            if (listOptions.Apps)
            {
                return await SnapListApps(snapPkgFileName, snapFilesystem);
            }

            if (listOptions.Feeds)
            {
                return await SnapListFeeds(snapPkgFileName, snapFilesystem);
            }

            return -1;
        }

        static async Task<int> SnapListFeeds(string snapPkgFileName, ISnapFilesystem snapFilesystem)
        {
            if (!File.Exists(snapPkgFileName))
            {
                Console.Error.WriteLine($"Error: Unable to find .snap in path {snapPkgFileName}.");
                return -1;
            }

            var snapFormatReader = new SnapFormatReader(snapFilesystem);

            Snaps snaps;
            try
            {
                snaps = await snapFormatReader.ReadFromDiskAsync(snapPkgFileName, CancellationToken.None);
                if (snaps == null)
                {
                    Console.Error.WriteLine(".snap file not found in current directory.");
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error parsing .snap. Message: {e.Message}");
                return -1;
            }

            Console.WriteLine($"Feeds ({snaps.Feeds.Count}):");

            foreach (var feed in snaps.Feeds)
            {
                Console.WriteLine($"Name: {feed.SourceType}. Type: {feed.SourceType}. Source: {feed.SourceUri}");
            }

            return 0;
        }

        static async Task<int> SnapListApps(string snapPkgFileName, ISnapFilesystem snapFilesystem)
        {
            if (!File.Exists(snapPkgFileName))
            {
                Console.Error.WriteLine($"Error: Unable to find .snap in path {snapPkgFileName}.");
                return -1;
            }

            var snapFormatReader = new SnapFormatReader(snapFilesystem);

            Snaps snaps;
            try
            {
                snaps = await snapFormatReader.ReadFromDiskAsync(snapPkgFileName, CancellationToken.None);
                if (snaps == null)
                {
                    Console.Error.WriteLine(".snap file not found in current directory.");
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error parsing .snap. Message: {e.Message}");
                return -1;
            }

            Console.WriteLine($"Snaps ({snaps.Apps.Count}):");
            foreach (var app in snaps.Apps)
            {
                var channels = app.Channels.Select(x => x.Name).ToList();
                Console.WriteLine($"Name: {app.Name}. Version: {app.Version}. Channels: {string.Join(", ", channels)}.");
            }

            return 0;
        }

        static void EnsureConsole()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return;

            if (Interlocked.CompareExchange(ref _consoleCreated, 1, 0) == 1) return;

            if (!NativeMethodsWindows.AttachConsole(-1))
            {
                NativeMethodsWindows.AllocConsole();
            }

            NativeMethodsWindows.GetStdHandle(StandardHandles.StdErrorHandle);
            NativeMethodsWindows.GetStdHandle(StandardHandles.StdOutputHandle);
        }
    }
}
