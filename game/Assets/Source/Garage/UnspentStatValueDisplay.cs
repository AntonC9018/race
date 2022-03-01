using UnityEngine;
using TMPro;

namespace Race.Garage
{
    [RequireComponent(typeof(TMP_Text))]
    public class UnspentStatValueDisplay : MonoBehaviour
    {
        private TMP_Text _text;
        private string _initialText;

        void Start()
        {
            _text = GetComponent<TMP_Text>();
            _initialText = _text.text;
        }

        public void OnStatsChanged(CarStatsChangedEventInfo info)
        {
            if (info.carProperties.IsAnyCarSelected)
            {
                ref var statsInfo = ref info.CurrentStatsInfo;
                float currentTotalValue = statsInfo.currentStats.GetTotalValue();
                float availableValue = statsInfo.totalStatValue - currentTotalValue;
                _text.text = $"Unspent value: {availableValue:F2}";
            }
            else
            {
                _text.text = _initialText;
            }
        }
    }
}