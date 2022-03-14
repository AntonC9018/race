using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public class CameraControl : MonoBehaviour
    {
        [SerializeField] private Transform _followedTransform;
        [SerializeField] private InputManager _inputManager;
        private int _currentCameraIndex;

        void Awake()
        {
            assert(_followedTransform != null);
            assert(_inputManager is not null);

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
            _inputManager.CarControls.Player.SwitchCamera.performed += OnSwitchCamera;
        }

        void Destroy()
        {
            if (_inputManager != null)
                _inputManager.CarControls.Player.SwitchCamera.performed -= OnSwitchCamera;
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
}
