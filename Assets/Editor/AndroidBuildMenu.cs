using UnityEditor;
using UnityEngine;
using System.IO;

public static class AndroidBuildMenu
{
    [MenuItem("PrismIsland/Build Android APK")]
    public static void BuildAndroid()
    {
        Debug.Log("[Build] Starting Android Build...");

        // 1. Ensure directory exists
        string buildDir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Builds");
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }
        string outputPath = Path.Combine(buildDir, "PrismIsland.apk");

        // 2. Auto-increment Bundle Version Code
        int currentCode = PlayerSettings.Android.bundleVersionCode;
        if (currentCode <= 0) currentCode = 1;
        int newCode = currentCode + 1;
        PlayerSettings.Android.bundleVersionCode = newCode;
        PlayerSettings.bundleVersion = $"1.0.{newCode}";
        Debug.Log($"[Build] Incremented Version: {PlayerSettings.bundleVersion} (Code: {newCode})");

        // 3. Generate runtime config for game client
        string configPath = Path.Combine(Application.dataPath, "Editor", "github_config.json");
        if (File.Exists(configPath))
        {
            try
            {
                string jsonText = File.ReadAllText(configPath);
                // Simple parsing without needing external models if we just regex or simple parse
                string owner = string.Empty;
                string repo = string.Empty;
                
                // Regex matches
                var ownerMatch = System.Text.RegularExpressions.Regex.Match(jsonText, "\"owner\"\\s*:\\s*\"([^\"]+)\"");
                var repoMatch = System.Text.RegularExpressions.Regex.Match(jsonText, "\"repo\"\\s*:\\s*\"([^\"]+)\"");
                if (ownerMatch.Success && repoMatch.Success)
                {
                    owner = ownerMatch.Groups[1].Value;
                    repo = repoMatch.Groups[1].Value;

                    string runtimeConfigDir = Path.Combine(Application.dataPath, "Resources");
                    if (!Directory.Exists(runtimeConfigDir)) Directory.CreateDirectory(runtimeConfigDir);
                    
                    string runtimeConfigPath = Path.Combine(runtimeConfigDir, "github_info.json");
                    string runtimeJson = $"{{\"owner\":\"{owner}\",\"repo\":\"{repo}\"}}";
                    File.WriteAllText(runtimeConfigPath, runtimeJson);
                    AssetDatabase.Refresh();
                    Debug.Log($"[Build] Generated runtime config for: {owner}/{repo}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Build] Failed to generate runtime config: " + e.Message);
            }
        }

        // 4. Get enabled scenes
        var scenes = EditorBuildSettings.scenes;
        var scenePaths = new System.Collections.Generic.List<string>();
        foreach (var scene in scenes)
        {
            if (scene.enabled)
            {
                scenePaths.Add(scene.path);
            }
        }

        if (scenePaths.Count == 0)
        {
            Debug.LogError("[Build] No enabled scenes in Build Settings!");
            return;
        }

        // 4. Switch build target (if not already Android)
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("[Build] Switching build target to Android...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        // 5. Run build
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenePaths.ToArray();
        buildPlayerOptions.locationPathName = outputPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[Build] Build Succeeded! Path: {outputPath} ({summary.totalSize / 1024 / 1024} MB)");
            
            // Try uploading to GitHub Releases
            if (File.Exists(configPath))
            {
                Debug.Log("[Build] github_config.json found. Starting automatic GitHub upload...");
                UploadToGitHub(PlayerSettings.bundleVersion, outputPath);
            }
            else
            {
                Debug.LogWarning("[Build] github_config.json not found. Skipping automatic GitHub upload.");
            }

            if (!Application.isBatchMode)
            {
                EditorUtility.RevealInFinder(outputPath);
            }
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError($"[Build] Build Failed! Errors: {summary.totalErrors}");
        }
    }

    private static void UploadToGitHub(string tagName, string apkPath)
    {
        string scriptPath = Path.Combine(Application.dataPath, "Editor", "UploadRelease.ps1");
        string arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -tagName \"{tagName}\" -apkPath \"{apkPath}\"";

        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        System.Diagnostics.Process process = new System.Diagnostics.Process { StartInfo = startInfo };
        
        process.OutputDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data)) Debug.Log(args.Data);
        };
        process.ErrorDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data)) Debug.LogError(args.Data);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (Application.isBatchMode)
            {
                Debug.Log("[GitHub] Waiting for upload process to finish in batchmode...");
                process.WaitForExit();
            }
            else
            {
                Debug.Log("[GitHub] Upload running in background...");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[GitHub] Failed to start upload process: " + e.Message);
        }
    }
}
