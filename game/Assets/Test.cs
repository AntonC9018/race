using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using static EngineCommon.Assertions;
namespace Race
{
    public class Test : MonoBehaviour
    {
        [SerializeField] string _resourceLabel;
        [SerializeField] AssetReference _reference;
        // Start is called before the first frame update
        void Start()
        {
            var lookupType = typeof(GameObject);
            var handle = Addressables.LoadResourceLocationsAsync(_resourceLabel, lookupType); 
            handle.Completed += handle =>
            {
                assert(handle.Result != null);
                foreach (var key in handle.Result)
                    Debug.Log(key);
                Addressables.Release(handle);
            };
        }

        void OnDone(AsyncOperationHandle<IResourceLocator> handle)
        {
            var result = handle.Result;
            
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
