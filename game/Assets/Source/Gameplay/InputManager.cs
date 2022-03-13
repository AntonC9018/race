using UnityEngine;

namespace Race.Gameplay
{
    public class InputManager : MonoBehaviour
    {
        public CarControls CarControls
        {
            get;
            private set;
        }

        void Awake()
        {
            CarControls = new CarControls();
        }
    }
}
