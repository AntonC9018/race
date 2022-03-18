using UnityEngine;
using UnityEngine.Events;

namespace Race.Garage
{
    [System.Serializable]
    public struct Currency
    {
        /// <summary>
        /// The main currency, earned in-game
        /// </summary>
        public int coins;

        /// <summary>
        /// Pay-to-win special currency, only bought for real money
        /// or with achievements.
        /// </summary>
        public int rubies;

        public static Currency Zero => new Currency { coins = 0, rubies = 0, };
    }

    [System.Serializable]
    public class UserDataModel
    {
        /// <summary>
        /// </summary>
        public string nickname = "nickname";

        /// <summary>
        /// The name of the last selected car.
        /// </summary>
        public string defaultCarName = "";

        /// <summary>
        /// The current amount of money.
        /// Currency is obtained in-game, or from real money.
        /// </summary>
        public Currency currency = Currency.Zero;
    }

    // TODO:
    // Again, measure whether this being a struct saves any allocations.
    // It definitely does do more copying, because I cannot make `UnityEvent` take by `readonly ref`,
    // unless they somehow detect that this is possible, which I doubt is even possible.
    public readonly struct UserPropertyChangedEventInfo<TProperty>
    {
        public readonly UserProperties userProperties;
        // TODO: We may want to keep the old currency around too, e.g. for animations. Currently unused
        public readonly TProperty oldVaue;
        public readonly TProperty newValue;

        public UserPropertyChangedEventInfo(UserProperties userProperties, TProperty oldVaue, TProperty newValue)
        {
            this.userProperties = userProperties;
            this.oldVaue = oldVaue;
            this.newValue = newValue;
        }
    }

    // TODO:
    // Turn this into a singleton?
    // Advantages:
    //    - Less work in the editor, can set up callbacks in the Start() of each interested class.
    //    - Less fragile design in case the model game object gets deleted.
    //    - With bottom-up design is easier to expand just by adding things,
    //      you don't have to go back and reference it in the initial prefab
    //      (but this aspect can be mitigated by injecting the prefab into the interested components).
    // Disadvantages:
    //    - The context is implicit, it's not immediately clear what objects depend on the singleton.
    //    - Cannot easily accomodate multiple models at once.
    public class UserProperties : MonoBehaviour
    {
        private UserDataModel _dataModel;
        public UserDataModel DataModel => _dataModel;
        private bool _isModelDirty;

        // TODO: load from the server.
        // TODO: validation with the server on every transaction.
        // TODO: maybe allow multiple profiles.

        // TODO:
        // Maybe do reactive setters, but I dislike those personally.
        // I'd much rather trigger the event manually, than have it be done
        // invisibly at a wrong moment.
        public UnityEvent<UserPropertyChangedEventInfo<Currency>> OnCurrencyChanged;
        public UnityEvent<UserPropertyChangedEventInfo<string>> OnNickNameChanged;

        public void TriggerCurrencyChanged()
        {
            var info = new UserPropertyChangedEventInfo<Currency>(
                userProperties: this,
                // TODO:
                oldVaue: Currency.Zero,
                newValue: _dataModel.currency);
            _isModelDirty = true;
            OnCurrencyChanged.Invoke(info);
        }

        public void TriggerNicknameChanged()
        {
            var info = new UserPropertyChangedEventInfo<string>(
                userProperties: this,
                // TODO:
                oldVaue: null,
                newValue: _dataModel.nickname);
            _isModelDirty = true;
            OnNickNameChanged.Invoke(info);
        }

        public void Initialize(CarProperties carProperties)
        {
            carProperties.OnCarSelected.AddListener(OnCarSelectionChanged);
            
            var model = new UserDataModel();
            TryLoadUserModelFromPlayerPrefs(model);
            _dataModel = model;
        }

        void OnDisable()
        {
            if (_isModelDirty)
            {
                SaveUserModelInPlayerPrefs(_dataModel);
                _isModelDirty = false;
            }
        }

        public void OnCarSelectionChanged(CarSelectionChangedEventInfo info)
        {
            if (info.carProperties.IsAnyCarSelected)
                _dataModel.defaultCarName = info.CurrentCarInfo.name;
            else
                _dataModel.defaultCarName = "";
            _isModelDirty = true;
        }
        
        // TODO: generate all of the below automatically.
        private const string prefix = nameof(UserDataModel);
        private const string nickname_Key = prefix + nameof(UserDataModel.nickname);
        private const string defaultCarName_Key = prefix + nameof(UserDataModel.defaultCarName);
        private const string currency_Key = prefix + nameof(UserDataModel.currency);
        private const string currency_coins_Key = currency_Key + nameof(Currency.coins);
        private const string currency_rubies_Key = currency_Key + nameof(Currency.rubies);

        private static bool TryLoadUserModelFromPlayerPrefs(UserDataModel modelToFill)
        {
            if (PlayerPrefs.HasKey(nickname_Key))
            {
                modelToFill.nickname = PlayerPrefs.GetString(nickname_Key);
                modelToFill.defaultCarName = PlayerPrefs.GetString(defaultCarName_Key);
                modelToFill.currency = new Currency
                {
                    coins = PlayerPrefs.GetInt(currency_coins_Key),
                    rubies = PlayerPrefs.GetInt(currency_rubies_Key),
                };
                return true;
            }
            return false;
        }

        private static void SaveUserModelInPlayerPrefs(UserDataModel model)
        {
            PlayerPrefs.SetString(nickname_Key, model.nickname);
            PlayerPrefs.SetString(defaultCarName_Key, model.defaultCarName);
            PlayerPrefs.SetInt(currency_coins_Key, model.currency.coins);
            PlayerPrefs.SetInt(currency_rubies_Key, model.currency.rubies);
        }
    }
}