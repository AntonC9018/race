using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VectorGraphics;

namespace Race.Garage
{
    // NOTE: Using SVGImage with the button component is buggy, plus, we need to emulate
    // being pushed down anyway.

    [RequireComponent(typeof(SVGImage))]
    public class PedestalRotation : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private PedestalRotationParams _rotationParams; 
        /// <summary>
        /// The direction doesn't necessarily have to be -1 or 1, 
        /// it can also indicate the amount by having a larger value.
        /// It will be scaled by the internal factor.
        /// </summary>
        [SerializeField] private float _direction;

        // TODO: It's a very good candidate for a singleton, as much as I dislike those.
        [SerializeField] private CarProperties _carProperties;

        public void Rotate()
        {
            var rotationAxis = Vector3.up;
            if (_carProperties.IsAnyCarSelected)
            {
                // The rotation should also be a reactive property of the object??
                // I think it shouldn't, so this usage imo is totally fine.
                // We can't cache the transform too, because the object might change.
                _carProperties.CurrentCarInfo.rootObject.transform
                    .Rotate(rotationAxis, _rotationParams.rotationScale * _direction);
            }
        }


        // TODO: Do something fancy with an input manager and callbacks.
        // While doing the nonsense below is fine for performance for a couple of objects,
        // we have a more fundamental problem here.
        private bool _isPointerOver;

        // TODO: SVG image is too limited. Probably need to do somthing custom.
        //
        // Idea:
        // Additively blend the arrows sprites that are pressed down with a white texture
        // to get the desired effect.
        //
        // Another idea:
        // Figure out what they do to render the SVG with their API (little luck so far),
        // export 2 textures, one lighter and a normal one. Substitute the sprite (or the uniforms)
        // when drawing it. The thing is, the actual shader calls are hidden after so many layers of
        // abstraction that it's hard for me to keep track of what's really happening.
        private SVGImage _svgImageComponent;

        private void Setup()
        {
            _svgImageComponent = GetComponent<SVGImage>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerOver = false;
        }

        public void FixedUpdate()
        {
            if (!_isPointerOver)
                return;
            
            if (Input.GetMouseButton(0))
                Rotate();
        }
    }
}