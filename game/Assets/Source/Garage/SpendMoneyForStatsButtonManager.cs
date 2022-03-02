using Kari.Plugins.Flags;
using UnityEngine;
using UnityEngine.UI;
using Race.Garage.Generated;
using static EngineCommon.Assertions;

namespace Race.Garage
{
    public class SpendMoneyForStatsButtonManager : MonoBehaviour
    {
        [SerializeField] private UserProperties _userProperties;
        [SerializeField] private CarProperties _carProperties;
        [SerializeField] private Button _button;
        private const float _StatIncreasePerCoin = 1;
        private const int _CoinUseAtATime = 1;

        [NiceFlags]
        public enum PossibilitiesFlags
        {
            NoCarSelected = 1 << 0,
            MaxStatsReached = 1 << 1,
            NotEnoughCoins = 1 << 2,
        }
        private PossibilitiesFlags _currentFlags = 0;

        void Awake()
        {
        }

        void OnEnable()
        {
            _carProperties.OnCarSelected.AddListener(OnCarSelected);
            _carProperties.OnStatsChanged.AddListener(OnStatsChanged);
            _userProperties.OnCurrencyChanged.AddListener(OnCurrencyChanged);
            _button.onClick.AddListener(TradeCoinForStat);
        }

        void OnDisable()
        {
            _carProperties.OnCarSelected.RemoveListener(OnCarSelected);
            _carProperties.OnStatsChanged.RemoveListener(OnStatsChanged);
            _userProperties.OnCurrencyChanged.RemoveListener(OnCurrencyChanged);
            _button.onClick.RemoveListener(TradeCoinForStat);
        }

        public void OnCarSelected(CarSelectionChangedEventInfo info)
        {
            _currentFlags.Set(PossibilitiesFlags.NoCarSelected, info.currentIndex < 0);

            if (info.currentIndex >= 0)
            {
                ref var statsInfo = ref info.CurrentCarInfo.dataModel.statsInfo;
                _currentFlags.Set(
                    PossibilitiesFlags.MaxStatsReached,
                    statsInfo.totalStatValue >= CarStatsHelper.MaxStatValue);
            }
            
            _button.interactable = _currentFlags == 0;
        }

        public void OnCurrencyChanged(UserPropertyChangedEventInfo<Currency> info)
        {
            _currentFlags.Set(
                PossibilitiesFlags.NotEnoughCoins,
                info.userProperties.DataModel.currency.coins < _CoinUseAtATime);
            
            _button.interactable = _currentFlags == 0;
        }

        public void OnStatsChanged(CarStatsChangedEventInfo info)
        {
            // Not implemented, because the additional value changes nowhere else but here.
        }

        public void TradeCoinForStat()
        {
            assert(_currentFlags == 0, "You can't trust the button??");
            ref var coins = ref _userProperties.DataModel.currency.coins;
            assert(coins >= _CoinUseAtATime);
            coins -= _CoinUseAtATime;
            _userProperties.TriggerCurrencyChanged();

            assert(_carProperties.IsAnyCarSelected);
            ref var statsInfo = ref _carProperties.CurrentCarInfo.dataModel.statsInfo;
            statsInfo.additionalStatValue += _StatIncreasePerCoin;
            statsInfo.ComputeNonSerializedProperties();
            _carProperties.TriggerStatsChangedEvent();
        }
    }
}