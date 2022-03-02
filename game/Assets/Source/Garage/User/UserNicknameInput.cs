using UnityEngine;
using TMPro;

namespace Race.Garage
{
    // There must be a way to generate such classes automatically,
    // they are kind of stupid tbh.
    public class UserNicknameInput : MonoBehaviour
    {
        [SerializeField] private UserProperties _userProperties;

        // Here the data binding is one way, so we don't need to listen to the event too.
        public void OnNicknameInput(string value)
        {
            _userProperties.DataModel.nickname = value;
            _userProperties.TriggerNicknameChanged();
        }
    }
}