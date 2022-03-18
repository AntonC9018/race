using System.IO;
using UnityEngine;
using static EngineCommon.Assertions;


namespace Race.Garage
{
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
        
        // TODO: allow only one of the properties, and do DI ina more streamlined way.
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
    }

    public class LocalInitialization : MonoBehaviour
    {
        [SerializeField] private CarProperties _carProperties;
        [SerializeField] private UserProperties _userProperties;

        [ContextMenuItem("Delete save files", nameof(DeleteSaveFiles))]
        [SerializeField] private CarPrefabInfo[] _carPrefabInfos;
        [SerializeField] private Transform _diRoot;

        public void Start()
        {
            assert(_carProperties != null);
            assert(_userProperties != null);
            _userProperties.Initialize(_carProperties);
            _carProperties.Initialize(_carPrefabInfos);

            {
                var properties = new InitializationHelper.Properties(_carProperties, _userProperties);
                InitializationHelper.InjectProperties(_diRoot, properties);
            }

            _carProperties.SelectCarByName(_userProperties.DataModel.defaultCarName);
        }

        private void DeleteSaveFiles()
        {
            foreach (var prefabInfo in _carPrefabInfos)
            {
                var displayInfo = prefabInfo.prefab.GetComponent<DisplayCarInfoComponent>().info;
                var saveFileFullPath = CarProperties.GetSaveFilePath(displayInfo.name);
                if (File.Exists(saveFileFullPath))
                {
                    print("Deleting " + saveFileFullPath);
                    File.Delete(saveFileFullPath);
                }
            }
        }
    }
}