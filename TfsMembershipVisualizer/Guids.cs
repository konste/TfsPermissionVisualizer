// Guids.cs
// MUST match guids.h
using System;

namespace TfsPermissionVisualizer
{
    static class Guids
    {
        public const string guidTfsPermissionVisualizerPackagePkgString = "c822db4d-30d5-4a2d-b0bc-694791c5cccf";
        public const string guidTfsPermissionVisualizerPackageCmdSetString = "db6aca8c-5059-4a77-adbf-7abfda2c2207";

        public static readonly Guid guidTfsPermissionVisualizerPackageCmdSet = new Guid(Guids.guidTfsPermissionVisualizerPackageCmdSetString);
    };
}