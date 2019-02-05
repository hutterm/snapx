﻿using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Snap.Core;
using Snap.Core.Resources;
using Snap.Extensions;
using Snap.Shared.Tests;
using Xunit;

namespace Snap.Tests.Core
{
    public class SnapAppWriterTests : IClassFixture<BaseFixture>
    {
        readonly BaseFixture _baseFixture;
        readonly ISnapAppWriter _snapAppWriter;
        readonly ISnapAppReader _snapAppReader;
        readonly ISnapFilesystem _snapFilesystem;

        public SnapAppWriterTests(BaseFixture baseFixture)
        {
            _baseFixture = baseFixture ?? throw new ArgumentNullException(nameof(baseFixture));
            _snapFilesystem = new SnapFilesystem();
            _snapAppWriter = new SnapAppWriter();
            _snapAppReader = new SnapAppReader();
        }

        [Fact]
        public void TestToSnapAppYamlString()
        {
            var snapApp = _baseFixture.BuildSnapApp();

            var snapAppYamlStr = _snapAppWriter.ToSnapAppYamlString(snapApp);
            Assert.NotNull(snapAppYamlStr);
        }
        
        [Fact]
        public void TestToSnapAppsYamlString()
        {
            var snapApps = _baseFixture.BuildSnapApps();

            var snapAppsYamlStr = _snapAppWriter.ToSnapAppsYamlString(snapApps);
            Assert.NotNull(snapAppsYamlStr);
        }

        [Fact]
        public void TestBuildSnapAppAssembly()
        {
            var snapAppBefore = _baseFixture.BuildSnapApp();

            using (var assembly = _snapAppWriter.BuildSnapAppAssembly(snapAppBefore))
            {
                var snapAppAfter = assembly.GetSnapApp(_snapAppReader, _snapAppWriter);
                Assert.NotNull(snapAppAfter);
            }
        }

        [Theory]
        [InlineData("WINDOWS")]
        [InlineData("LINUX")]
        public async Task TestOptimizeSnapDllForPackageArchive(string osPlatformStr)
        {
            var osPlatform = OSPlatform.Create(osPlatformStr);

            using (var snapDllAssemblyDefinition = await _snapFilesystem.FileReadAssemblyDefinitionAsync(typeof(SnapAppWriter).Assembly.Location, CancellationToken.None))
            using (var optimizedAssemblyDefinition = _snapAppWriter.OptimizeSnapDllForPackageArchive(snapDllAssemblyDefinition, osPlatform))
            {
                Assert.NotNull(optimizedAssemblyDefinition);

                var optimizedAssembly = Assembly.Load(optimizedAssemblyDefinition.ToByteArray());

                // Assembly is rewritten so we have to use a dynamic cast :(

                var optimizedEmbeddedResources = (dynamic)Activator.CreateInstance
                    (optimizedAssembly.GetType(typeof(SnapEmbeddedResources).FullName, true), true);

                Assert.NotNull(optimizedEmbeddedResources);

                Assert.True((bool)optimizedEmbeddedResources.IsOptimized);
                Assert.Throws<NullReferenceException>(() => object.ReferenceEquals(null, optimizedEmbeddedResources.CoreRunWindows));
                Assert.Throws<NullReferenceException>(() => object.ReferenceEquals(null, optimizedEmbeddedResources.CoreRunLinux));
            }
        }

    }
}
