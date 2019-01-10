﻿using System.Diagnostics.CodeAnalysis;

namespace Snap.Options
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    class MainOptions
    {
        public ListOptions ListVerb { get; set; }
        public Sha512Options Sha512Verb { get; set; }
        public InstallNupkgOptions InstallNupkgVerb { get; set; }
    }
}
