using System.Runtime.CompilerServices;
using UnityEngine;

namespace Race.Gameplay
{
    /// <summary>
    /// Allows defining these properties in the inspector and sharing them between cars.
    /// </summary>
    [CreateAssetMenu(
        fileName = "New " + nameof(CarSpecInfoObject),
        menuName = nameof(CarSpecInfoObject),
        // Second grouping.
        order = 51)]
    public class CarSpecInfoObject : ScriptableObject
    {
        public CarSpecInfo info;
    }
}