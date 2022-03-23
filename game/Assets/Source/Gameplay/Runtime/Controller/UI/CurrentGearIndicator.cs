using System;
using TMPro;
using UnityEngine;

namespace Race.Gameplay
{
    public class CurrentGearIndicator : MonoBehaviour, IInitialize<CarProperties>
    {
        [SerializeField] private TMP_Text _text;
        public void Initialize(CarProperties carProperties)
        {
            carProperties.OnGearShifted.AddListener(OnGearShifted);
            carProperties.OnDrivingToggled.AddListener(OnDrivingToggled);
            OnGearShifted(carProperties);
        }

        private void OnGearShifted(CarProperties carProperties)
        {
            var currentGear = carProperties.DataModel.DrivingState.gearIndex;
            _text.text = currentGear == 0 ? "R" : currentGear.ToString();
        }

        private void OnDrivingToggled(CarProperties carProperties)
        {
            if (carProperties.DataModel.IsDrivingEnabled())
                OnGearShifted(carProperties);
        }
    }
}
