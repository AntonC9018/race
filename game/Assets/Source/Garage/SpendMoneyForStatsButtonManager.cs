using Kari.Plugins.Flags;
using UnityEngine;
using UnityEngine.UI;
using Race.Garage.Generated;
using static EngineCommon.Assertions;

namespace Race.Garage
{
    public class SpendMoneyForStatsButtonManager : MonoBehaviour, InitializationHelper.IInitialize
    {
        [SerializeField] private Button _button;

        private CarProperties _carProperties;
        private UserProperties _userProperties;
        
        private const float _StatIncreasePerCoin = 10;
        private const int _CoinUseAtATime = 50;

        [NiceFlags]
        public enum PossibilitiesFlags
        {
            NoCarSelected = 1 << 0,
            MaxStatsReached = 1 << 1,
            NotEnoughCoins = 1 << 2,
        }
        private PossibilitiesFlags _currentFlags = 0;

        public void Initialize(in InitializationHelper.Properties properties)
        {
            {
                var car = properties.car;
                car.OnCarSelected.AddListener(OnCarSelected);
                car.OnStatsChanged.AddListener(OnStatsChanged);
                _carProperties = car;
            }
            {
                var user = properties.user;
                user.OnCurrencyChanged.AddListener(OnCurrencyChanged);

                ResetCoinsFlag(user.DataModel);
                
                _userProperties = user;
            }
            _button.onClick.AddListener(TradeCoinsForStatValue);

            ResetButtonInteractability();
        }
        
        private void ResetCoinsFlag(UserDataModel dataModel)
        {
            _currentFlags.Set(
                PossibilitiesFlags.NotEnoughCoins,
                dataModel.currency.coins < _CoinUseAtATime);
        }

        private void ResetButtonInteractability()
        {
            _button.interactable = _currentFlags == 0;
        }

        private void OnCarSelected(CarSelectionChangedEventInfo info)
        {
            _currentFlags.Set(PossibilitiesFlags.NoCarSelected, info.currentIndex < 0);
            ResetButtonInteractability();
        }

        private void OnCurrencyChanged(UserPropertyChangedEventInfo<Currency> info)
        {
            ResetCoinsFlag(info.userProperties.DataModel);
            ResetButtonInteractability();
        }

        private void OnStatsChanged(CarStatsChangedEventInfo info)
        {
            _currentFlags.Set(
                PossibilitiesFlags.MaxStatsReached,
                info.CurrentStatsInfo.totalStatValue >= CarStatsHelper.MaxStatValue);
        }

        public void TradeCoinsForStatValue()
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