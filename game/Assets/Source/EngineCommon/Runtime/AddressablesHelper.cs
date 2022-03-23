using UnityEngine;
using UnityEngine.AddressableAssets;

using LocationsHandle = UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<System.Collections.Generic.IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>>;

namespace EngineCommon
{
    public static class AddressablesHelper
    {
        public static LocationsHandle GetGameObjectLocations(string key)
        {
            return Addressables.LoadResourceLocationsAsync(key, typeof(GameObject));
        }

    }
}