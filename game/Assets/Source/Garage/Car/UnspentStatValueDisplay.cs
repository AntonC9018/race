using UnityEngine;
using TMPro;
using System;

namespace Race.Garage
{
    [RequireComponent(typeof(TMP_Text))]
    public class UnspentStatValueDisplay : MonoBehaviour, InitializationHelper.IInitialize
    {
        private TMP_Text _text;
        private string _initialText;

        void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _initialText = _text.text;
        }

        public void Initialize(in InitializationHelper.Properties properties)
        {
            var carProperties = properties.car;
            carProperties.OnStatsChanged.AddListener(OnStatsChanged);
            carProperties.OnCarSelected.AddListener(OnCarSelected);
        }

        private void OnStatsChanged(CarStatsChangedEventInfo info)
        {
            ref var statsInfo = ref info.CurrentStatsInfo;
            float currentTotalValue = statsInfo.currentStats.GetTotalValue();
            float availableValue = statsInfo.totalStatValue - currentTotalValue;
            _text.text = $"Unspent value: {availableValue:F2}";
        }

        private void OnCarSelected(CarSelectionChangedEventInfo info)
        {
            if (info.IsAnyCarSelected)
                _text.text = _initialText;
        }
    }
}