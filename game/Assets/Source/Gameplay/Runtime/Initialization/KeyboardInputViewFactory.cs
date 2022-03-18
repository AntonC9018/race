using UnityEngine;
using Race.Gameplay;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public interface IEnableDisableInput
    {
        void EnableAllInput();
        void DisableAllInput();
    }
    
    /// <summary>
    /// Creates and initializes input views.
    /// </summary>
    public interface IInputViewManager
    {
        ICarInputView CreateCarInputView(int participantIndex, Gameplay.CarProperties carProperties);
        ICameraInputView CreateCameraInputView(int participantIndex, Gameplay.CarProperties carProperties);
    }

    public class KeyboardInputViewFactory : MonoBehaviour, IInputViewManager, IEnableDisableInput
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
        private CarControls _carControls;

        void Awake()
        {
            var carControls = new CarControls();
            _carControls = carControls;
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
            _keyboardInputView.ResetTo(carProperties);
            return _keyboardInputView;
        }

        public void EnableAllInput()
        {
            _carControls.Enable();
        }

        public void DisableAllInput()
        {
            _carControls.Disable();
        }
    }
}
