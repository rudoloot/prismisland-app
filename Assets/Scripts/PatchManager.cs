using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PatchManager : MonoBehaviour
{
    [SerializeField] private Text statusText;
    [SerializeField] private Slider progressBar;
    
    // The name of your main scene to load after patching
    [SerializeField] private string nextSceneName = "SampleScene";

    void Start()
    {
        if (statusText) statusText.text = "Initializing...";
        if (progressBar) progressBar.value = 0;
        
        StartCoroutine(CheckAndDownloadUpdates());
    }

    private IEnumerator CheckAndDownloadUpdates()
    {
        // 1. Initialize Addressables
        var initOp = Addressables.InitializeAsync();
        yield return initOp;

        if (statusText) statusText.text = "Checking for updates...";

        // 2. Check for Catalogs to update
        var checkCatalogOp = Addressables.CheckForCatalogUpdates(false);
        yield return checkCatalogOp;

        if (checkCatalogOp.Result.Count > 0)
        {
            if (statusText) statusText.text = "Updating Catalog...";
            // 3. Update Catalogs
            var updateCatalogOp = Addressables.UpdateCatalogs(checkCatalogOp.Result, false);
            yield return updateCatalogOp;
            
            // Wait for catalog update to complete
        }

        // 4. Determine Download Size
        if (statusText) statusText.text = "Checking download size...";
        
        // Use a generic key or label that encompasses your updatable assets
        // For simplicity, checking all dependencies of the "default" label or just trying to download everything
        // Often, games use a specific label like "Preload" or an Addressables group
        // If you don't have a specific key, we can check a known key, e.g. nextSceneName if it's Addressable
        
        // Get all keys (this depends on how you organize your Addressables, here we assume a label "Updateable")
        var sizeOp = Addressables.GetDownloadSizeAsync((IEnumerable)new[] { "default", "Updateable" });
        yield return sizeOp;

        long totalDownloadSize = sizeOp.Result;

        if (totalDownloadSize > 0)
        {
            if (statusText) statusText.text = $"Downloading {(totalDownloadSize / (1024f * 1024f)):F2} MB...";
            
            // 5. Download the dependencies
            var downloadOp = Addressables.DownloadDependenciesAsync((IEnumerable)new[] { "default", "Updateable" }, Addressables.MergeMode.Union);
            
            while (!downloadOp.IsDone)
            {
                if (progressBar)
                {
                    progressBar.value = downloadOp.PercentComplete;
                }
                yield return null;
            }

            if (downloadOp.Status == AsyncOperationStatus.Succeeded)
            {
                if (statusText) statusText.text = "Download Complete!";
            }
            else
            {
                if (statusText) statusText.text = "Download Failed! Please check connection.";
                Debug.LogError("Failed to download Addressables dependencies!");
                Addressables.Release(downloadOp);
                yield break; // Stop here on failure
            }

            Addressables.Release(downloadOp);
        }
        else
        {
            if (statusText) statusText.text = "Already up to date!";
        }

        // 6. Clean up operations
        Addressables.Release(sizeOp);

        // 7. Load Main Scene
        yield return new WaitForSeconds(0.5f); // Brief pause before switching
        if (statusText) statusText.text = "Loading Game...";
        
        // If the MainScene is in Build Settings:
        // SceneManager.LoadScene(nextSceneName);
        
        // If MainScene is Addressable:
        Addressables.LoadSceneAsync(nextSceneName);
    }
}
