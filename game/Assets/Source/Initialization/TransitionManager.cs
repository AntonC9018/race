using System.Collections.Generic;
using System.Threading.Tasks;
using Race.Garage;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
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

        void Start()
        {
            var task = InitializeGarage();
            task.ContinueWith((t) => Debug.Log("Done loading garage"));
        }

        private async Task InitializeGarage()
        {
            assert(_garageScenePrefab != null);
            assert(_garageTransform == null);

        
            var handle = _garageScenePrefab.LoadAssetAsync();

            // Tasks are not supported in WebGL, so might want to refactor this to use coroutines.
            // https://docs.unity3d.com/Packages/com.unity.addressables@1.9/manual/AddressableAssetsAsyncOperationHandle.html
            var garagePrefab = await handle.Task;
            var garageGameObject = GameObject.Instantiate(garagePrefab);
            var garageTransform = garageGameObject.transform;

            _garageTransform = garageTransform;

            var initializationTransform = FindInitializationTransform(_garageTransform);
            var initializationComponent = initializationTransform.GetComponent<IGarageInitialize>();
            await initializationComponent.Initialize();
        }

        private Task TransitionFromGarageToGameplay(in GameplayInitializationInfo info)
        {
            assert(_garageTransform != null);
            assert(_gameplayScenePrefab != null);
            assert(_garageToGameplayTransitionScenePrefab != null);

            _garageTransform.gameObject.SetActive(false);

            var gameplayScenePrefabTask = _gameplayScenePrefab.LoadAssetAsync();

            static async Task<GameObject> GetCarPrefab(int index, AsyncOperationHandle<IList<IResourceLocation>> locationsHandle)
            {
                var locations = await locationsHandle.Task;
                var correctLocation = locations[index];
                var prefabHandle = Addressables.LoadAssetAsync<GameObject>(correctLocation);
                return await prefabHandle.Task;
            }

            // This is stupid and I hate it ...
            // Addressables' API is terrible IMO. I'd do a custom thing and be happy.
            // Their code is complicated and unreadable too.
            var gameplayCarsLocationsHandle = Addressables.LoadResourceLocationsAsync("gameplay");

            /*
                I'd do something like the following:
                - Refer to anything by the group prefix, and their index in that group.
                - Each group would have a metadata section, where the names can be mapped to indices if needed,
                  and any other metadata would be stored.
                  I think you'd still have to give some sort of "description" to the things that a bundle
                  or a server contains.
                  That section should be able to contain anything, perhaps even like smaller car models,
                  so that the player could browse the cars without loading in whole detailed meshes.
                - When loading the things, you must first collect the groups that you will be downloading from,
                  which I guess should be known statically, or retrieved dynamically. If it's known statically,
                  you can just get the needed group names synchronously.
                  Then you'd load all things from the groups in bulk again.
                  Once they have been loaded, you can just index directly into them.

                So I guess I'd like a lower-level abstraction better than Addressables.
                The lazy-loading of every object individually really gets to me.
                I guess I just don't think in objects, but in data.

                Like I would rather group the objects I need to query by their group name manually, than
                downloading each one individually and handing all of that off to some magical system.
                I would be much more efficient too.

                I might be missing something at this point too tho.
            */

            Task[] playerCarsTasks;
            {
                var playerCount = info.playerInfos.Length;
                assert(playerCount == 1);

                playerCarsTasks = new Task[playerCount];
                for (int i = 0; i < playerCarsTasks.Length; i++)
                {
                    // This is again just stupid, because currently WE KNOW the cars are stored in the same bundle.
                    // So they will always resolve instantly after the locations have been loaded.
                    // We could've just iterated them manually at that point.
                    var task = GetPlayerCar(info.playerInfos[i], gameplayCarsLocationsHandle);
                    playerCarsTasks[i] = task;

                    static async Task<GameObject> GetPlayerCar(PlayerInfo info, AsyncOperationHandle<IList<IResourceLocation>> locationsHandle)
                    {
                        var prefab = await GetCarPrefab(info.carIndex, locationsHandle);
                        // TODO: all that other stuff from the Transition script.
                        // Also, I think we have to delegate this to the main thread anyway.
                        var car = GameObject.Instantiate(prefab);
                        return car;
                    }
                }
                // TODO: approximately the same for bots
            }

            //?
            // if (_garageToGameplayTransitionTransform != null)
            // {
            //     var initializationTransform = FindInitializationTransform(_garageToGameplayTransitionTransform);
            // }

            return Task.WhenAll(playerCarsTasks);
        }

        private Transform FindInitializationTransform(Transform root)
        {
            return root.Find("initialization");
        }
    }
}