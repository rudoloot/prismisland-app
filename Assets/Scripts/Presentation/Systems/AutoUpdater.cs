using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class AutoUpdater : MonoBehaviour
{
    private string owner = "";
    private string repo = "";

    private string latestVersion = "";
    private string downloadUrl = "";
    private bool updateAvailable = false;
    private bool isDownloading = false;
    private float downloadProgress = 0f;
    private string statusMessage = "";

    private Rect windowRect;
    private Font customFont;

    void Start()
    {
        // 1. Read runtime config
        TextAsset infoAsset = Resources.Load<TextAsset>("github_info");
        if (infoAsset != null)
        {
            var ownerMatch = System.Text.RegularExpressions.Regex.Match(infoAsset.text, "\"owner\"\\s*:\\s*\"([^\"]+)\"");
            var repoMatch = System.Text.RegularExpressions.Regex.Match(infoAsset.text, "\"repo\"\\s*:\\s*\"([^\"]+)\"");
            if (ownerMatch.Success && repoMatch.Success)
            {
                owner = ownerMatch.Groups[1].Value;
                repo = repoMatch.Groups[1].Value;
                Debug.Log($"[AutoUpdater] Configured for: {owner}/{repo}");
                
                // Start checking
                StartCoroutine(CheckForUpdates());
            }
        }
        else
        {
            Debug.LogWarning("[AutoUpdater] github_info not found in Resources. Update checking disabled.");
        }

        // Initialize window rect in the center
        windowRect = new Rect(Screen.width * 0.15f, Screen.height * 0.35f, Screen.width * 0.7f, Screen.height * 0.3f);
        customFont = Resources.Load<Font>("Fonts/Pretendard-Regular");
    }

    private IEnumerator CheckForUpdates()
    {
        statusMessage = "업데이트 확인 중...";
        string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // GitHub API requires User-Agent header
            webRequest.SetRequestHeader("User-Agent", "Unity-AutoUpdater");
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonText = webRequest.downloadHandler.text;
                var tagMatch = System.Text.RegularExpressions.Regex.Match(jsonText, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                var assetMatch = System.Text.RegularExpressions.Regex.Match(jsonText, "\"browser_download_url\"\\s*:\\s*\"([^\"]+PrismIsland\\.apk)\"");

                if (tagMatch.Success && assetMatch.Success)
                {
                    latestVersion = tagMatch.Groups[1].Value;
                    downloadUrl = assetMatch.Groups[1].Value;

                    // Clean version string
                    string cleanLatest = latestVersion.Replace("v", "").Trim();
                    string cleanCurrent = Application.version.Replace("v", "").Trim();

                    // Compare versions
                    if (CompareVersionStrings(cleanLatest, cleanCurrent) > 0)
                    {
                        updateAvailable = true;
                        statusMessage = $"새 업데이트가 있습니다! (v{cleanLatest})";
                        Debug.Log($"[AutoUpdater] Update available: v{cleanCurrent} -> v{cleanLatest}");
                    }
                    else
                    {
                        Debug.Log("[AutoUpdater] Game is up to date.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[AutoUpdater] Failed to check for updates: " + webRequest.error);
            }
        }
    }

    private int CompareVersionStrings(string v1, string v2)
    {
        var segments1 = v1.Split('.');
        var segments2 = v2.Split('.');
        int length = Mathf.Min(segments1.Length, segments2.Length);
        
        for (int i = 0; i < length; i++)
        {
            if (int.TryParse(segments1[i], out int num1) && int.TryParse(segments2[i], out int num2))
            {
                if (num1 != num2) return num1.CompareTo(num2);
            }
            else
            {
                int val = string.Compare(segments1[i], segments2[i], System.StringComparison.OrdinalIgnoreCase);
                if (val != 0) return val;
            }
        }
        return segments1.Length.CompareTo(segments2.Length);
    }

    private IEnumerator DownloadAndInstall()
    {
        isDownloading = true;
        updateAvailable = false;
        statusMessage = "새 패키지 다운로드 중...";
        
        string savePath = Path.Combine(Application.temporaryCachePath, "update.apk");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(downloadUrl))
        {
            webRequest.downloadHandler = new DownloadHandlerFile(savePath);
            webRequest.SendWebRequest();

            while (!webRequest.isDone)
            {
                downloadProgress = webRequest.downloadProgress;
                yield return null;
            }

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                statusMessage = "설치 준비 완료! 패치를 실행합니다.";
                Debug.Log("[AutoUpdater] Download complete. Triggering native install...");
                InstallAPK(savePath);
            }
            else
            {
                statusMessage = "다운로드 실패: " + webRequest.error;
                isDownloading = false;
                updateAvailable = true; // Allow retry
                Debug.LogError("[AutoUpdater] Download failed: " + webRequest.error);
            }
        }
    }

    private void InstallAPK(string filePath)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");

            string packageName = context.Call<string>("getPackageName");
            string authority = packageName + ".fileprovider";

            AndroidJavaObject fileObject = new AndroidJavaObject("java.io.File", filePath);
            
            AndroidJavaClass fileProvider = new AndroidJavaClass("androidx.core.content.FileProvider");
            AndroidJavaObject uri = fileProvider.CallStatic<AndroidJavaObject>("getUriForFile", context, authority, fileObject);

            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW");
            intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
            intent.Call<AndroidJavaObject>("addFlags", 1); // Intent.FLAG_GRANT_READ_URI_PERMISSION
            intent.Call<AndroidJavaObject>("addFlags", 268435456); // Intent.FLAG_ACTIVITY_NEW_TASK

            currentActivity.Call("startActivity", intent);
            statusMessage = "설치 프로세스가 시작되었습니다.";
        }
        catch (System.Exception e)
        {
            statusMessage = "설치 실행 오류: " + e.Message;
            Debug.LogError("[AutoUpdater] Failed to install APK: " + e.Message);
        }
#else
        Debug.LogWarning($"[AutoUpdater] Installation is only supported on Android. Temp path was: {filePath}");
        statusMessage = "에디터 또는 비안드로이드 플랫폼이므로 파일만 다운로드되었습니다.";
        isDownloading = false;
#endif
    }

    void OnGUI()
    {
        if (!updateAvailable && !isDownloading) return;

        if (customFont != null)
        {
            GUI.skin.font = customFont;
        }

        int baseFontSize = Mathf.RoundToInt(Screen.height * 0.016f);
        
        GUIStyle windowStyle = new GUIStyle(GUI.skin.box);
        windowStyle.normal.background = Texture2D.whiteTexture;

        // Overlay backing
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        // Window background
        GUI.color = new Color(0.92f, 0.86f, 0.72f, 1f); 
        GUI.Box(windowRect, "", windowStyle);
        GUI.color = Color.white;

        // Borders
        GUI.color = new Color(0.5f, 0.35f, 0.2f, 1f);
        GUI.Box(new Rect(windowRect.x - 2, windowRect.y - 2, windowRect.width + 4, windowRect.height + 4), "");
        GUI.color = new Color(0.92f, 0.86f, 0.72f, 1f);
        GUI.Box(windowRect, "");
        GUI.color = Color.white;

        // Draw title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontSize = Mathf.RoundToInt(baseFontSize * 1.2f);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(0.25f, 0.15f, 0.05f, 1f);
        GUI.Label(new Rect(windowRect.x, windowRect.y + 15, windowRect.width, baseFontSize * 2), "🚀 게임 업데이트 알림", titleStyle);

        // Draw status
        GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
        statusStyle.alignment = TextAnchor.MiddleCenter;
        statusStyle.fontSize = baseFontSize;
        statusStyle.normal.textColor = new Color(0.25f, 0.15f, 0.05f, 1f);
        GUI.Label(new Rect(windowRect.x + 10, windowRect.y + 55, windowRect.width - 20, baseFontSize * 2), statusMessage, statusStyle);

        if (updateAvailable)
        {
            float btnW = windowRect.width * 0.35f;
            float btnH = Screen.height * 0.06f;
            float btnY = windowRect.y + windowRect.height - btnH - 15;

            // Yes Button
            if (GUI.Button(new Rect(windowRect.x + windowRect.width * 0.1f, btnY, btnW, btnH), "지금 패치하기"))
            {
                StartCoroutine(DownloadAndInstall());
            }

            // No Button
            if (GUI.Button(new Rect(windowRect.x + windowRect.width * 0.55f, btnY, btnW, btnH), "나중에"))
            {
                updateAvailable = false;
            }
        }
        else if (isDownloading)
        {
            float barW = windowRect.width * 0.8f;
            float barH = Screen.height * 0.03f;
            float barX = windowRect.x + (windowRect.width - barW) / 2f;
            float barY = windowRect.y + windowRect.height - barH - 25;

            Rect bgRect = new Rect(barX, barY, barW, barH);
            GUI.color = new Color(0.85f, 0.78f, 0.65f, 1f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
            
            Rect fgRect = new Rect(barX, barY, barW * downloadProgress, barH);
            GUI.color = new Color(0.2f, 0.6f, 0.8f, 1f);
            GUI.DrawTexture(fgRect, Texture2D.whiteTexture);
            
            GUI.color = Color.white;
            GUIStyle progressStyle = new GUIStyle(GUI.skin.label);
            progressStyle.alignment = TextAnchor.MiddleCenter;
            progressStyle.fontSize = Mathf.RoundToInt(barH * 0.5f);
            progressStyle.normal.textColor = Color.black;
            GUI.Label(bgRect, $"{(downloadProgress * 100f):F0}% 완료", progressStyle);
        }
    }
}
