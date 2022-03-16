using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public interface ICameraInputView
    {
        void AddOnCameraSwitchedListener(Action<InputAction.CallbackContext> action);
        void RemoveOnCameraSwitchedListener(Action<InputAction.CallbackContext> action);
    }

    public class CameraControl : MonoBehaviour
    {
        private Transform _followedTransform;
        private ICameraInputView _inputView;
        private int _currentCameraIndex;

        #if DEBUG
            private bool _hasBeenInitialized;
        #endif        

        /// <summary>
        /// Only use this before the Start has been called.
        /// </summary>
        // TODO: allow resetting? so that we can easily reuse objects.
        public void Initialize(Transform followedTransform, ICameraInputView cameraInputView)
        {
            #if DEBUG
                assert(!_hasBeenInitialized);
            #endif
            _followedTransform = followedTransform;
            _inputView = cameraInputView;
        }

        void Awake()
        {
            _currentCameraIndex = 0;
            var transform = this.transform;
            var childCount = transform.childCount;
            assert(childCount > 0, "No cameras?");
            
            transform.GetChild(0).gameObject.SetActive(true);
            for (int i = 1; i < childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);
        }

        void Start()
        {
            _hasBeenInitialized = true;
            
            assert(_followedTransform != null);
            assert(_inputView is not null);

            _inputView.AddOnCameraSwitchedListener(OnSwitchCamera);
        }

        void Destroy()
        {
            if (_inputView != null)
                _inputView.RemoveOnCameraSwitchedListener(OnSwitchCamera);
        }

        private void OnSwitchCamera(InputAction.CallbackContext callbackContext)
        {
            assert(callbackContext.performed);

            var transform = this.transform;
            int newIndex = (_currentCameraIndex + 1) % transform.childCount;
            transform.GetChild(_currentCameraIndex).gameObject.SetActive(false);
            transform.GetChild(newIndex).gameObject.SetActive(true);
            _currentCameraIndex = newIndex;
        }

        // Does not need to be `LateUpdate`, because we're handling movement in `FixedUpdate`.
        void Update()
        {
            // TODO: something fancier
            transform.SetPositionAndRotation(_followedTransform.position, _followedTransform.rotation);
        }
    }

    // I think this is an unnecessary abstraction, actions are good enough of an abstraction already.
    public class CameraKeyboardInputView : ICameraInputView
    {
        private CarControls.PlayerActions _player;

        public CameraKeyboardInputView(CarControls.PlayerActions player)
        {
            _player = player;
        }

        // For now do it here
        public void AddOnCameraSwitchedListener(Action<InputAction.CallbackContext> action)
        {
            _player.SwitchCamera.performed += action;
        }

        public void RemoveOnCameraSwitchedListener(Action<InputAction.CallbackContext> action)
        {
            _player.SwitchCamera.performed -= action;
        }
    }
}
