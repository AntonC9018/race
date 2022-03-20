using UnityEngine;

namespace Race.Gameplay
{
    // I guess we'll have to copy these and then delete this component?? is there a better way?
    // I guess we could store this component like this directly in the data model.
    // Then we'd still have access to the template that the car was derived from,
    // even though I doubt that's any useful.
    // But that's better than doing copying?
    // I guess I'm going to go for that for now.

    // TODO: better name for this

    [System.Serializable]
    public class CarInfoComponent : MonoBehaviour
    {
        public CarTemplate template;
        public CarColliderParts colliderParts;
        public CarVisualParts visualParts;
        public float elevationSuchThatWheelsAreLevelWithTheGround;
    }
}