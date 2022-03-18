using UnityEngine;
using TMPro;

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


        private void ResetThings(CarProperties carProperties)
        {
            if (carProperties.IsAnyCarSelected)
            {
                ref var statsInfo = ref carProperties.CurrentCarInfo.dataModel.statsInfo;
                float currentTotalValue = statsInfo.currentStats.GetTotalValue();
                float availableValue = statsInfo.totalStatValue - currentTotalValue;
                _text.text = $"Unspent value: {availableValue:F2}";
            }
            else
            {
                _text.text = _initialText;
            }
        }

        public void OnStatsChanged(CarStatsChangedEventInfo info)
        {
            ResetThings(info.carProperties);
        }

        public void Initialize(in InitializationHelper.Properties properties)
        {
            var carProperties = properties.car;
            carProperties.OnStatsChanged.AddListener(OnStatsChanged);
            ResetThings(carProperties);
        }
    }
}