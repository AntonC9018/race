using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static EngineCommon.Assertions;


namespace Race.Garage
{
    public interface IGarageInitialize
    {
        Task Initialize(GarageInitializationInfo initializationInfo);
    }

    public class GarageInitialization : MonoBehaviour, IGarageInitialize
    {
        [SerializeField] private GarageCommonInitializationStuffComponent _commonStuff;
        [SerializeField] private FromGarageToGameplayTransitionManager _transition;

        async Task IGarageInitialize.Initialize(GarageInitializationInfo initializationInfo)
        {
            var transitionInitializationTask = _transition.Initialize(_commonStuff.stuff, initializationInfo);

            assert(_commonStuff != null);
            assert(_transition != null);

            const string label = "display";
            var handle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
            var locations = await handle.Task;

            // We don't release the handle here for now.
            var listHandle = Addressables.LoadAssetsAsync<GameObject>(locations, callback: null);
            var prefabs = await listHandle.Task;
            
            var arrayOfPrefabs = prefabs.Select(p => new CarPrefabInfo { prefab = p, }).ToArray();

            await transitionInitializationTask;
            InitializationHelper.InitializeGarage(_commonStuff.stuff, arrayOfPrefabs);

            Addressables.Release(handle);
        }
    }

    public static class InitializationHelper
    {
        public readonly struct Properties
        {
            public readonly CarProperties car;
            public readonly UserProperties user;

            public Properties(CarProperties car, UserProperties user)
            {
                this.car = car;
                this.user = user;
            }
        }
        
        // TODO: allow only one of the properties, and do DI in a more streamlined way.
        public interface IInitialize
        {
            void Initialize(in Properties properties);
        }

        // For now, inject the dependencies manually.
        public static void InjectProperties(Transform root, in Properties properties)
        {
            var initializationComponents = root.GetComponentsInChildren<IInitialize>(includeInactive: false);
            foreach (var component in initializationComponents)
                component.Initialize(properties);
        }

        public static void InitializeGarage(
            in GarageCommonInitializationStuff initializationInfo,
            CarPrefabInfo[] carPrefabInfos)
        {
            var carProperties = initializationInfo.carProperties;
            var userProperties = initializationInfo.userProperties;
            var diRoot = initializationInfo.diRoot;

            assert(carProperties != null);
            assert(userProperties != null);
            assert(diRoot != null);
            assert(carPrefabInfos != null);

            userProperties.Initialize(carProperties);
            carProperties.Initialize(carPrefabInfos);

            {
                var properties = new Properties(carProperties, userProperties);
                InjectProperties(diRoot, properties);
            }

            carProperties.SelectCarByName(userProperties.DataModel.defaultCarName);
        }
    }
}