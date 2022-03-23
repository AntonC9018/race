using System.IO;
using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Garage
{
    public class LocalGarageInitialization : MonoBehaviour
    {
        [SerializeField] private GarageCommonInitializationStuffComponent _commonStuff;
        [ContextMenuItem("Delete save files", nameof(DeleteSaveFiles))]
        [SerializeField] private CarPrefabInfo[] _carPrefabInfos;

        public void Start()
        {
            assert(_commonStuff != null);
            InitializationHelper.InitializeGarage(_commonStuff.stuff, _carPrefabInfos);
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