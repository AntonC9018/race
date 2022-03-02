using UnityEngine;
using TMPro;

namespace Race.Garage
{
    // There must be a way to generate such classes automatically,
    // they are kind of stupid tbh.
    public class UserNicknameDisplay : MonoBehaviour
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

        public void OnUserDataModelLoaded(UserDataModel model)
        {
            _text.text = model.nickname;
        }
    }
}