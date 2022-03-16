using UnityEngine;
using TMPro;
using static EngineCommon.Assertions;
using EngineCommon;
using System;

namespace Race.Gameplay
{
    [System.Serializable]
    public struct ValueRange
    {
        // Just functional
        public float minValue;
        public float maxValue;
        public readonly float Length => maxValue - minValue;
    }

    [System.Serializable]
    public struct RadialValueVisualConfiguration
    {
        // Purely visual
        [Min(1)]
        public int pipsPerText;
        
        [Range(0, 1)]
        public float textOffsetFromPipInRadii;
    }

    public class RadialValueDisplay : MonoBehaviour
    {
        [SerializeField] private RadialPipsWidget _pips;
        [SerializeField] private RadialValueVisualConfiguration _configuration;
        [SerializeField] private GameObject _textGameObjectPrefab;
        [SerializeField] private RectTransform _needleTransform;
        private TMP_Text[] _valueTexts;


        // TODO: may want to just flatten it into a method, we'll see.
        public void ResetPipsAndTextsToValues(ValueRange valueRange, float largePipGap)
        {
            var textContainer = transform;

            float range = valueRange.maxValue - valueRange.minValue;
            int pipsCount = (int)(range / largePipGap);
            int textCount = MathHelper.CeilDivide(pipsCount, _configuration.pipsPerText);

            // I think the array resizing and reusing thing is taking it too far.
            // This function will probably only be called once anyway.
            int oldTextCount = ResizeArrayOrDisableUnusedChildren(ref _valueTexts, textCount);
            assert(_valueTexts.Length == textCount);

            _pips.PipConfiguration.largePipInfo.count = pipsCount;
            _pips.InvalidatePipInfo();

            var circle = ((RectTransform) _pips.transform).GetCircleInfo();
            var pipsGeometryInfo = _pips.GetGeometryInfo(circle);
            var largePipsOffsetEnumerator = pipsGeometryInfo.EnumerateLargePipOffsets();

            while (largePipsOffsetEnumerator.MoveNext())
            {
                int largePipIndex = largePipsOffsetEnumerator.Index;
                if (largePipIndex % _configuration.pipsPerText != 0)
                    continue;

                var largePipInfo = largePipsOffsetEnumerator.Current;

                {
                    int textIndex = largePipIndex / _configuration.pipsPerText;

                    if (textIndex >= textCount)
                    {
                        assert(false, largePipIndex.ToString());
                    }

                    TMP_Text textComponent;
                    GameObject textGameObject;
                    // Create new / reuse existing game object.
                    // TODO: might want to refactor the array in a wrapper.
                    if (textIndex >= oldTextCount)
                    {
                        textGameObject = GameObject.Instantiate(_textGameObjectPrefab);

                        textComponent = textGameObject.GetComponent<TMP_Text>();
                        assert(textComponent != null);
                        _valueTexts[textIndex] = textComponent;
                    }
                    else
                    {
                        textComponent = _valueTexts[textIndex];
                        textGameObject = textComponent.gameObject;
                        textGameObject.SetActive(true);
                    }

                    {
                        var textTransform = (RectTransform) textComponent.transform;
                        var pipBottomPosition = largePipInfo.offset + circle.center;
                        var moreOffset = largePipInfo.normal * _configuration.textOffsetFromPipInRadii * circle.radius;
                        textTransform.localPosition = pipBottomPosition - moreOffset;
                        textTransform.SetParent(textContainer, worldPositionStays: false);
                    }

                    {
                        float valueOffset = largePipIndex * largePipGap;
                        int value = Mathf.RoundToInt(valueRange.minValue + valueOffset);
                        var text = value.ToString();
                        textComponent.text = text;
                        textGameObject.name = text;
                    }
                }
            }

            // Returns the old length
            static int ResizeArrayOrDisableUnusedChildren(ref TMP_Text[] inoutValueTexts, int textCount)
            {
                if (inoutValueTexts is null)
                {
                    inoutValueTexts = new TMP_Text[textCount];
                    return 0;
                }

                int oldTextCount = inoutValueTexts.Length;

                if (oldTextCount > textCount)
                {
                    for (int i = textCount; i < oldTextCount; i++)
                        inoutValueTexts[i].gameObject.SetActive(false);
                }
                else if (oldTextCount < textCount)
                {
                    Array.Resize(ref inoutValueTexts, textCount);
                }

                return oldTextCount;
            }
        }

        public void ResetNeedleRotation(float newValueNormalized)
        {
            // TODO:
            // This is wrong. I think the best way to go about it is to have the pips cache the geometry info,
            // which will be initialized by resetting it first from the external code, aka by setting the visual
            // configuration with a setter?
            // Another way would be to just serialize it here, in this object.
            // This one is weird, we'll have to see.
            var visualConfiguration = _pips._visualConfiguration;

            float rotationSignedAngleRangeLength = visualConfiguration.SignedAngleRangeLength;
            float rotationOffset = rotationSignedAngleRangeLength * newValueNormalized;
            float rotationAngle = rotationOffset + visualConfiguration.MinAngle;

            _needleTransform.rotation = Quaternion.AngleAxis(rotationAngle * Mathf.Rad2Deg, Vector3.forward);
        }
    }
}