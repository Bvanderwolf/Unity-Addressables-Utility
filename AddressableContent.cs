public static class AddressableContent
{
    /// <summary>
    /// Loads the cached content catalogs at the given path. Use this if you are offline and want to load the
    /// cached version of the content catalog. Does nothing if the cache doesn't exist or the catalog is already loaded.
    /// Provides the loaded content catalog reference.
    /// </summary>
    public static void LoadContentCatalog(string catalogPath, Action<DataRequestResult<IResourceLocator>> callback)
    {
        if (!CacheExists())
            return;

        DownloadContentCatalog(catalogPath, callback);
    }

    /// <summary>
    /// Downloads the remote content catalog at the given path. Use this method when you are online and
    /// always want to download the latest content catalog. Provides the loaded catalog reference.
    /// Does nothing if the content catalog is already loaded.
    /// </summary>
    public static void DownloadContentCatalog(string catalogPath, Action<DataRequestResult<IResourceLocator>> callback)
    {
        if (IsContentCatalogLoaded(catalogPath))
            return;

        AsyncOperationHandle<IResourceLocator> operation = Addressables.LoadContentCatalogAsync(catalogPath, true);
        operation.Completed += OnCompleted;

        return;

        void OnCompleted(AsyncOperationHandle<IResourceLocator> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                IResourceLocator catalog = handle.Result;
                callback(DataRequestResult<IResourceLocator>.CreateSuccess(catalog));
            }
            else
            {
                callback(DataRequestResult<IResourceLocator>.CreateError($"Failed to load content catalog at path: {catalogPath}"));
            }
        }
    }

    /// <summary>
    /// Checks for updates of loaded content catalogs. Make sure to load the content catalog before calling this method.
    /// Provides a list of content catalog paths that have updates available. Should be used when logging in the
    /// second time to check for updates. If the update was done during the first login, this update should be detected.
    /// </summary>
    public static void GetUpdatedCatalogs(Action<DataRequestResult<string[]>> callback)
    {
        AsyncOperationHandle<List<string>> operation = Addressables.CheckForCatalogUpdates();
        operation.Completed += OnCompleted;

        return;

        void OnCompleted(AsyncOperationHandle<List<string>> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                string[] catalogs = handle.Result.ToArray();
                callback(DataRequestResult<string[]>.CreateSuccess(catalogs));
            }
            else
            {
                callback(DataRequestResult<string[]>.CreateError("Failed to check content catalog update"));
            }
        }
    }

    /// <summary>
    /// Downloads the updated remote catalogs at the given paths. Use this method after receiving updated catalogs
    /// from the <see cref="GetUpdatedCatalogs"/> method after logging in the second time. Provides the updated catalogs.
    /// Make sure to update your own catalogs with these updated catalogs. Ignores paths of catalogs that are not loaded.
    /// </summary>
    public static void DownloadUpdatedContentCatalogs(string[] catalogPaths, Action<DataRequestResult<IResourceLocator[]>> callback)
    {
        string[] loadedCatalogs = catalogPaths.Where(IsContentCatalogLoaded).ToArray();
        if (loadedCatalogs.Length == 0)
            return;

        AsyncOperationHandle operation = Addressables.UpdateCatalogs(loadedCatalogs);
        operation.Completed += OnCompleted;

        return;

        void OnCompleted(AsyncOperationHandle handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                IResourceLocator[] catalogs = loadedCatalogs.Select(GetCatalogReference).ToArray();
                callback(DataRequestResult<IResourceLocator[]>.CreateSuccess(catalogs));
            }
            else
            {
                callback(DataRequestResult<IResourceLocator[]>.CreateError("Failed to update content catalogs"));
            }
        }
    }

    /// <summary>
    /// Retrieves the download size of the content of referenced by given catalogs. Use the catalog references received
    /// by the <see cref="DownloadContentCatalog"/> method. Provides the download size in bytes.
    /// </summary>
    public static void GetDownloadSize(IList<IResourceLocator> catalogs, Action<DataRequestResult<long?>> callback)
    {
        IEnumerable keys = catalogs.SelectMany(catalog => catalog.Keys);
        AsyncOperationHandle<long> operation = Addressables.GetDownloadSizeAsync(keys);
        operation.Completed += OnCompleted;

        return;

        void OnCompleted(AsyncOperationHandle<long> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                long downloadSize = handle.Result;
                long? result = downloadSize > 0 ? downloadSize : null;
                callback(DataRequestResult<long?>.CreateSuccess(result));
            }
            else
            {
                callback(DataRequestResult<long?>.CreateError("Failed to check content download size"));
            }
        }
    }

    /// <summary>
    /// Downloads the content referenced by the given catalogs. Use the catalog references received by the <see cref="DownloadContentCatalog"/>
    /// method. Provides the download operation handle. Make sure to check the "IsDone" property of the operation handle before using
    /// any of its other properties as this handle will be released after completion.
    /// </summary>
    public static AsyncOperationHandle DownloadContent(IList<IResourceLocator> catalogs, Action<RequestResult> callback)
    {
        IEnumerable keys = catalogs.SelectMany(catalog => catalog.Keys);
        AsyncOperationHandle operation = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union);
        operation.Completed += OnCompleted;

        return (operation);

        void OnCompleted(AsyncOperationHandle handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                callback(RequestResult.CreateSuccess());
            }
            else
            {
                callback(RequestResult.CreateError("Failed to download content"));
            }

            handle.Release();
        }
    }

    /// <summary>
    /// Returns a reference to a loaded content catalog with the given path.
    /// </summary>
    public static IResourceLocator GetCatalogReference(string catalogPath)
    {
        foreach (IResourceLocator locator in Addressables.ResourceLocators)
        {
            if (locator is ResourceLocationMap && locator.LocatorId == catalogPath)
                return (locator);
        }

        return (null);
    }

    /// <summary>
    /// Returns whether the content catalog with the given path is loaded.
    /// </summary>
    public static bool IsContentCatalogLoaded(string catalogPath)
    {
        foreach (IResourceLocator locator in Addressables.ResourceLocators)
        {
            if (locator is ResourceLocationMap && locator.LocatorId == catalogPath)
                return (true);
        }

        return (false);
    }

    /// <summary>
    /// Returns whether the content with the given key is loaded.
    /// </summary>
    public static bool IsContentInLoadedCatalog(string key)
    {
        foreach (IResourceLocator locator in Addressables.ResourceLocators)
        {
            if (locator is not ResourceLocationMap)
                continue;
            
            foreach (object keyObject in locator.Keys)
            {
                if (keyObject.ToString() == key)
                    return (true);
            }
        }

        return (false);
    }

    /// <summary>
    /// Deletes cached addressable data from the device.
    /// </summary>
    public static void DeleteCacheFiles()
    {
        string companyName = Application.companyName;
        string productName = Application.productName;
        string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string localLowDirectory = Path.GetFullPath(Path.Combine(appDataDirectory, "..\\LocalLow"));
        string unityDirectory = Path.Combine(localLowDirectory, "Unity", $"{companyName}_{productName}");
        string companyDirectory = Path.Combine(localLowDirectory, companyName, productName, "com.unity.addressables");

        if (Directory.Exists(unityDirectory))
        {
            Directory.Delete(unityDirectory, true);
            Debug.Log("Unity directory cache cleared.");
        }

        if (Directory.Exists(companyDirectory))
        {
            Directory.Delete(companyDirectory, true);
            Debug.Log("Company directory cache cleared.");
        }
    }

    /// <summary>
    /// Returns whether any addressable cache exists on the device.
    /// </summary>
    public static bool CacheExists()
    {
        string companyName = Application.companyName;
        string productName = Application.productName;
        string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string localLowDirectory = Path.GetFullPath(Path.Combine(appDataDirectory, "..\\LocalLow"));
        string unityDirectory = Path.Combine(localLowDirectory, "Unity", $"{companyName}_{productName}");
        string companyDirectory = Path.Combine(localLowDirectory, companyName, productName, "com.unity.addressables");

        return (Directory.Exists(unityDirectory) && Directory.Exists(companyDirectory));
    }
    
    /// <summary>
    /// Returns whether cache exists for the given catalog on the device.
    /// </summary>
    public static bool CacheExists(ResourceLocationMap catalog)
    {
        if (catalog.Locations.Count == 0)
            return (false);
        
        List<Hash128> versions = new List<Hash128>();
        List<IResourceLocation> dependencies = new List<IResourceLocation>();
        IEnumerable<IResourceLocation> locations = catalog.Locations.SelectMany(location => location.Value);

        foreach (IResourceLocation location in locations)
            if (location.HasDependencies)
                dependencies.AddRange(location.Dependencies);

        foreach (IResourceLocation dependency in dependencies)
        {
            if (dependency.Data == null)
                return (false);
    
            AssetBundleRequestOptions options = (AssetBundleRequestOptions)dependency.Data;
            string bundleName = options.BundleName;
    
            versions.Clear();
            Caching.GetCachedVersions(bundleName, versions);

            if (versions.Count == 0)
                return (false);
        }

        return (true);
    }
}
