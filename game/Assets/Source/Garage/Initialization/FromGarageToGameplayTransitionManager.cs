using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kari.Plugins.Terminal;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.UI;
using static EngineCommon.Assertions;

namespace Race.Garage
{
    [System.Serializable]
    public struct PlayerInfo
    {
        public int carIndex;
        public Garage.CarDataModel carDataModel;
        public UserDataModel userDataModel;
    }
   
    [System.Serializable]
    public struct BotInfo
    {
        public int carIndex;
    }

    public struct GarageToGameplayTransitionInfo
    {
        // We don't pass arrays because we don't need to keep them in memory.
        // TODO: Could pass IEnumerable's?
        public PlayerInfo[] playerInfos;
        public BotInfo[] botInfos;
        public IResourceLocation mapResouceLocation;
    }

    public readonly struct GarageInitializationInfo
    {
        public readonly ITransitionFromGarageToGameplay fromGarageToGameplay;

        public GarageInitializationInfo(ITransitionFromGarageToGameplay fromGarageToGameplay)
        {
            this.fromGarageToGameplay = fromGarageToGameplay;
        }
    }

    public interface ITransitionFromGarageToGameplay
    {
        Task TransitionFromGarageToGameplay(GarageToGameplayTransitionInfo info);
    }

    public class FromGarageToGameplayTransitionManager : MonoBehaviour
    {
        private const string TracksLabel = "track";

        // TODO: The asset things need to be managed somewhere else, definitely not here.
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Dropdown _levelDropdown;

        private IList<IResourceLocation> _mapResouceLocations;

        private CarProperties _carProperties;
        private UserProperties _userProperties;
        private ITransitionFromGarageToGameplay _transitionHandler;
        private Task _transitionTask;

        public async Task Initialize(GarageCommonInitializationStuff commonStuff, GarageInitializationInfo handler)
        {
            assert(_button != null);
            assert(_levelDropdown != null);
            assert(handler.fromGarageToGameplay != null);
            assert(commonStuff.carProperties != null);
            assert(commonStuff.userProperties != null);

            _carProperties = commonStuff.carProperties;
            _userProperties = commonStuff.userProperties;
            _transitionHandler = handler.fromGarageToGameplay;
            
            _carProperties.OnCarSelected.AddListener(OnCarSelected);
            _button.onClick.AddListener(OnButtonClicked);

            var mapLocationsHandle = Addressables.LoadResourceLocationsAsync(TracksLabel, typeof(GameObject));
            var mapLocations = await mapLocationsHandle.Task;

            _mapResouceLocations = mapLocations;
            _levelDropdown.options = mapLocations
                .Select(loc => new TMP_Dropdown.OptionData(loc.PrimaryKey))
                .ToList();

            // We don't release the handle, because the locations are needed
            // for the whole lifetime of the application. (at least for now).
        }

        async void Update()
        {
            if (_transitionTask is not null)
            {
                await _transitionTask;
                _transitionTask = null;
            }
        }

        private void OnCarSelected(CarSelectionChangedEventInfo info)
        {
            _button.interactable = info.IsAnyCarSelected;
        }

        private void OnButtonClicked()
        {
            assert(_carProperties.IsAnyCarSelected, "??");
            assert(_transitionHandler is not null);
            
            if (_transitionTask is not null)
                return;
            
            _transitionTask = InitiateTransition();
        }

        private Task InitiateTransition()
        {
            var carProperties = _carProperties;
            var transitionInfo = new GarageToGameplayTransitionInfo
            {
                playerInfos = new[]
                {
                    new PlayerInfo
                    {
                        carIndex = carProperties.CurrentCarIndex,
                        carDataModel = carProperties.CurrentCarInfo.dataModel,
                        userDataModel = _userProperties.DataModel,
                    }
                },

                botInfos = new[]
                {
                    new BotInfo
                    {
                        carIndex = 0,
                    }
                },

                mapResouceLocation = _mapResouceLocations[_levelDropdown.value],
            };

            return _transitionHandler.TransitionFromGarageToGameplay(transitionInfo);
        }

        [Command("go", "Transition from garage to gameplay")]
        public static void InitiateTransitionCommand()
        {
            var transition = Transform.FindObjectOfType<FromGarageToGameplayTransitionManager>();
            if (transition == null)
            {
                Debug.LogError("Could not do find transition");
                return;
            }
            
            transition.OnButtonClicked();
            Debug.Log("Initiated the transition");
        }
    }
}