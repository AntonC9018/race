using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using EngineCommon.ColorPicker;
using Kari.Plugins.DataObject;
using Kari.Plugins.Flags;
using UnityEngine;
using TMPro;
using static EngineCommon.Assertions;
using UnityEngine.Events;

namespace Race.Garage
{
    /// <summary>
    /// Stores the actual source of truth data associated with the car.
    /// Other systems should synchronize to this data.
    /// Provides no means of actually setting the values, for that use `CarProperties`.
    /// </summary>
    [System.Serializable]
    [DataObject]
    public partial class CarDataModel
    {
        /// <summary>
        /// </summary>
        public Color mainColor;

        /// <summary>
        /// </summary>
        public string name;

        /// <summary>
        /// </summary>
        public CarStatsInfo statsInfo;

        /// <summary>
        /// Use this for serialization for now, but it should be replaced by a typesafe function
        /// generated via Kari.
        /// </summary>
        public readonly static XmlSerializer Serializer = new XmlSerializer(typeof(CarDataModel));

        // I'm not using notifyPropertyChanged or whatnot, because I want to have more control
        // over these things. I love declarative programming, but I want to do it with my
        // code generator when possible to have max control over things.
    }

    /// <summary>
    /// Information needed to implement the data binding between the car model
    /// and the widgets providing the interaction.
    /// This info is provided by the prefabs to aid in the discovery of the needed
    /// components in the prefabs.
    /// </summary>
    [System.Serializable]
    public struct DisplayCarInfo
    {
        public MeshRenderer meshRenderer;
        public string name;

        /// <summary>
        /// Base, unchangeable stats of the car.
        /// The values cannot go below these values and are purely additive on top.
        /// </summary>
        public CarStats baseStats;
    }

    [NiceFlags]
    public enum CarPrefabInfoFlags
    {
        /// <summary>
        /// Indicates whether the prefab can be used directly as is,
        /// or whether it should be spawned, and then reused.
        /// </summary>
        IsPrespawnedBit = 1 << 0,
    }

    [System.Serializable]
    public struct CarPrefabInfo
    {
        // public CarPrefabInfoFlags flags;

        /// <summary>
        /// Contains the car's prefabs.
        /// Must have a `DisplayCarInfoComponent`, via which we would get its mesh renderer.
        /// These are allowed to already exist in the scene, in which case they will not be duplicated.
        /// </summary>
        public GameObject prefab;
    }

    public struct CarInstanceInfo
    {
        public Material mainMaterial;
        public CarDataModel dataModel;
        public GameObject rootObject;
    }

    // TODO: I assume this one is going to be boxed, so it being a struct
    // should produce even more garbage than it being a class, but this needs profiling.
    /// <summary>
    /// Info passed when a car selection changes.
    /// </summary>
    public readonly struct CarSelectionChangedEventInfo
    {
        /// <summary>
        /// The CarProperties that raised the event.
        /// </summary>
        public readonly CarProperties carProperties;

        /// <summary>
        /// The index of the deselected car.
        /// Access `carProperties` to get the car model associated with it.
        /// Will be -1 if the car is the first one selected.
        /// </summary>
        public readonly int previousIndex;
        
        /// <summary>
        /// The index of the selected car.
        /// Will be -1 in case the car got deselected.
        /// </summary>
        public readonly int currentIndex;

        public CarSelectionChangedEventInfo(CarProperties carProperties, int previousIndex, int currentIndex)
        {
            this.carProperties = carProperties;
            this.previousIndex = previousIndex;
            this.currentIndex = currentIndex;
        }

        public ref CarInstanceInfo PreviousCarInfo => ref carProperties.GetCarInfo(previousIndex);
        public ref CarInstanceInfo CurrentCarInfo => ref carProperties.GetCarInfo(currentIndex);
    }

    
    public readonly struct CarStatsChangedEventInfo
    {
        /// <summary>
        /// The CarProperties that raised the event.
        /// </summary>
        public readonly CarProperties carProperties;

        /// <summary>
        /// The index (id) of the stat that has changed.
        /// The index of -1 means all stats have changed.
        /// </summary>
        public readonly int statIndex;

        public CarStatsChangedEventInfo(CarProperties carProperties, int statIndex)
        {
            this.carProperties = carProperties;
            this.statIndex = statIndex;
        }

        public ref CarStatsInfo CurrentStatsInfo => ref carProperties.CurrentCarInfo.dataModel.statsInfo;
    }

    /// <summary>
    /// Provides data binding between the currently selected car and the other systems
    /// that need to get notified when that data changes.
    /// </summary>
    public class CarProperties : MonoBehaviour
    {
        // TODO: codegen stuff.

        [SerializeField] internal CarPrefabInfo[] _carPrefabInfos;

        /// <summary>
        /// </summary>
        private CarInstanceInfo[] _carInstanceInfos;

        /// <summary>
        /// </summary>
        public ref CarInstanceInfo GetCarInfo(int index)
        {
            assert(CurrentCarIndex >= 0 && CurrentCarIndex < _carInstanceInfos.Length);
            return ref _carInstanceInfos[index];
        }

        /// <summary>
        /// </summary>
        public ref CarInstanceInfo CurrentCarInfo => ref GetCarInfo(CurrentCarIndex);

        private int _currentDropdownSelectionIndex = 0;
        private bool _currentIsDirty;

        /// <summary>
        /// Returns -1 if no car is currently selected.
        /// </summary>
        private int CurrentCarIndex => _currentDropdownSelectionIndex - 1;

        /// <summary>
        /// Check this before accessing <c>CurrentCarInfo</c>.
        /// </summary>
        public bool IsAnyCarSelected => _currentDropdownSelectionIndex > 0;

        // For now, to avoid creating tiny scripts that do almost nothing,
        // just reference the color picker and the car mesh renderer here,
        // but when the property count grows, events can be used to decouple things.
        // I'm doing the simple thing here for now, it's not necessarily scalable
        // in the long run.
        // TODO: Could also consider the mesh renderer as the source of truth,
        // it's not clear what I want yet anyway.
        //
        // For example, here, we could use a bridge script that would listen to the picker events,
        // set the color here, which would notify it back, which it should ignore.
        // And the other way, when the data changes here, it would update it on the picker, and ignore
        // the event coming from that.
        [SerializeField] private CUIColorPicker _colorPicker;
        [SerializeField] private TMP_Dropdown _carNameDropdown;

        [SerializeField] public UnityEvent<CarSelectionChangedEventInfo> OnCarSelected;

        /// <summary>
        /// </summary>
        [SerializeField] public UnityEvent<CarStatsChangedEventInfo> OnStatsChanged;

        void Start()
        {
            assert(_carPrefabInfos is not null);
            _carInstanceInfos = new CarInstanceInfo[_carPrefabInfos.Length];

            var options = new List<TMP_Dropdown.OptionData>(_carPrefabInfos.Length + 1)
            {
                // The first options is "no selection"
                new TMP_Dropdown.OptionData("none"),
            };

            // TODO: serialize the currently selected car.
            int lastPrespawnedEnabledIndex = -1;
            for (int i = 0; i < _carPrefabInfos.Length; i++)
            {
                var prefabInfo = _carPrefabInfos[i];
                assert(prefabInfo.prefab != null);

                var infoComponent = prefabInfo.prefab.GetComponent<DisplayCarInfoComponent>();
                assert(infoComponent != null);
                
                // if (prefabInfo.flags.HasFlag(CarPrefabInfoFlags.IsPrespawnedBit))
                if (!IsPrefab(prefabInfo.prefab))
                {
                    if (lastPrespawnedEnabledIndex != -1)
                        prefabInfo.prefab.SetActive(false);
                    else if (prefabInfo.prefab.activeSelf)
                        lastPrespawnedEnabledIndex = i;

                    ResetInstanceInfo(ref _carInstanceInfos[i], infoComponent, prefabInfo.prefab);
                }

                var option = new TMP_Dropdown.OptionData(infoComponent.info.name);
                options.Add(option);
            }
            _carNameDropdown.AddOptions(options);

            if (lastPrespawnedEnabledIndex != -1)
            {
                _carNameDropdown.value = lastPrespawnedEnabledIndex;
                _currentDropdownSelectionIndex = lastPrespawnedEnabledIndex + 1;
            }

            // Check whether the object is a prefab or an instantiated object in the scene or not.
            // This check feels like a hack, but it seems reliable.
            static bool IsPrefab(GameObject gameObject)
            {
                return gameObject.scene.name is null;
            }
        }

        internal void TriggerStatsChangedEvent(int statChangedIndex = -1)
        {
            var info = new CarStatsChangedEventInfo(
                carProperties: this,
                statIndex: statChangedIndex);
            OnStatsChanged.Invoke(info);
            _currentIsDirty = true;
        }

        private void ResetInstanceInfo(ref CarInstanceInfo instanceInfo,
            DisplayCarInfoComponent infoComponent, GameObject carInstance)
        {
            assert(infoComponent != null);
            assert(infoComponent.info.meshRenderer != null);
            assert(carInstance != null);

            ref var info = ref infoComponent.info;

            var material = info.meshRenderer.material;

            var statsInfo = new CarStatsInfo
            {
                baseStats    = info.baseStats,
                currentStats = info.baseStats,
                additionalStatValue = 0,
            };
            statsInfo.ComputeNonSerializedProperties();

            instanceInfo.dataModel = new CarDataModel
            {
                name      = info.name,
                mainColor = material.color,
                statsInfo = statsInfo,
            };
            instanceInfo.mainMaterial = material;
            instanceInfo.rootObject = carInstance;
        }

        void OnEnable()
        {
            if (IsAnyCarSelected)
                ResetModelWithCarDataMaybeFromFile(ref CurrentCarInfo);

            assert(_colorPicker != null);
            _colorPicker.OnValueChangedEvent.AddListener(OnPickerColorSet);

            assert(_carNameDropdown != null);
            _carNameDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
        
        void OnDisable()
        {
            if (_colorPicker != null)
                _colorPicker.OnValueChangedEvent.RemoveListener(OnPickerColorSet);

            if (_carNameDropdown != null)
                _carNameDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            
            if (IsAnyCarSelected)
                MaybeWriteCurrentModel();
        }


        // TODO: might want to customize the way we get the file path.
        public static string GetFilePath(string carName)
        {
            return Application.persistentDataPath + "/cardata_" + carName + ".xml";
        }

        internal void ResetModelWithCarDataMaybeFromFile(ref CarInstanceInfo info)
        {
            // Let's say the object's name is how we store the data.
            // Let's say we store it in XML for now.
            // TODO: might be worth it to pack the whole array in a single file.

            var dataFullFilePath = GetFilePath(info.dataModel.name);

            if (File.Exists(dataFullFilePath))
            // if (false)
            {
                print(dataFullFilePath);
                using var textReader = new StreamReader(dataFullFilePath);
                // The `Deserialize` cannot keep the existing values wherever
                // a value for a field was not found. It's a pretty stupid API design tbh.
                // TODO: generate a typesafe serialization function.
                // TODO: don't read the file if it hasn't changed.
                // TODO: maybe watch the asset and hotreload it.
                // TODO: check if the stats are valid?
                info.dataModel = (CarDataModel) CarDataModel.Serializer.Deserialize(textReader);

                // TODO: 
                // may want to encapsulate this in the struct,
                // but meh, working with pure data is more comfortable.
                info.dataModel.statsInfo.ComputeNonSerializedProperties();
            }

            ResetModel(ref info);

            // TODO: This is a little bit messy without the bridge handlers.
            void ResetModel(ref CarInstanceInfo info)
            {
                info.mainMaterial.color = info.dataModel.mainColor;
                _colorPicker.ColorRGB = info.dataModel.mainColor;    
                // TODO: Fire callbacks and whatnot. Actually, I'm doing that below.
                // The callbacks need to know the index of the previous car, which this func doesn't have.
                // So it should either get this info, or the callback should happen elsewhere.
            }
        }

        private static void WriteModel(CarDataModel model, string fileName)
        {
            using var textWriter = new StreamWriter(fileName);
            CarDataModel.Serializer.Serialize(textWriter, model);
        }

        private void MaybeWriteCurrentModel()
        {
            if (_currentIsDirty)
            {
                var model = CurrentCarInfo.dataModel;
                var fullFilePath = GetFilePath(model.name);
                WriteModel(model, fullFilePath);
            }
        }

        public readonly struct PropertySetContext
        {
            public readonly CarInstanceInfo info;
            
            // TODO: Should be a codegened enum probably 
            public readonly string nameOfPropertyThatChanged;
        }

        /// <summary>
        /// Callback used for the color picker.
        /// </summary>
        public void OnPickerColorSet(Color color)
        {
            if (!IsAnyCarSelected)
                return;

            ref var info = ref CurrentCarInfo;
            var model = info.dataModel;

            if (model.mainColor != color)
            {
                _currentIsDirty = true;
                model.mainColor = color;

                if (info.mainMaterial != null)
                    info.mainMaterial.color = model.mainColor;

                // Currently, the only source that the data comes from is the color picker,
                // so i'm just not resetting it here.
                // But with an event system, that would be the responsibility of a bridge script.

                // TODO: fire callbacks.
                // PropertySetContext context
                // context.model = _currentModel;
                // context.nameOfPropertyThatChanged = nameof(CarDataModel.mainColor);
            }
        }

        /// <summary>
        /// </summary>
        public void OnDropdownValueChanged(int carIndex)
        {
            assert(carIndex >= 0);
            if (_currentDropdownSelectionIndex == carIndex)
                return;

            var eventInfo = new CarSelectionChangedEventInfo(
                carProperties : this,
                previousIndex : CurrentCarIndex,
                currentIndex : carIndex - 1);

            if (IsAnyCarSelected)
            {
                MaybeWriteCurrentModel();
                _currentIsDirty = false;
                
                assert(CurrentCarInfo.rootObject != null);
                CurrentCarInfo.rootObject.SetActive(false);
            }

            assert(carIndex - 1 < _carInstanceInfos.Length);
            _currentDropdownSelectionIndex = carIndex;

            // The none option (at index 0) is just deselecting.
            if (carIndex > 0)
            {
                ref var carInfo = ref CurrentCarInfo;

                if (carInfo.rootObject == null)
                {
                    ref var prefabInfo = ref _carPrefabInfos[CurrentCarIndex];
                    
                    var carGameObject = GameObject.Instantiate(prefabInfo.prefab);
                    // carGameObject.SetActive(false);
                    carGameObject.transform.SetParent(transform, worldPositionStays: false);

                    var infoComponent = carGameObject.GetComponent<DisplayCarInfoComponent>();
                    ResetInstanceInfo(ref carInfo, infoComponent, carGameObject);
                }
                ResetModelWithCarDataMaybeFromFile(ref carInfo);

                carInfo.rootObject.SetActive(true);
            }

            OnCarSelected.Invoke(eventInfo);
        }
    }
}