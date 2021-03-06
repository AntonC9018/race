using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Kari.Plugins.DataObject;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static EngineCommon.Assertions;

namespace Race.Garage
{
    /// <summary>
    /// Contains the name of the given stat, just as it should be diplayed in the UI.
    /// </summary>
    // TODO: generate code with this attribute, currently unimplemented.
    [UnityEngine.Scripting.Preserve]
    public sealed class DisplayNameAttribute : InspectorNameAttribute
    {
        public DisplayNameAttribute(string displayName) : base(displayName)
        {
        }
    }

    // TODO:
    // This probably won't compile with code stripping enabled, because
    // I think it could strip these attributes too.
    // No, you cannot subclass `RangeAttribute` to apply `UnityEngine.Scripting.Preserve`,
    // because it's a sealed class, so we should definitely generate code for this.
    // https://docs.unity3d.com/Manual/ManagedCodeStripping.html

    /// <summary>
    /// Stored in CarDataModel, <see cref="EngineCommon.CarProperties"/>  
    /// </summary>
    [System.Serializable]
    [DataObject]
    public partial struct CarStats
    {
        // NOTE:
        // The stats are floats simply because the sliders work with floats,
        // and also I just made them floats initially.
        //
        // Initially, I thought I'd store the "normalized" values for the stats in the fields,
        // but later decided that the stat ranges should really tell the weights of these stats.
        // Like, how much stat value maxes out the stat.
        // For example, health is 2 times less valuable than the speed,
        // because the max value of speed is twice as high. 
        //
        // The lower bound is arbitrary at this point, I don't know what that can be useful for yet.
        // I mean, the normalization (transforming these into physically meaningful numbers)
        // will have to be done elsewhere anyway.

        /// <summary>
        /// The amount of overload a car can take, essentially.
        /// Driving in a wrong gear damages the car. 
        /// </summary>
        [Range(0, 100)]
        public float health;
        
        /// <summary>
        /// </summary>
        [DisplayName("speed")]
        [Range(0, 200)]
        public float accelerationModifier;

        /// <summary>
        /// Factor of weight reduction.
        /// </summary>
        // [Range(10, 200)]
        // public float lightness;
    }

    /// <summary>
    /// I thought it might be worth it to put this data into a separate struct.
    /// </summary>
    [System.Serializable]
    [DataObject]
    public partial struct CarStatsInfo
    {
        /// <summary>
        /// Base, unchangeable stats of the car.
        /// The values cannot go below these values and are purely additive on top.
        /// </summary>
        // TODO: May be worth it to store this field as a reference type.
        public CarStats baseStats;

        /// <summary>
        /// The total value of base stats + additional stat value.
        /// </summary>
        // [System.NonSerialized]
        [XmlIgnore]
        public float totalStatValue;

        /// <summary>
        /// This value can be redistributed among the stats to increase them.
        /// Represents the max such value.
        /// Don't forget to recalculate the total stat value after you reset this!
        /// </summary>
        public float additionalStatValue;
        
        /// <summary>
        /// The current stat values.
        /// </summary>
        public CarStats currentStats;


        // TODO: 
        // This `ComputeNonSerializedProperties()` and the `totalStatValue` mechanism
        // should be handled by a separate query system, which I did not implement here.
        // See `concepts/Queries` at the root of the repo for a prototype and some ideas.

        /// <summary>
        /// We could initialize these properties (in my case fields) lazily, but
        /// there isn't much benefit in that. The only benefit is that this function won't have to
        /// be called after the struct is initialized.
        /// </summary>
        public void ComputeNonSerializedProperties()
        {
            var baseValue = baseStats.GetTotalValue();
            totalStatValue = baseValue + additionalStatValue;
        }
    }

    public static class CarStatsHelper
    {
        // TODO: should be autogenerated.
        internal static CarStatFieldReflectionInfo[] _StatReflectionInfos;

        public const int Count = 2;
        public const int HealthIndex = 0;
        public const int AccelerationModifierIndex = 1;
        public static readonly float MaxStatValue;

        // A manual meaningless switch works for now, but code generation is way better here.
        // Doing this manually is way too fragile.
        public static ref float GetStatRef(this ref CarStats stats, int index)
        {
            switch (index)
            {
                case HealthIndex:
                    return ref stats.health;
                case AccelerationModifierIndex:
                    return ref stats.accelerationModifier;
                default:
                {
                    assert(false);
                    // A dummy return here.
                    return ref stats.health;
                }
            }
        }

        static CarStatsHelper()
        {
            var type = typeof(CarStats);
            // This one allocates a new array, which we don't really need.
            var fields = type.GetFields();
            assert(fields.Length == Count);
            _StatReflectionInfos = new CarStatFieldReflectionInfo[Count];
            for (int i = 0; i < Count; i++)
            {
                var field = fields[i];
                ref var currentInfo = ref _StatReflectionInfos[i];
                
                {
                    var rangeAttribute = field.GetCustomAttribute<RangeAttribute>();
                    assert(rangeAttribute is not null, field.Name);
                    currentInfo.minValue = rangeAttribute.min;
                    currentInfo.maxValue = rangeAttribute.max;
                }

                {
                    var nameAttribute = field.GetCustomAttribute<DisplayNameAttribute>();
                    if (nameAttribute is null)
                    {
                        // TODO: capitalize appropriately.
                        currentInfo.displayName = field.Name;
                    }
                    else
                    {
                        currentInfo.displayName = nameAttribute.displayName;
                    }
                }
            }

            float sum = 0;
            foreach (ref var info in _StatReflectionInfos.AsSpan())
                sum += info.maxValue;
            MaxStatValue = sum;
        }

        public static float GetTotalValue(this ref CarStats stats)
        {
            float sum = 0;
            // TODO: autogenerate this sum (without a switch).
            for (int i = 0; i < Count; i++)
                sum += stats.GetStatRef(i); // / _StatReflectionInfos[i].ValueRange;
            return sum;
        }
    }

    public struct CarStatFieldReflectionInfo
    {
        /// <summary>
        /// The string displayed to the user.
        /// </summary>
        // TODO: localization.
        public string displayName;
        public float minValue;
        public float maxValue;
        public float ValueRange => maxValue - minValue;
    }

    public class CarStatsManager : MonoBehaviour, InitializationHelper.IInitialize
    {
        /// <summary>
        /// The container where the sliders will be spawned.
        /// </summary>
        [SerializeField] private RectTransform _statsTransform;

        /// <summary>
        /// Children must have a slider component and a label component.
        /// </summary>
        [SerializeField] private GameObject _statsSliderPrefab;

        // Maybe use a singleton?
        private CarProperties _carProperties;

        // This one might not be needed at all, because we can just
        // hide or destory the children and be done with it.
        private ValueChangedCapture[] _sliderValueChangedCaptures;
        private Slider[] _sliders;

        public void Initialize(in InitializationHelper.Properties properties)
        {
            var carProperties = properties.car;
            _carProperties = carProperties;

            var count = CarStatsHelper.Count;
            _sliderValueChangedCaptures = new ValueChangedCapture[count];

            for (int i = 0; i < count; i++)
            {
                var slider = _sliders[i];
                var capture = new ValueChangedCapture(this, i);
                slider.onValueChanged.AddListener(capture.Delegate);
                _sliderValueChangedCaptures[i] = capture;
            }

            // not sure about this aspect.
            carProperties.OnCarSelected.AddListener(OnCarSelected);
            carProperties.OnStatsChanged.AddListener(OnStatsChanged);
        }

        void Awake()
        {
            assert(_statsTransform != null);
            assert(_statsSliderPrefab != null);
            assert(_statsTransform.childCount == 0,
                "The layout object must be empty and must be unmodified by other code");

            {
                // I'm not sure how to do this one correctly.
                // _statsTransform.gameObject.SetActive(false);

                // I'm not sure how this works with the immediate children adding children locally.
                // layoutTransform.hierarchyCapacity = CarStats.Count;

                
                _sliders = new Slider[CarStatsHelper.Count];

                for (int i = 0; i < CarStatsHelper.Count; i++)
                {
                    var childGameObject = GameObject.Instantiate(_statsSliderPrefab);
                    ref var info = ref CarStatsHelper._StatReflectionInfos[i];
                    childGameObject.name = info.displayName;
                    
                    {
                        var slider = childGameObject.GetComponentInChildren<Slider>();
                        assert(slider != null);
                        slider.minValue = info.minValue;
                        slider.maxValue = info.maxValue;
                        slider.value = slider.minValue;

                        _sliders[i] = slider;
                    }
                    {
                        // This might mess up things, because it does depth first search,
                        // but really we want breadth first search here.
                        var label = childGameObject.GetComponentInChildren<TMP_Text>();
                        assert(label != null);
                        label.text = info.displayName;
                    }

                    childGameObject.transform.SetParent(_statsTransform, worldPositionStays: false);
                }
             
                _statsTransform.gameObject.SetActive(true);
            }
        }
        
        public void CleanUpStuff()
        {
            if (_statsTransform == null)
                return;
            _statsTransform.gameObject.SetActive(false);

            // TODO: it's not clear whether we want this yet.
            #if false
            {
                var childCount = layoutTransform.childCount;

                assert(childCount == _sliderValueChangedDelegates.Length,
                    "The layout object must be empty and must be unmodified by other code");

                for (int i = 0; i < childCount; i++)
                {
                    var childTransform = _statsLayout.GetChild(i);
                    var slider = childTransform.GetComponentInChildren<Slider>();
                    assert(slider != null);
                    // TODO: make sure it's been removed.
                    slider.onValueChanged.RemoveListener(_sliderValueChangedCaptures[i].Delegate);
                }
            }
            #endif

            for (int i = 0; i < CarStatsHelper.Count; i++)
            {
                _sliders[i].onValueChanged.RemoveListener(_sliderValueChangedCaptures[i].Delegate);
            }

            _carProperties.OnCarSelected.RemoveListener(OnCarSelected);
            _carProperties.OnStatsChanged.RemoveListener(OnStatsChanged);
        }

        public class ValueChangedCapture
        {
            private CarStatsManager _statsManager;
            private int _sliderIndex;

            public ValueChangedCapture(CarStatsManager statsManager, int sliderIndex)
            {
                _statsManager = statsManager;
                _sliderIndex = sliderIndex;
            }

            public void Delegate(float value)
            {
                _statsManager.OnSliderValueChanged(_sliderIndex, value);
            }
        }

        private void OnSliderValueChanged(int sliderIndex, float value)
        {
            if (!_carProperties.IsAnyCarSelected)
                return;

            assert(sliderIndex < CarStatsHelper.Count);

            ref var info = ref _carProperties.CurrentCarInfo.dataModel.statsInfo;
            ref var currentStats = ref info.currentStats;
            ref float stat = ref currentStats.GetStatRef(sliderIndex);
            if (Mathf.Approximately(value, stat))
                return;

            float baseValue = info.baseStats.GetStatRef(sliderIndex);
            float newValue = value;
            // It's already clamped from above, so we only clamp it from below.
            if (newValue < baseValue)
                newValue = baseValue;
            stat = newValue;

            // Floats lose precision.
            // This is why I recalculate it every time by summing up the stats.
            // And subtracting that from the total allowed "stat value".
            // It may be worth it to always store the underlying values as ints.
            // Or maybe at least store them within the same range, and then rescale
            // them when they are needed.
            // TODO: maybe cache this sum, although it really doens't matter.
            float totalCurrentValue = currentStats.GetTotalValue();
            float overflow = totalCurrentValue - info.totalStatValue;
            if (overflow > 0)
            {
                newValue -= overflow; // * CarStatsHelper._StatReflectionInfos[sliderIndex].ValueRange;
            }

            assert(Mathf.Approximately(newValue, baseValue)
                || newValue > baseValue);

            if (!Mathf.Approximately(value, newValue))
                _sliders[sliderIndex].value = newValue;

            // Old idea: scrapped. Redistribution happens manually.
            // float removedValue = (value - stat) / _statReflectionInfos[sliderIndex].ValueRange;
            // float valuePerStatToAdd = removedValue / (CarStatsHelper.Count - 1);
            // for (int i = 0; i < CarStatsHelper.Count; i++)
            // {
            //     if (i != sliderIndex)
            //     {
            //         // Obvious bug: this is unconstrained by the max value.
            //         float addedValue = valuePerStatToAdd * _statReflectionInfos[i].ValueRange;
            //         ref float v = ref stats.GetStatRef(i);
            //         v += addedValue;
            //         _sliders[i].value = v;
            //     }
            // }

            // TODO: might want to keep the old stats too, so, perhaps, have a setter for the stats.
            _carProperties.TriggerStatsChangedEvent(sliderIndex);
        }

        private void OnCarSelected(CarSelectionChangedEventInfo info)
        {
            assert(info.previousIndex != info.currentIndex);

            // Might want to disable the canvas component, people say it's more efficient.
            if (!info.WasAnyCarSelected)
            {
                _statsTransform.gameObject.SetActive(true);
            }
            else if (!info.IsAnyCarSelected)
            {
                // This line makes it so that the slider goes away when no car is selected.
                // _statsTransform.gameObject.SetActive(false);
                return;
            }

            // Should be equivalent to _carProperties.CurrentCarInfo.
            // TODO: tests for these events.
            ref var statsInfo = ref info.CurrentCarInfo.dataModel.statsInfo;
            
            // Reset stats
            {
                // Since we know these don't change, might be worth it to cache them.
                // var sliders = _statsTransform.GetComponentsInChildren<Slider>();
                // assert(sliders.Length == CarStatsHelper.Count);

                for (int i = 0; i < CarStatsHelper.Count; i++)
                    _sliders[i].value = statsInfo.currentStats.GetStatRef(i);
            }

            _carProperties.TriggerStatsChangedEvent();
        }

        public void OnStatsChanged(CarStatsChangedEventInfo info)
        {
            ref var stats = ref info.carProperties.CurrentCarInfo.dataModel.statsInfo.currentStats;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void SetSingle(ref CarStats stats, int index)
            {
                float statValue = stats.GetStatRef(index);
                _sliders[index].value = statValue;
            }

            if (info.HaveAllStatsChanged)
            {
                for (int i = 0; i < CarStatsHelper.Count; i++)
                    SetSingle(ref stats, i);
            }
            else
            {
                SetSingle(ref stats, info.statIndex);
            }
        }
    }

    
    // Reflection without boxing of the struct with __makeref.
    // https://stackoverflow.com/a/9928322/9731532
    // The problem here is that the value still has to be boxed, so there's little benefit.
    static class ReflectionHelper
    {
        public static void SetValueForValueType<T>(this FieldInfo field, ref T item, object value) where T : struct
        {
            field.SetValueDirect(__makeref(item), value);
        }
    }
}