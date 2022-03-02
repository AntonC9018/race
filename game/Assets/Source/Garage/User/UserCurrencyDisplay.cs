using UnityEngine;
using TMPro;

namespace Race.Garage
{
    // There must be a way to generate such classes automatically,
    // they are kind of stupid tbh.
    public class UserCurrencyDisplay : MonoBehaviour
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

        public void OnUserDataModelLoaded(UserDataModel model)
        {
            Display(model.currency);
        }
    }
}