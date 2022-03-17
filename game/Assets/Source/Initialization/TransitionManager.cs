using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static EngineCommon.Assertions;

namespace Race.SceneTransition
{
    public class TransitionManager : MonoBehaviour
    {
        // These are not scenes, but gameobjects.
        // They act like scenes, but I'm using prefabs instead of actual scenes,
        // because scenes have no benefits over prefabs as far as I can tell.
        [SerializeField] private AssetReferenceGameObject _garageScenePrefab;
        [SerializeField] private AssetReferenceGameObject _gameplayScenePrefab;
        [SerializeField] private AssetReferenceGameObject _garageToGameplayTransitionScenePrefab;

        private Transform _garageTransform;
        private Transform _gameplayTransform;
        private Transform _garageToGameplayTransitionTransform;

        
        /*
            In garage, button (GO! sort of button) ->
            Hide garage (I guess disable the root?), put on the loading screen ->
            
            The next operations can be done in any order:
            - Get the data needed to initialize the gameplay scene (user data model, car data model),
              extract only the relevant bits.
            - Asynchronously load the needed prefabs.
            - Create empty game object for the gameplay scene root.


            *This step has to be done in an assembly that knows of both the garage and the gameplay*. 

            Once the car prefabs have been loaded (this step is implemented):
            - For players, configure the car spec based on the stats.
            - For all, set up the colliders and other stuff.


            Now, hide the loading screen, and let the gameplay scene take control.
            So, find the object tagged with "Initialization" in the instantiated prefab,
            and let it do the rest.
            I think it should be the one to set up the inputs views, because, actually,
            the inputs shouldn't be activated immediately???

            Could do something fancier when I understand the problem better.
        */

        private async Task InitializeGarage()
        {
            assert(_garageScenePrefab != null);
            assert(_garageTransform == null);

            var handle = _garageScenePrefab.LoadAssetAsync();

            // Tasks are not supported in WebGL, so might want to refactor this to use coroutines.
            // https://docs.unity3d.com/Packages/com.unity.addressables@1.9/manual/AddressableAssetsAsyncOperationHandle.html
            var prefab = await handle.Task;

            var garageGameObject = GameObject.Instantiate(prefab);
            var garageTransform = garageGameObject.transform;

            _garageTransform = garageTransform;

            var initializationTransform = FindInitializationTransform(_garageTransform);
            var initializationComponent = initializationTransform.GetComponent<IInitialization>();
            initializationComponent.Initialize();
        }

        // private Task TransitionFromGarageToGameplay(in GameplayInitializationInfo info)
        // {
        //     assert(_garageTransform != null);
        //     assert(_gameplayScenePrefab != null);
        //     assert(_garageToGameplayTransitionScenePrefab != null);

        //     _garageTransform.gameObject.SetActive(false);

        //     var gameplayScenePrefabTask = _gameplayScenePrefab.LoadAssetAsync();

        //     // var catalog = Addressables.LoadContentCatalogAsync();

        //     // _garageToGameplayTransitionScenePrefab.

        //     if (_garageToGameplayTransitionTransform != null)
        //     {
        //         var initializationTransform = FindInitializationTransform(_garageToGameplayTransitionTransform);
        //     }
        // }

        private Transform FindInitializationTransform(Transform root)
        {
            return root.Find("initialization");
        }
    }

    public interface IInitialization
    {
        void Initialize();
    }
}