using UnityEngine;
using TMPro;
using static EngineCommon.Assertions;
using EngineCommon;
using UnityEngine.Serialization;

namespace Race.Gameplay
{
    [System.Serializable]
    public struct RadialValueDisplayConfiguration
    {
        // TODO: these 3
        public float minValue;
        public float maxValue;
        public float gap;


        public int pipsPerText;
        public float textOffsetFromPipInRadii;
    }

    public class RadialValueDisplay : MonoBehaviour
    {
        [SerializeField] private RadialPipsWidget _pips;
        [SerializeField] private RadialValueDisplayConfiguration _configuration;
        [SerializeField] private GameObject _textGameObjectPrefab;
        private TMP_Text[] _valueTexts;

        void Awake()
        {
            int pipsCount = _pips.PipConfiguration.largePipInfo.count;
            int textCount = pipsCount / _configuration.pipsPerText;
            _valueTexts = new TMP_Text[textCount];

            var circle = ((RectTransform) transform).GetCircleInfo();
            var pipsGeometryInfo = _pips.GetGeometryInfo(circle);
            var largePipsOffsetEnumerator = pipsGeometryInfo.EnumerateLargePipOffsets();

            while (largePipsOffsetEnumerator.MoveNext())
            {
                if (largePipsOffsetEnumerator.Index % _configuration.pipsPerText != 0)
                    continue;
                var largePipInfo = largePipsOffsetEnumerator.Current;

                var textGameObject = GameObject.Instantiate(_textGameObjectPrefab);
                {
                    var textTransform = (RectTransform) textGameObject.transform;
                    var pipBottomPosition = largePipInfo.offset + circle.center;
                    var moreOffset = largePipInfo.normal * _configuration.textOffsetFromPipInRadii * circle.radius;
                    textTransform.localPosition = pipBottomPosition - moreOffset;
                    textTransform.SetParent(transform, worldPositionStays: false);
                }
                {
                    var textComponent = textGameObject.GetComponent<TMP_Text>();
                    assert(textComponent != null);
                    _valueTexts[largePipsOffsetEnumerator.Index / _configuration.pipsPerText] = textComponent;
                }
            }
        }
    }
}