using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Race.SceneTransition
{
    public class TransitionManager : MonoBehaviour
    {
        // These are not scenes, but gameobjects.
        // They act like scenes, but I'm using prefabs instead of actual scenes,
        // because scenes have no benefits over prefabs as far as I can tell.
        [SerializeField] private AssetReference _garageScenePrefab;
        // [SerializeField] private AssetReference _gameplayScenePrefab;
        [SerializeField] private AssetReference _addScenePrefab;

        
    }
}