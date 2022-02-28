// Licensed under MIT.
// Copyright (c) 2016 Snapshot Games Inc.
// See https://github.com/AntonC9018/cui_color_picker
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EngineCommon.ColorPicker
{
    public struct HSV
    {
        public float hue;
        public float saturation;
        public float value;
    }
    public struct ColorInBothFormats
    {
        public Color rgb;
        public HSV hsv;
    }

    public class CUIColorPicker : MonoBehaviour
    {
        [SerializeField] private Image _saturationValueImage;
        // Inferred from the above.
        private RectTransform _saturationValueRectTransform;

        [SerializeField] private RectTransform _saturationValueKnobTransform;
        // Inferred from the above.
        private RectTransform _hueRectTransform;

        [SerializeField] private Image _hueImage;
        [SerializeField] private RectTransform _hueKnobTransform;
        [SerializeField] private Image _resultImage = null;
        [SerializeField] private Color _initialColor = Color.white;

        private enum InputMethod
        {
            Mouse,
            Touch,
        }
        [SerializeField] private InputMethod _usedInputMethod = InputMethod.Mouse;

        [Space] public UnityEvent<ColorInBothFormats> _onValueChangedEvent;


        private enum DragState
        {
            Idle,
            DragHue,
            DragSV,
        }
        private DragState _currentDragState;

        // The elements are immutable.
        private static readonly Color[] _HueColors = new Color[]
        {
            Color.red,
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            Color.magenta,
        };

        // The elements are immutable.
        private static readonly Color[] _ConstantInterpolationColors = new Color[]
        {
            new Color(0, 0, 0),
            new Color(0, 0, 0),
            new Color(1, 1, 1),
        };

        // The last color from which the texture is made is the only one that changes.
        private Color _lastInterpolatedColor;
        
        // Internally, the color is stored as HSV, 
        // but we give the converted color as a value passed to the callback.
        private HSV _currentColor;

        // The texture applied to the saturation-value input area.
        // The texture present in that image will be overwritten.
        private Texture2D _saturationValueTexture;


        /// <summary>
        /// Returns the current color in RGB format.
        /// You should cache the result of the getter, it does a computation.
        /// The setter fires the callbacks as well as resets the knob positions.
        /// </summary>
        Color ColorRGB
        {
            get
            {
                return InterpolateColors(
                    _currentColor.saturation, _currentColor.value, _lastInterpolatedColor);
            }

            set
            {
                ResetToColor(value);

                // Do we want to fire these?
                _onValueChangedEvent?.Invoke(
                    new ColorInBothFormats
                    {
                        hsv = _currentColor,
                        rgb = value,   
                    });
            }
        }

        /// <summary>
        /// Gets the current color, in HSV format.
        /// </summary>
        HSV ColorHSV
        {
            get
            {
                return _currentColor;
            }

            set
            {
                ResetToColor(value);
            }
        }


        private void Awake()
        {
            Debug.Assert(_saturationValueImage != null);
            Debug.Assert(_saturationValueKnobTransform != null);
            Debug.Assert(_hueImage != null);
            Debug.Assert(_hueKnobTransform != null);

            // We allow null for this one.
            // Debug.Assert(_resultImage != null);
            
            // wtf?? Isn't it always those two
            Debug.Assert(
                _usedInputMethod == InputMethod.Touch
                || _usedInputMethod == InputMethod.Mouse,
                "Only touch or mouse supported");

            _lastInterpolatedColor = _HueColors[0];

            var hueTexture = new Texture2D(1, 7);
            for (int i = 0; i < 7; i++)
                hueTexture.SetPixel(0, i, _HueColors[i % 6]);
            hueTexture.Apply();

            _hueImage.sprite = Sprite.Create(
                hueTexture, new Rect(0, 0.5f, 1, 6), new Vector2(0.5f, 0.5f));
            _hueRectTransform = ((RectTransform) _hueImage.transform);

            _saturationValueTexture = new Texture2D(2, 2);
            _saturationValueImage.sprite = Sprite.Create(
                _saturationValueTexture, new Rect(0.5f, 0.5f, 1, 1), new Vector2(0.5f, 0.5f));
            _saturationValueRectTransform = ((RectTransform) _saturationValueImage.transform);
        }

        private void Update()
        {
            if (!gameObject.activeSelf)
                return;

            // The touch object is used if the input method is set to Touch.
            // It's a pretty large struct, but it's not readonly, hence I'm passing it by ref.
            Touch touch = default;
            if (_usedInputMethod == InputMethod.Touch)
            {
                if (Input.touchCount != 1)
                    return;
                touch = Input.GetTouch(0);
            }

            switch (_currentDragState)
            {
                case DragState.Idle:
                {
                    if (!GetInputDown(_usedInputMethod, ref touch))
                        break;

                    // Start dragging the corresponding color thing
                    {
                        if (IsMouseWithin(_hueRectTransform, _usedInputMethod, ref touch))
                            _currentDragState = DragState.DragHue;

                        else if (IsMouseWithin(_saturationValueRectTransform, _usedInputMethod, ref touch))
                            _currentDragState = DragState.DragSV;

                        static bool IsMouseWithin(RectTransform transform, InputMethod inputMethod, ref Touch touch)
                        {
                            Vector2 mousePosition = GetLocalInput(transform, inputMethod, ref touch);
                            return transform.rect.Contains(mousePosition);
                        }
                    }

                    break;
                }

                case DragState.DragHue:
                {
                    var clampedMousePosition = GetClampedLocalInput(_hueRectTransform, _usedInputMethod, ref touch);

                    {
                        var hueSize = _hueRectTransform.rect.size;
                        float hue = clampedMousePosition.y / hueSize.y * 6;

                        if (hue != _currentColor.hue)
                        {
                            var newLastColor = CalculateLastColor(hue);

                            // This could possibly not change if the mouse displacement was tiny, maybe?
                            if (newLastColor != _lastInterpolatedColor)
                            {
                                HSV colorToSend = new HSV
                                {
                                    hue = hue,
                                    saturation = _currentColor.saturation,
                                    value = _currentColor.value,
                                };
                                ApplySaturationValue(colorToSend, newLastColor);

                                _lastInterpolatedColor = newLastColor;
                            }
                            _currentColor.hue = hue;
                        }
                    }

                    _hueKnobTransform.localPosition = new Vector2(_hueKnobTransform.localPosition.x, clampedMousePosition.y);

                    // A final callback may be desirable?
                    if (GetInputUp(_usedInputMethod, ref touch))
                        _currentDragState = DragState.Idle;
                    
                    break;
                }

                case DragState.DragSV:
                {
                    var clampedMousePosition = GetClampedLocalInput(_saturationValueRectTransform, _usedInputMethod, ref touch);

                    {
                        var saturationValueSize = _saturationValueRectTransform.rect.size;
                        float saturation = clampedMousePosition.x / saturationValueSize.x;
                        float value = clampedMousePosition.y / saturationValueSize.y;

                        if (saturation != _currentColor.saturation
                            || value != _currentColor.value)
                        {
                            HSV newColor = new HSV
                            {
                                hue = _currentColor.hue,
                                saturation = saturation,
                                value = value,
                            };
                            ApplySaturationValue(newColor, _lastInterpolatedColor);
                            
                            _currentColor = newColor;
                        }
                    }

                    // I think it can change even without the color changing.
                    _saturationValueKnobTransform.localPosition = clampedMousePosition;

                    // A final callback may be desirable?
                    if (GetInputUp(_usedInputMethod, ref touch))
                        _currentDragState = DragState.Idle;

                    break;
                }
            }
            

            static bool GetInputUp(InputMethod inputMethod, ref Touch touch)
            {
                switch (inputMethod)
                {
                    case InputMethod.Mouse:
                        return Input.GetMouseButtonUp(0);

                    case InputMethod.Touch:
                    {
                        return touch.phase == TouchPhase.Ended
                            || touch.phase == TouchPhase.Canceled;
                    }

                    default:
                    {
                        Debug.Assert(false);
                        return false;
                    }
                }
            }

            static bool GetInputDown(InputMethod inputMethod, ref Touch touch)
            {
                switch (inputMethod)
                {
                    case InputMethod.Mouse:
                        return Input.GetMouseButtonDown(0);

                    case InputMethod.Touch:
                    {
                        return touch.phase != TouchPhase.Ended
                            && touch.phase != TouchPhase.Canceled;
                    }

                    default:
                    {
                        Debug.Assert(false);
                        return false;
                    }
                }
            }

            static Vector2 GetLocalInput(RectTransform rectTransform, InputMethod inputMethod, ref Touch touch)
            {
                switch (inputMethod)
                {
                    case InputMethod.Mouse:
                    {
                        return rectTransform.InverseTransformPoint(Input.mousePosition);
                    }

                    case InputMethod.Touch:
                    {
                        return rectTransform.InverseTransformPoint(touch.position);
                    }

                    default:
                    {
                        Debug.Assert(false, "The only allowed input methods are mouse and touch.");
                        break;
                    }
                }
                return Vector3.positiveInfinity;
            }

            static Vector2 ClampToRectBounds(Vector2 input, Rect rect)
            {
                var min = rect.min;
                var max = rect.max;

                return new Vector2(
                    x: Mathf.Clamp(input.x, min.x, max.x),
                    y: Mathf.Clamp(input.y, min.y, max.y));
            }
            
            static Vector2 GetClampedLocalInput(RectTransform rectTransform, InputMethod inputMethod, ref Touch touch)
            {
                var unclampedMousePosition = GetLocalInput(rectTransform, inputMethod, ref touch);
                return ClampToRectBounds(unclampedMousePosition, rectTransform.rect);
            }
        }

        private void Start()
        {
            // Set the first 4 pixels, the last one gets reset in the method below
            {
                _saturationValueTexture.SetPixel(0, 0, _ConstantInterpolationColors[0]);
                _saturationValueTexture.SetPixel(0, 1, _ConstantInterpolationColors[1]);
                _saturationValueTexture.SetPixel(1, 0, _ConstantInterpolationColors[2]);
            }
            ResetToColor(_initialColor);
        }

        private void ResetToColor(Color colorRGB)
        {
            HSV colorHSV;
            Color.RGBToHSV(colorRGB, out colorHSV.hue, out colorHSV.saturation, out colorHSV.value);
            ResetToColor(colorHSV);
        }

        private void ResetToColor(HSV colorHSV)
        {
            var lastColorRGB = CalculateLastColor(colorHSV.hue);
            UpdateTextureToLastColor(lastColorRGB);

            var newColorRGB = InterpolateColors(colorHSV.saturation, colorHSV.value, lastColorRGB);

            if (_resultImage != null)
                _resultImage.color = newColorRGB;

            _lastInterpolatedColor = lastColorRGB;

            var saturationValueSize = _saturationValueRectTransform.rect.size;
            _saturationValueKnobTransform.localPosition = new Vector2(
                colorHSV.saturation * saturationValueSize.x, colorHSV.value * saturationValueSize.y);
            _hueKnobTransform.localPosition = new Vector2(
                _hueKnobTransform.localPosition.x, colorHSV.hue / 6 * saturationValueSize.y);

            _currentColor = colorHSV;
        }

        private static Color CalculateLastColor(float colorHue)
        {
            int i0 = Mathf.Clamp((int) colorHue, 0, _HueColors.Length - 1);
            int i1 = (i0 + 1) % _HueColors.Length;
            return Color.Lerp(_HueColors[i0], _HueColors[i1], colorHue - i0);
        }

        private void UpdateTextureToLastColor(Color newLastInterpolationColor)
        {
            // The bottom-right coordinate is the only non-constant one.
            _saturationValueTexture.SetPixel(1, 1, newLastInterpolationColor);
            _saturationValueTexture.Apply();
        }

        // TODO: read this math
        private static Color InterpolateColors(float colorSaturation, float colorValue, Color lastInterpolatedColor)
        {
            Vector2 sv = new Vector2(colorSaturation, colorValue);
            Vector2 isv = new Vector2(1 - sv.x, 1 - sv.y);
            Color c0 = isv.x * isv.y * _ConstantInterpolationColors[0];
            Color c1 = sv.x * isv.y * _ConstantInterpolationColors[1];
            Color c2 = isv.x * sv.y * _ConstantInterpolationColors[2];
            Color c3 = sv.x * sv.y * lastInterpolatedColor;
            Color resultColor = c0 + c1 + c2 + c3;
            return resultColor;
        }

        private void ApplySaturationValue(HSV colorToSend, Color lastInterpolationColor)
        {
            // Can only ever hit this if the mouse didn't move, so it should be handled in the Update.
            Debug.Assert(_currentColor.saturation != colorToSend.saturation
                || _currentColor.value != colorToSend.value
                || _lastInterpolatedColor != lastInterpolationColor,
                "Only invoke ApplySaturationValue after having checked if the color parameters have changed.");

            Color newColor = InterpolateColors(colorToSend.saturation, colorToSend.value, lastInterpolationColor);
            UpdateTextureToLastColor(lastInterpolationColor);
            
            if (_resultImage != null)
                _resultImage.color = newColor;


            _onValueChangedEvent?.Invoke(
                new ColorInBothFormats
                {
                    hsv = colorToSend,
                    rgb = newColor,   
                });
        }
    }
}
