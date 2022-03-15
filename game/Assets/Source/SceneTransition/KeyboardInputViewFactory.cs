using UnityEngine;
using Race.Gameplay;
using static EngineCommon.Assertions;

namespace Race.SceneTransition
{
    public class KeyboardInputViewFactory : MonoBehaviour, IInputViewFactory
    {
        [SerializeField] private KeyboardInputSmoothingParameters _smootingParameters;

        // I'm not sure about how this should work, actually.
        // I guess if the user changes the input method, we want the input to keep working,
        // which means that the individual input view must also be adaptive,
        // which means they have to handle all possible input sources,
        // which means this factory kind of server little purpose.
        // Sure, it handles their initialization, encapsulates their dependencies, but we could simply
        // take a struct with these adaptive input views, initialized by the user.
        private CarKeyboardInputView _keyboardInputView;
        private CameraKeyboardInputView _cameraInputView;

        void Awake()
        {
            var carControls = new CarControls();
            _cameraInputView = new CameraKeyboardInputView(carControls.Player);
            _keyboardInputView = new CarKeyboardInputView(_smootingParameters, carControls.Player);
        }

        public ICameraInputView CreateCameraInputView(int playerIndex, Gameplay.CarProperties carProperties)
        {
            assert(playerIndex == 0, "Multiple player keyboard input unimplemented.");
            return _cameraInputView;
        }

        public ICarInputView CreateCarInputView(int playerIndex, Gameplay.CarProperties carProperties)
        {
            assert(playerIndex == 0, "Multiple player keyboard input unimplemented.");
            _keyboardInputView.CarProperties = carProperties;
            return _keyboardInputView;
        }
    }
}
