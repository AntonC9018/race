using UnityEngine;
using TMPro;

namespace Race.Garage
{
    // There must be a way to generate such classes automatically,
    // they are kind of stupid tbh.
    public class UserNicknameDisplay : MonoBehaviour, InitializationHelper.IInitialize
    {
        private TMP_Text _text;

        void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        public void OnNicknameChanged(UserPropertyChangedEventInfo<string> info)
        {
            _text.text = info.newValue;
        }

        public void Initialize(in InitializationHelper.Properties properties)
        {
            var userProperties = properties.user;
            _text.text = userProperties.DataModel.nickname;
            userProperties.OnNickNameChanged.AddListener(OnNicknameChanged);
        }
    }
}