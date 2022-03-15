using UnityEngine;
using Race.Gameplay;
using Race.Garage;
using System;
using static EngineCommon.Assertions;

namespace Race.SceneTransition
{
    public struct PlayerInfo
    {
        public int carIndex;
        public Garage.CarDataModel carDataModel;
        public UserDataModel userDataModel;
        public IPlayerInputViewFactory inputViewFactory;
    }

    /// <summary>
    /// Creates and initializes input views.
    /// </summary>
    public interface IPlayerInputViewFactory
    {
        ICarInputView CreateCarInputView(int playerIndex, in PlayerInfo playerInfo, Gameplay.CarProperties carProperties);
        ICameraInputView CreateCameraInputView(int playerIndex, in PlayerInfo playerInfo, Gameplay.CarProperties carProperties);
    }

    public class KeyboardPlayerInputViewFactory : MonoBehaviour, IPlayerInputViewFactory
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

        public ICameraInputView CreateCameraInputView(int playerIndex, in PlayerInfo playerInfo, Gameplay.CarProperties carProperties)
        {
            assert(playerIndex == 0, "Multiple player keyboard input unimplemented.");
            return _cameraInputView;
        }

        public ICarInputView CreateCarInputView(int playerIndex, in PlayerInfo playerInfo, Gameplay.CarProperties carProperties)
        {
            assert(playerIndex == 0, "Multiple player keyboard input unimplemented.");
            _keyboardInputView.CarProperties = carProperties;
            return _keyboardInputView;
        }
    }
   
    public struct BotInfo
    {
        public int carIndex;
        public ICarInputView inputView;
    }

    public ref struct GameplayInitializationInfo
    {
        public Span<PlayerInfo> playerInfos;
        public Span<BotInfo> botInfos;
    }

    public struct StatsConversionRates
    {
        // for now keep it const
        public const float c_torqueFactor = 1.0f;
        public readonly float torqueFactor => c_torqueFactor;
    }

    public class Transition : MonoBehaviour
    {
        [SerializeField] private GameObject _cameraControlPrefab;

        public static void AdjustMotorCharacteristicsToStats(in CarStats currentStats, in StatsConversionRates rates, ref Race.Gameplay.CarSpecInfo inoutSpecInfo)
        {
            ref var engine = ref inoutSpecInfo.engine;
            
            float motorRPMAtOldTorque;
            {
                float a = currentStats.accelerationModifier;
                float torqueBaseline = engine.maxTorque;
                float newTorque = a * rates.torqueFactor + torqueBaseline;

                // At optimalRPM the engine gave T torque.
                // Now it will give T' torque at that same point.
                // We shift it by dN to get the new desired RPM, such that at the old RPM it stays at T.  
                float previousTorque = torqueBaseline;
                // How much of the new torque is enough to get the previous torque.
                float neededEfficiencyForOldTorque = previousTorque / newTorque;

                motorRPMAtOldTorque = CarDataModelHelper.GetLowEngineRPMAtEngineEfficiency(neededEfficiencyForOldTorque, engine);

                engine.maxTorque = newTorque;
            }
            
            foreach (ref var g in inoutSpecInfo.transmission.gearRatios.AsSpan())
                g = engine.optimalRPM / (g * motorRPMAtOldTorque);
        }

        // TODO:
        public GameObject SpawnCar(Transform parent, int carIndex)
        {
            return null;
        }

        public void ApplyColor(Color color, Gameplay.CarProperties properties)
        {
            properties.VisualParts.body.GetComponent<MeshRenderer>().material.color = color;
        }

        public void InitializeScene(Transform root, in GameplayInitializationInfo initInfo)
        {
            // assert(initInfo.playerInfos.Length == 1, "For now only one player is allowed");

            var playerInfos = initInfo.playerInfos;

            for (int playerIndex = 0; playerIndex < playerInfos.Length; playerIndex++)
            {
                ref var playerInfo = ref playerInfos[playerIndex];
                var car = SpawnCar(root, playerInfo.carIndex);
                
                var carProperties = car.GetComponent<Gameplay.CarProperties>();
                assert(carProperties != null, "The car prefab must contain a `CarProperties` component");

                var dataModel = carProperties.DataModel;

                // Properties
                {
                    {
                        ref readonly var playerStats = ref playerInfo.carDataModel.statsInfo.currentStats;
                        var conversionRates = new StatsConversionRates();
                        AdjustMotorCharacteristicsToStats(playerStats, conversionRates, ref dataModel._spec);
                    }
                    {
                        ApplyColor(playerInfo.carDataModel.mainColor, carProperties);
                    }
                }

                // Logic
                {
                    // TODO: remove the factory, just pass adaptive input views
                    var factory = playerInfo.inputViewFactory;
                    
                    {
                        var cameraInput = factory.CreateCameraInputView(playerIndex, playerInfo, carProperties);
                        var cameraControlGameObject = GameObject.Instantiate(_cameraControlPrefab);
                        var cameraControl = cameraControlGameObject.GetComponent<CameraControl>();
                        cameraControl.Initialize(car.transform, cameraInput);
                    }
                    {
                        var carInput = factory.CreateCarInputView(playerIndex, playerInfo, carProperties);
                        var carController = car.GetComponent<CarController>();
                        carController.Initialize(carProperties, carInput);
                    }
                }
            }
        }
    }
}
