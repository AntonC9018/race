using System.IO;
using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Garage
{
    public class LocalGarageInitialization : MonoBehaviour
    {
        [SerializeField] private GarageInitialization _initializationInfoComponent;
        [ContextMenuItem("Delete save files", nameof(DeleteSaveFiles))]
        [SerializeField] private CarPrefabInfo[] _carPrefabInfos;

        public void Start()
        {
            ref readonly var info = ref _initializationInfoComponent.info;
            InitializationHelper.InitializeGarage(in info, _carPrefabInfos);
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