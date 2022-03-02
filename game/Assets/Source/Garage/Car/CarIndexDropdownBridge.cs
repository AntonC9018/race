using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Race.Garage
{
    public class CarIndexDropdownBridge : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _carNameDropdown;
        [SerializeField] private CarProperties _carProperties;


        void Start()
        {
            // TODO: OnCarsListLoaded or something should be an event
            // The cars in fact are not going to be known in advance, so we also need
            // a way to add options dynamically, but that's way outside the current scope.
            var options = new List<TMP_Dropdown.OptionData>(_carProperties.CarInstanceInfos.Length + 1)
            {
                // The first option is "no selection"
                new TMP_Dropdown.OptionData("none"),
            };

            // For some reason, you cannot iterate over an array by ref, but you can over a span.
            // I'm not using Linq here, because the instance infos are structs, 
            // and I don't want to copy them.
            // There is this lib, which looks awesome, but I haven't used it yet.
            // https://github.com/VictorNicollet/NoAlloq
            foreach (ref var instanceInfo in _carProperties.CarInstanceInfos.AsSpan())
                options.Add(new TMP_Dropdown.OptionData(instanceInfo.name));

            _carNameDropdown.AddOptions(options);
        }

        // I guess it's worth it to do it this way, instead of manually wiring it up,
        // because we need to reference both of the objects anyway.
        // And also we need to have more control over the order of initialization.
        void OnEnable()
        {
            _carNameDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            _carProperties.OnCarSelected.AddListener(OnCarSelected);
        }

        void OnDisable()
        {
            _carNameDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            _carProperties.OnCarSelected.RemoveListener(OnCarSelected);
        }
        
        /// <summary>
        /// </summary>
        public void OnDropdownValueChanged(int dropdownCarIndex)
        {
            _carProperties.SelectCar(dropdownCarIndex - 1);
        }

        public void OnCarSelected(CarSelectionChangedEventInfo info)
        {
            _carNameDropdown.value = info.currentIndex + 1;
        }
    }
}