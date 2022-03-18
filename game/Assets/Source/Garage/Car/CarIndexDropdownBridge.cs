using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Race.Garage
{
    public class CarIndexDropdownBridge : MonoBehaviour, InitializationHelper.IInitialize
    {
        [SerializeField] private TMP_Dropdown _carNameDropdown;
        private CarProperties _carProperties;


        public void Initialize(in InitializationHelper.Properties properties)
        {
            var carProperties = properties.car;
            _carProperties = carProperties;

            var options = new List<TMP_Dropdown.OptionData>(carProperties.CarInstanceInfos.Length + 1)
            {
                // The first option is "no selection"
                new TMP_Dropdown.OptionData("none"),
            };

            // For some reason, you cannot iterate over an array by ref, but you can over a span.
            // I'm not using Linq here, because the instance infos are structs, 
            // and I don't want to copy them.
            // There is this lib, which looks awesome, but I haven't used it yet.
            // https://github.com/VictorNicollet/NoAlloq
            foreach (ref var instanceInfo in carProperties.CarInstanceInfos.AsSpan())
                options.Add(new TMP_Dropdown.OptionData(instanceInfo.name));

            _carNameDropdown.AddOptions(options);
            _carNameDropdown.value = carProperties.CurrentCarIndex + 1;
            _carNameDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            
            carProperties.OnCarSelected.AddListener(OnCarSelected);
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