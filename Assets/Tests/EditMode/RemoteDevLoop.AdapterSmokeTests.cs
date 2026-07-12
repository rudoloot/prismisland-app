using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class RemoteDevLoopAdapterSmokeTests
{
    [Test]
    public void AndroidBuildEntryPointIsInstalled()
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var buildEntryPoint = Path.Combine(projectRoot, "Assets", "Editor", "Build", "BuildAndroid.cs");

        Assert.That(File.Exists(buildEntryPoint), Is.True, "The project-owned Android build entry point is missing.");
    }
}
