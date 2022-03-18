using UnityEngine;
using TMPro;

namespace Race.Garage
{
    // There must be a way to generate such classes automatically,
    // they are kind of stupid tbh.
    public class UserCurrencyDisplay : MonoBehaviour, InitializationHelper.IInitialize
    {
        [SerializeField] private TMP_Text _coinText;
        [SerializeField] private TMP_Text _rubyText;

        private void Display(Currency currency)
        {
            _coinText.text = currency.coins.ToString();
            _rubyText.text = currency.rubies.ToString();
        }

        public void OnCurrencyChanged(UserPropertyChangedEventInfo<Currency> info)
        {
            Display(info.newValue);
        }

        public void Initialize(in InitializationHelper.Properties properties)
        {
            var userProperties = properties.user;
            Display(userProperties.DataModel.currency);
            userProperties.OnCurrencyChanged.AddListener(OnCurrencyChanged);
        }
    }
}