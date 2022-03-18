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

                _currentFlags.Set(PossibilitiesFlags.NoCarSelected, car.IsAnyCarSelected);
                if (car.IsAnyCarSelected)
                    ResetMaxStatsFlag(ref car.CurrentCarInfo);

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
        private void OnDataModelLoaded(UserDataModel dataModel)
        {
            ResetButtonInteractability();
        }

        private void OnCarsInitialized(CarProperties properties)
        {
            ResetButtonInteractability();
        }

        private void ResetMaxStatsFlag(ref CarInstanceInfo carInfo)
        {
            _currentFlags.Set(
                PossibilitiesFlags.MaxStatsReached,
                carInfo.dataModel.statsInfo.totalStatValue >= CarStatsHelper.MaxStatValue);
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

        public void OnCarSelected(CarSelectionChangedEventInfo info)
        {
            _currentFlags.Set(PossibilitiesFlags.NoCarSelected, info.currentIndex < 0);

            if (info.currentIndex >= 0)
                ResetMaxStatsFlag(ref info.CurrentCarInfo);

            ResetButtonInteractability();
        }

        public void OnCurrencyChanged(UserPropertyChangedEventInfo<Currency> info)
        {
            ResetCoinsFlag(info.userProperties.DataModel);
            ResetButtonInteractability();
        }

        public void OnStatsChanged(CarStatsChangedEventInfo info)
        {
            // Not implemented, because the additional value changes nowhere else but here.
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