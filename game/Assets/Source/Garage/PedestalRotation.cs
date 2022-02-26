using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Race
{
    public class PedestalRotation : MonoBehaviour
    {
        [SerializeField] private Transform _thingToRotate;
        [SerializeField] private float _rotationIncrement;

        void Start()
        {
            
        }

        void Update()
        {
            const int left = 0;
            const int right = 1;

            var rotationAxis = Vector3.up;

            if (Input.GetMouseButton(left))
            {
                _thingToRotate.Rotate(rotationAxis, _rotationIncrement);
            }
            else if (Input.GetMouseButton(right))
            {
                _thingToRotate.Rotate(rotationAxis, -_rotationIncrement);
            }
        }
    }
}
