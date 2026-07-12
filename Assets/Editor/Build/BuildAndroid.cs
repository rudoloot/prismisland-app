using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RemoteDevLoop.Build
{
    public static class BuildAndroid
    {
        private const string BuildMethodName = "RemoteDevLoop.Build.BuildAndroid.Execute";

        [Serializable]
        private sealed class BuildResultRecord
        {
            public string SchemaVersion = "1.0";
            public string Status;
            public string Message;
            public string OutputPath;
            public long TotalSize;
            public string VersionName;
            public int VersionCode;
            public string ArtifactFormat;
            public string FinishedAtUtc;
        }

        private sealed class BuildRequest
        {
            public string OutputPath;
            public string ReportPath;
            public string VersionName;
            public int VersionCode;
            public bool Development;
            public bool BuildAppBundle;
            public string ArtifactFormat;

            public static BuildRequest FromCommandLine()
            {
                var values = ReadCommandLineValues();
                var artifactFormat = Required(values, "-rdlArtifactFormat");
                if (artifactFormat != "Apk" && artifactFormat != "Aab")
                {
                    throw new ArgumentException("-rdlArtifactFormat must be Apk or Aab.");
                }

                int versionCode;
                if (!int.TryParse(Required(values, "-rdlVersionCode"), out versionCode) || versionCode <= 0)
                {
                    throw new ArgumentException("-rdlVersionCode must be a positive integer.");
                }

                bool development;
                if (!bool.TryParse(Required(values, "-rdlDevelopment"), out development))
                {
                    throw new ArgumentException("-rdlDevelopment must be true or false.");
                }

                var outputPath = Path.GetFullPath(Required(values, "-rdlOutputPath"));
                var expectedExtension = artifactFormat == "Apk" ? ".apk" : ".aab";
                if (!string.Equals(Path.GetExtension(outputPath), expectedExtension, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Output path extension does not match artifact format.");
                }

                return new BuildRequest
                {
                    OutputPath = outputPath,
                    ReportPath = Path.GetFullPath(Required(values, "-rdlReportPath")),
                    VersionName = Required(values, "-rdlVersionName"),
                    VersionCode = versionCode,
                    Development = development,
                    BuildAppBundle = artifactFormat == "Aab",
                    ArtifactFormat = artifactFormat
                };
            }

            private static Dictionary<string, string> ReadCommandLineValues()
            {
                var arguments = Environment.GetCommandLineArgs();
                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var index = 0; index < arguments.Length - 1; index++)
                {
                    if (arguments[index].StartsWith("-rdl", StringComparison.Ordinal))
                    {
                        values[arguments[index]] = arguments[index + 1];
                    }
                }

                return values;
            }

            private static string Required(IDictionary<string, string> values, string name)
            {
                string value;
                if (!values.TryGetValue(name, out value) || string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Missing required command line value: " + name);
                }

                return value;
            }
        }

        private sealed class PlayerSettingsSnapshot
        {
            private readonly string bundleVersion;
            private readonly int bundleVersionCode;
            private readonly bool buildAppBundle;
            private readonly bool useCustomKeystore;

            public PlayerSettingsSnapshot()
            {
                bundleVersion = PlayerSettings.bundleVersion;
                bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
                buildAppBundle = EditorUserBuildSettings.buildAppBundle;
                useCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            }

            public void Restore()
            {
                PlayerSettings.bundleVersion = bundleVersion;
                PlayerSettings.Android.bundleVersionCode = bundleVersionCode;
                EditorUserBuildSettings.buildAppBundle = buildAppBundle;
                PlayerSettings.Android.useCustomKeystore = useCustomKeystore;
            }
        }

        public static void Execute()
        {
            BuildRequest request = null;
            PlayerSettingsSnapshot settings = null;
            BuildResultRecord result = null;
            var exitCode = 1;

            try
            {
                request = BuildRequest.FromCommandLine();
                EnsureAndroidTargetIsActive();
                EnsureOutputIsOutsideAssets(request.OutputPath);

                settings = new PlayerSettingsSnapshot();
                PlayerSettings.bundleVersion = request.VersionName;
                PlayerSettings.Android.bundleVersionCode = request.VersionCode;
                EditorUserBuildSettings.buildAppBundle = request.BuildAppBundle;
                PlayerSettings.Android.useCustomKeystore = false;

                var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
                if (scenes.Length == 0)
                {
                    throw new InvalidOperationException("No enabled scenes are configured in Build Settings.");
                }

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = request.OutputPath,
                    target = BuildTarget.Android,
                    options = request.Development ? BuildOptions.Development : BuildOptions.None
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                var artifactExists = File.Exists(request.OutputPath) && new FileInfo(request.OutputPath).Length > 0;
                var succeeded = summary.result == BuildResult.Succeeded && artifactExists;

                result = CreateResult(
                    request,
                    succeeded ? "Succeeded" : "Failed",
                    succeeded ? "Android build succeeded." : "Unity reported a failed build or no artifact was produced.",
                        ToInt64Size(summary.totalSize));
                exitCode = succeeded ? 0 : 1;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (request != null)
                {
                    result = CreateResult(request, "Failed", exception.Message, 0);
                }
            }
            finally
            {
                if (settings != null)
                {
                    settings.Restore();
                }

                if (request != null)
                {
                    WriteReport(request.ReportPath, result ?? CreateResult(request, "Failed", "Build ended without a result.", 0));
                }

                EditorApplication.Exit(exitCode);
            }
        }

        private static BuildResultRecord CreateResult(BuildRequest request, string status, string message, long totalSize)
        {
            return new BuildResultRecord
            {
                Status = status,
                Message = message,
                OutputPath = request.OutputPath,
                TotalSize = totalSize,
                VersionName = request.VersionName,
                VersionCode = request.VersionCode,
                ArtifactFormat = request.ArtifactFormat,
                FinishedAtUtc = DateTime.UtcNow.ToString("o")
            };
        }

        private static long ToInt64Size(ulong totalSize)
        {
            return totalSize > long.MaxValue ? long.MaxValue : (long)totalSize;
        }

        private static void WriteReport(string reportPath, BuildResultRecord result)
        {
            var directory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(reportPath, JsonUtility.ToJson(result, true));
        }

        private static void EnsureAndroidTargetIsActive()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                throw new InvalidOperationException(
                    "Android is not the active build target. Start Unity with -buildTarget Android before invoking " + BuildMethodName + ".");
            }
        }

        private static void EnsureOutputIsOutsideAssets(string outputPath)
        {
            var assetsPath = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var normalizedOutput = Path.GetFullPath(outputPath);
            if (normalizedOutput.StartsWith(assetsPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Android build output must be outside Assets to avoid importing generated artifacts.");
            }
        }
    }
}
