using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static EngineCommon.Assertions;

namespace Race.Garage
{
    // TODO:
    // Can be repurposed for buying rubies too, if needed.
    // I guess we'd just have a `Currency` instead of coins, display only those which aren't zero.
    [System.Serializable]
    public struct BuyCoinsOptionData : IComparable<BuyCoinsOptionData>
    {
        // The recommended datatype is decimal, but Unity cannot serialize decimal, what?
        [InspectorName("Money (in Dollars)")] 
        public float money;

        public int coins;

        public readonly int CompareTo(BuyCoinsOptionData other)
        {
            return coins - other.coins;
        }
    }

    public class BuyCoinsOptionsManager : MonoBehaviour, InitializationHelper.IInitialize
    {
        /// <summary>
        /// The container in which to spawn the buttons.
        /// </summary>
        [SerializeField] private RectTransform _containerTransform;

        /// <summary>
        /// Has to have a button component and a text component.
        /// </summary>
        [SerializeField] private GameObject _optionPrefab;

        /// <summary>
        /// Description of each option.
        /// </summary>
        [ContextMenuItem("Sort in ascending order", nameof(SortOptions))]
        [SerializeField] private BuyCoinsOptionData[] _optionDatas;

        private UserProperties _userProperties;
        
        private void SortOptions()
        {
            Array.Sort(_optionDatas);
        }

        /// <summary>
        /// The instantiated buttons.
        /// </summary>
        private Button[] _buttons;

        /// <summary>
        /// The callbacks added to the buttons.
        /// </summary>
        private UnityAction[] _buttonClickedDelegates;

        void Awake()
        {
            assert(_optionDatas is not null);
            assert(_containerTransform != null);
            assert(_optionPrefab != null);

            _buttons = new Button[_optionDatas.Length];

            for (int i = 0; i < _optionDatas.Length; i++)
            {
                var gameObject = GameObject.Instantiate(_optionPrefab);
                var newTransform = gameObject.transform;

                {
                    var button = newTransform.GetComponentInChildren<Button>();
                    assert(button != null);
                    _buttons[i] = button;
                }

                {
                    var text = newTransform.GetComponentInChildren<TMP_Text>();
                    assert(text != null);
                    
                    var option = _optionDatas[i];
                    text.text = $"Buy {option.coins} coins for ${option.money:F2}";
                }

                gameObject.transform.SetParent(_containerTransform);
            }
        }

        public UnityAction GetButtonClickedDelegate(int index)
        {
            return delegate()
            {
                var option = _optionDatas[index];
                Debug.Log($"You just bought {option.coins} coins for ${option.money:F2}.");

                _userProperties.DataModel.currency.coins += option.coins;
                _userProperties.TriggerCurrencyChanged();
            };
        }

        public void Initialize(in InitializationHelper.Properties properties)
        {
            _userProperties = properties.user;
            _buttonClickedDelegates = new UnityAction[_buttons.Length];
            for (int i = 0; i < _buttons.Length; i++)
            {
                var deleg = GetButtonClickedDelegate(i);
                _buttons[i].onClick.AddListener(deleg);
                _buttonClickedDelegates[i] = deleg;
            }
        }
    }
}
