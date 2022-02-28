// Licensed under MIT.
// Copyright (c) 2016 Snapshot Games Inc.
// See https://github.com/AntonC9018/cui_color_picker
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EngineCommon.ColorPicker
{
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

        // Can be reset right after the thing is spawned from script.
        // If you want to trigger the events too, use the ColorRGB setter instead.
        // TODO: this should use a custom property setter, I've seen it showcased somewhere.
        [SerializeField] public Color initialColor = Color.white;

        private enum InputMethod
        {
            Mouse,
            Touch,
        }
        [SerializeField] private InputMethod _usedInputMethod = InputMethod.Mouse;

        [Space] public UnityEvent<Color> OnValueChangedEvent;


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
        // private static readonly Color[] _ConstantInterpolationColors = new Color[]
        // {
        //     new Color(0, 0, 0),
        //     new Color(0, 0, 0),
        //     new Color(1, 1, 1),
        // };

        // The last color from which the texture is made is the only one that changes.
        private Vector3 _lastInterpolatedColor;
        
        private struct HSV
        {
            public float hue;
            public float saturation;
            public float value;
            public static HSV Invalid => new HSV { hue = -1, saturation = 0, value = 0 };
            public bool IsValid => this.hue >= 0;
        }
        // Internally, the color is stored as HSV, 
        // but we give the converted color as a value passed to the callback.
        private HSV _currentColor = HSV.Invalid;

        // A property needed to prevent double-initialization.
        private bool HasInited => _currentColor.IsValid;

        // The texture applied to the saturation-value input area.
        // The texture present in that image will be overwritten.
        private Texture2D _saturationValueTexture;

        /// <summary>
        /// Returns the current color in RGB format.
        /// You should cache the result of the getter, it does a computation.
        /// The setter fires the callbacks as well as resets the knob positions.
        /// </summary>
        public Color ColorRGB
        {
            get
            {
                return InterpolateColors(
                    _currentColor.saturation, _currentColor.value, _lastInterpolatedColor)
                        .FromRGBVector();
            }

            set
            {
                // Possibly a hack, used to prevent stuff.
                if (HasInited)
                    ResetToColor(value);
                else
                    Initialize(value);

                // Do we always want to fire these?
                OnValueChangedEvent?.Invoke(value);
            }
        }
        
        private void Start()
        {
            if (!HasInited)
                Initialize(initialColor);
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

            // red
            _lastInterpolatedColor = _HueColors[0].ToRBGVector();

            var hueTexture = new Texture2D(1, _HueColors.Length);
            for (int i = 0; i < _HueColors.Length; i++)
                hueTexture.SetPixel(0, i, _HueColors[i]);
            hueTexture.Apply();
            
            _hueImage.sprite = Sprite.Create(
                hueTexture,
                new Rect(0, 0.5f, 1, _HueColors.Length - 1),
                pivot: new Vector2(0.5f, 0.5f));
            _hueRectTransform = ((RectTransform) _hueImage.transform);

            _saturationValueTexture = new Texture2D(2, 2);
            _saturationValueImage.sprite = Sprite.Create(
                _saturationValueTexture,
                // The texture assigns pixel values to *centers* of the pixels,
                // so we cut 3/4 of each pixel away, because otherwise the outer parts of 
                // those would get lerped too, which we don't want.
                /*
                    .___________.
                    |  x__|__x  |
                    |__|     |__|
                    |  x_____x  |
                    ._____|_____.
                */
                new Rect(0.5f, 0.5f, 1, 1),
                pivot: new Vector2(0.5f, 0.5f));
            _saturationValueRectTransform = ((RectTransform) _saturationValueImage.transform);
        }

        private void Update()
        {
            if (!gameObject.activeSelf)
                return;

            // The touch object is used if the input method is set to Touch.
            // It's a pretty large struct, but it's not readonly, hence I'm passing it by ref (via the capture).
            Touch touch = default;
            InputMethod inputMethod = _usedInputMethod;
            if (inputMethod == InputMethod.Touch)
            {
                if (Input.touchCount != 1)
                    return;
                touch = Input.GetTouch(0);
            }

            switch (_currentDragState)
            {
                case DragState.Idle:
                {
                    if (!GetInputDown())
                        break;

                    // Start dragging the corresponding color thing
                    {
                        if (IsMouseWithin(_hueRectTransform))
                            _currentDragState = DragState.DragHue;

                        else if (IsMouseWithin(_saturationValueRectTransform))
                            _currentDragState = DragState.DragSV;

                        bool IsMouseWithin(RectTransform transform)
                        {
                            Vector2 mousePosition = GetLocalInput(transform);
                            return transform.rect.Contains(mousePosition);
                        }
                    }

                    break;
                }

                case DragState.DragHue:
                {
                    var clampedMousePosition = GetClampedLocalInput(_hueRectTransform);

                    {
                        var hueSize = _hueRectTransform.rect.size;
                        float hue = clampedMousePosition.y / hueSize.y;

                        if (!Mathf.Approximately(hue, _currentColor.hue))
                        {
                            var newLastColor = CalculateLastColor(hue).ToRBGVector();

                            // This could possibly not change if the mouse displacement was tiny, maybe?
                            if (newLastColor != _lastInterpolatedColor)
                            {
                                ApplySaturationValue(_currentColor.saturation, _currentColor.value, newLastColor);

                                _lastInterpolatedColor = newLastColor;
                            }
                            _currentColor.hue = hue;
                        }
                    }

                    _hueKnobTransform.localPosition = new Vector2(_hueKnobTransform.localPosition.x, clampedMousePosition.y);

                    // A final callback may be desirable?
                    if (GetInputUp())
                        _currentDragState = DragState.Idle;
                    
                    break;
                }

                case DragState.DragSV:
                {
                    var clampedMousePosition = GetClampedLocalInput(_saturationValueRectTransform);

                    {
                        var saturationValueSize = _saturationValueRectTransform.rect.size;
                        float value = clampedMousePosition.x / saturationValueSize.x;
                        float saturation = clampedMousePosition.y / saturationValueSize.y;

                        if (!Mathf.Approximately(saturation, _currentColor.saturation)
                            || !Mathf.Approximately(value, _currentColor.value))
                        {
                            ApplySaturationValue(saturation, value, _lastInterpolatedColor);
                            
                            _currentColor.saturation = saturation;
                            _currentColor.value = value;
                        }
                    }

                    // I think it can change even without the color changing.
                    _saturationValueKnobTransform.localPosition = clampedMousePosition;

                    // A final callback may be desirable?
                    if (GetInputUp())
                        _currentDragState = DragState.Idle;

                    break;
                }
            }
            

            bool GetInputUp()
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

            bool GetInputDown()
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

            Vector2 GetLocalInput(RectTransform rectTransform)
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
            
            Vector2 GetClampedLocalInput(RectTransform rectTransform)
            {
                var unclampedMousePosition = GetLocalInput(rectTransform);
                return ClampToRectBounds(unclampedMousePosition, rectTransform.rect);
            }
        }

        private void Initialize(Color color)
        {
            // Set the first 4 pixels, the last one gets reset in the method below
            {
                // _saturationValueTexture.SetPixel(0, 0, _ConstantInterpolationColors[0]);
                // _saturationValueTexture.SetPixel(0, 1, _ConstantInterpolationColors[1]);
                // _saturationValueTexture.SetPixel(1, 0, _ConstantInterpolationColors[2]);
                
                _saturationValueTexture.SetPixel(0, 0, Color.black);
                _saturationValueTexture.SetPixel(0, 1, Color.black);
                _saturationValueTexture.SetPixel(1, 0, Color.white);
            }
            ResetToColor(color);
        }

        private void ResetToColor(Color colorRGB)
        {
            HSV colorHSV;
            Color.RGBToHSV(colorRGB, out colorHSV.hue, out colorHSV.saturation, out colorHSV.value);
            ResetToColor(colorHSV);
        }

        private void ResetToColor(HSV colorHSV)
        {
            {
                var lastColorRGB = CalculateLastColor(colorHSV.hue);
                UpdateTextureToLastColor(lastColorRGB);

                var lastColorVector = lastColorRGB.ToRBGVector();
                var newColorRGB = InterpolateColors(colorHSV.saturation, colorHSV.value, lastColorVector);
                _lastInterpolatedColor = lastColorVector;

                if (_resultImage != null)
                    _resultImage.color = newColorRGB.FromRGBVector();
            }
            {
                var saturationValueSize = _saturationValueRectTransform.rect.size;
                _saturationValueKnobTransform.localPosition = new Vector2(
                    colorHSV.value * saturationValueSize.x, colorHSV.saturation * saturationValueSize.y);
                                    
                var hueSize = _hueRectTransform.rect.size;
                _hueKnobTransform.localPosition = new Vector2(
                    _hueKnobTransform.localPosition.x, colorHSV.hue * hueSize.y);

                _currentColor = colorHSV;
            }
        }

        private static Color CalculateLastColor(float colorHue)
        {
            float m = colorHue * _HueColors.Length;
            int i0 = Mathf.Clamp((int) m, 0, _HueColors.Length - 1);
            int i1 = (i0 + 1) % _HueColors.Length;
            return Color.Lerp(_HueColors[i0], _HueColors[i1], m - i0);
        }

        private void UpdateTextureToLastColor(Color newLastInterpolationColor)
        {
            // The bottom-right coordinate is the only non-constant one.
            _saturationValueTexture.SetPixel(1, 1, newLastInterpolationColor);
            _saturationValueTexture.Apply();
        }

        // TODO: read this math
        private static Vector3 InterpolateColors(float colorSaturation, float colorValue, Vector3 lastInterpolationColorRGB)
        {
            // Vector2 sv = new Vector2(colorSaturation, colorValue);
            // Vector2 isv = new Vector2(1 - sv.x, 1 - sv.y);
            // Color c0 = isv.x * isv.y * _ConstantInterpolationColors[0];
            // Color c1 = sv.x * isv.y * _ConstantInterpolationColors[1];
            // Color c2 = isv.x * sv.y * _ConstantInterpolationColors[2];
            // Color c3 = sv.x * sv.y * lastInterpolationColor;
            // Color resultColor = c0 + c1 + c2 + c3;
            // return resultColor;

            Vector3 white = new Vector3(1, 1, 1);
            return (1 - colorSaturation) * colorValue * white
                + colorSaturation * colorValue * lastInterpolationColorRGB;
        }

        private void ApplySaturationValue(float saturation, float value, Vector3 lastInterpolationColor)
        {
            // Can only ever hit this if the mouse didn't move, so it should be handled in the Update.
            Debug.Assert(_currentColor.saturation != saturation
                || _currentColor.value != value
                || _lastInterpolatedColor != lastInterpolationColor,
                "Only invoke ApplySaturationValue after having checked if the color parameters have changed.");

            Color newColor = InterpolateColors(saturation, value, lastInterpolationColor).FromRGBVector();
            UpdateTextureToLastColor(lastInterpolationColor.FromRGBVector());
            
            if (_resultImage != null)
                _resultImage.color = newColor;

            OnValueChangedEvent?.Invoke(newColor);
        }
    }
}
