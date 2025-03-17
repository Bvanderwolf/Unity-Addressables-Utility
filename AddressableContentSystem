using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableContentSystem : MonoBehaviour
{
    /// <summary>
    /// The list of loaded catalogs.
    /// </summary>
    private readonly IList<IResourceLocator> _catalogs = new List<IResourceLocator>();

    /// <summary>
    /// Returns whether any content catalogs have been loaded.
    /// </summary>
    public bool HasLoadedCatalogs => _catalogs.Count > 0;


    public bool DownloadContentCatalog(string catalogPath, Action<RequestResult> callback)
    {
        if (AddressableContent.IsContentCatalogLoaded(catalogPath))
        {
            Debug.LogWarning("Catalog already loaded");
            return (false);
        }

        AddressableContent.DownloadContentCatalog(catalogPath, OnComplete);

        return (true);

        void OnComplete(DataRequestResult<IResourceLocator> result)
        {
            if (result.success)
            {
                _catalogs.Add(result.data);
                Debug.Log($"Catalog loaded. Id: {result.data.LocatorId}, keys: {string.Join(", ", result.data.Keys)}");
                callback?.Invoke(RequestResult.CreateSuccess());
            }
            else
            {
                Debug.LogWarning("Failed: " + result.error);
                callback?.Invoke(RequestResult.CreateError(result.error));
            }
        }
    }

    public bool LoadContentCatalog(string catalogPath, Action<RequestResult> callback)
    {
        if (!AddressableContent.CacheExists())
        {
            Debug.LogWarning("Cache does not exist");
            return (false);
        }

        AddressableContent.LoadContentCatalog(catalogPath, OnComplete);

        return (true);

        void OnComplete(DataRequestResult<IResourceLocator> result)
        {
            if (result.success)
            {
                _catalogs.Add(result.data);
                Debug.Log($"Catalog loaded. Id: {result.data.LocatorId}, keys: {string.Join(", ", result.data.Keys)}");
                callback?.Invoke(RequestResult.CreateSuccess());
            }
            else
            {
                Debug.LogWarning("Failed: " + result.error);
                callback?.Invoke(RequestResult.CreateError(result.error));
            }
        }
    }

    public bool GetDownloadSize(Action<DataRequestResult<long?>> callback)
    {
        if (_catalogs.Count == 0)
        {
            Debug.LogWarning("Catalogs should be loaded before getting download size");
            return (false);
        }

        AddressableContent.GetDownloadSize(_catalogs, OnComplete);

        return (true);

        void OnComplete(DataRequestResult<long?> result)
        {
            if (result.success)
            {
                if (result.data.HasValue)
                {
                    Debug.Log($"Success: content ready ({result.data.Value} MB) ");
                }
                else
                {
                    Debug.Log("Success: content loaded");
                }

                callback?.Invoke(DataRequestResult<long?>.CreateSuccess(result.data));
            }
            else
            {
                Debug.Log("Failed: " + result.error);
                callback?.Invoke(DataRequestResult<long?>.CreateError(result.error));
            }
        }
    }

    public bool DownloadContent(Action<DownloadStatus> statusCallback, Action<RequestResult> resultCallback)
    {
        if (_catalogs.Count == 0)
        {
            Debug.LogWarning("Catalog should be loaded before downloading content");
            return (false);
        }

        AsyncOperationHandle operation = AddressableContent.DownloadContent(_catalogs, OnComplete);
        StartCoroutine(CallbackDownloadStatusRoutine(operation, statusCallback));

        return (true);

        void OnComplete(RequestResult result)
        {
            Debug.Log(result.success ? "Content download was a success" : "Content download failed");
            resultCallback?.Invoke(result);
        }
    }

    public bool GetUpdatedCatalogs(Action<DataRequestResult<string[]>> callback)
    {
        if (_catalogs.Count == 0)
        {
            Debug.LogWarning("Catalog should be loaded before checking for updates");
            return (false);
        }

        AddressableContent.GetUpdatedCatalogs(OnComplete);

        return (true);

        void OnComplete(DataRequestResult<string[]> result)
        {
            if (result.success)
            {
                if (result.data.Length != 0)
                    Debug.Log($"Found {result.data.Length} updated catalogs");

                callback?.Invoke(DataRequestResult<string[]>.CreateSuccess(result.data));
            }
            else
            {
                Debug.LogError($"Retrieval of updated catalogs failed: {result.error}");

                callback?.Invoke(DataRequestResult<string[]>.CreateError(result.error));
            }
        }
    }

    public bool DownloadUpdatedCatalogs(string[] catalogPaths, Action<RequestResult> callback)
    {
        if (_catalogs.Count == 0)
        {
            Debug.LogWarning("Catalog should be loaded before updating");
            return (false);
        }

        AddressableContent.DownloadUpdatedContentCatalogs(catalogPaths, OnComplete);

        return (true);

        void OnComplete(DataRequestResult<IResourceLocator[]> result)
        {
            if (result.success)
            {
                for (int i = 0; i < _catalogs.Count; i++)
                {
                    string locatorId = _catalogs[i].LocatorId;
                    int indexOfUpdatedCatalog =
                        Array.FindIndex(result.data, (catalog) => catalog.LocatorId == locatorId);
                    if (indexOfUpdatedCatalog == -1)
                        continue;

                    _catalogs[i] = result.data[indexOfUpdatedCatalog];
                }

                Debug.Log($"Download of updated catalogs was a success. Updated {_catalogs.Count} catalogs");

                callback?.Invoke(RequestResult.CreateSuccess());
            }
            else
            {
                Debug.LogError($"Download of updated catalogs failed: {result.error}");

                callback?.Invoke(RequestResult.CreateError(result.error));
            }
        }
    }

    public bool CacheExistsForLoadedCatalogs()
    {
        if (_catalogs.Count == 0)
            return (false);

        foreach (IResourceLocator catalog in _catalogs)
            if (!AddressableContent.CacheExists((ResourceLocationMap)catalog))
                return (false);

        return (true);
    }

    private IEnumerator CallbackDownloadStatusRoutine(AsyncOperationHandle operation, Action<DownloadStatus> callback)
    {
        while (!operation.IsDone)
        {
            DownloadStatus status = operation.GetDownloadStatus();
            callback?.Invoke(status);

            yield return null;
        }
    }

    public void ClearRuntimeCache()
    {
        for (int i = _catalogs.Count - 1; i >= 0; i--)
        {
            Addressables.RemoveResourceLocator(_catalogs[i]);
            _catalogs.RemoveAt(i);
        }
    }
}
