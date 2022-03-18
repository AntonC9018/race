using System.Threading.Tasks;
using Kari.Plugins.Terminal;
using UnityEngine;
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

    public class TransitionManager : MonoBehaviour
    {
        private GarageFunctionalInfo _garageInfo;
        private GarageInitializationInfo _transitionInfo;

        public void Initalize(GarageFunctionalInfo garageInfo, GarageInitializationInfo handler)
        {
            _garageInfo = garageInfo;
            _transitionInfo = handler;
            assert(_transitionInfo.fromGarageToGameplay != null);
        }

        public void OnButtonClicked()
        {
            if (_transitionInfo.fromGarageToGameplay == null)
                return;

            var carProperties = _garageInfo.carProperties;
            var transitionInfo = new GarageToGameplayTransitionInfo
            {
                playerInfos = new[]
                {
                    new PlayerInfo
                    {
                        carIndex = carProperties.CurrentCarIndex,
                        carDataModel = carProperties.CurrentCarInfo.dataModel,
                        userDataModel = _garageInfo.userProperties.DataModel,
                    }
                },

                botInfos = new[]
                {
                    new BotInfo
                    {
                        carIndex = 0,
                    }
                },
            };
            
            _transitionInfo.fromGarageToGameplay.TransitionFromGarageToGameplay(transitionInfo);
        }

        [Command("go", "Transition from garage to gameplay")]
        public static void InitiateTransitionCommand()
        {
            var transition = Transform.FindObjectOfType<TransitionManager>();
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