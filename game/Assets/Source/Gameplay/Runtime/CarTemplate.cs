using System.Runtime.CompilerServices;
using UnityEngine;

namespace Race.Gameplay
{
    /// <summary>
    /// Defines some data used only on object initialization,
    /// so does not have to be mutated at runtime.
    /// </summary>
    [CreateAssetMenu(
        fileName = "New " + nameof(CarTemplate),
        menuName = nameof(CarTemplate),
        // Second grouping.
        order = 51)]
    public class CarTemplate : ScriptableObject
    {
        /// <summary>
        /// The actual spec will be deduced from this based on the stats.
        /// See <c>SceneTransition</c>.
        /// </summary>
        public CarSpecInfo baseSpec;

        /// <summary>
        /// Used to initialize the colliders.
        /// </summary>
        public CarColliderSetupHelper.CenterOfMassAdjustmentParameters centerOfMassAdjustmentParameters;
    }
}