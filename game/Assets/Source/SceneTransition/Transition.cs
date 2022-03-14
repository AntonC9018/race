using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Race.Gameplay;

namespace Race.SceneTransition
{
    public struct PlayerInitializationInfo
    {
        public ICarInputView inputView;
        public CarEngineInfo engineInfo;
        public CarTransmissionInfo transmissionInfo;
    }

    public class Transition : MonoBehaviour
    {
        public bool Things(Race.Garage.CarDataModel dataModel, float wheelCircumference)
        {
            ref readonly var stats = ref dataModel.statsInfo.currentStats;
            
            float a = stats.accelerationModifier;
            
            // TODO: get somewhere?
            var reference = new CarEngineInfo();

            float torqueBaseline = reference.maxTorque;
            const float torqueFactor = 1.0f;
            float torque = a * torqueFactor + torqueBaseline;

            // Approximate gear ratios I found online.
            // float[] referenceGearRatios = new float[]
            // {
            //     3.136f,
            //     1.888f,
            //     1.330f,
            //     1,
            //     0.814f,
            // };

            // // speed at which the engine should reach peak efficiency in the last gear at torqueBaseline.
            // const float speedInLastGearBaseline = 80.0f;
            // const float speedInFirstGearBaseline = 8.0f;


            // At optimalRPM the engine gave T torque.
            // Now it will give T' torque at that same point.
            // We shift it by dN to get the new desired RPM, such that at the old RPM it stays at T.  
            // float engineEfficiency = CarDataModelHelper.GetEngineEfficiency(reference);
            
            // TODO: compute it somehow.
            float newTorque = torque;
            float previousTorque = torqueBaseline;
            // How much of the new torque is enough to get the previous torque.
            float neededEfficiencyForOldTorque = previousTorque / newTorque;

            float motorRPMAtOldTorque = CarDataModelHelper.GetLowEngineRPMAtEngineEfficiency(neededEfficiencyForOldTorque, reference);
            float deltaRPM = motorRPMAtOldTorque - reference.optimalRPM;

            // So after this we either adjust the max RPM and the optimal RPM,
            // or change the gear ratios.
            // I'm going to adjust the gear ratios.
            var transmission = new CarTransmissionInfo();
            float[] newGearRatios = transmission.gearRatios[..];

            // was:     wheelRPM * g = N
            // became:  wheelRPM * g' = N'
            // N' = oldRPM, g' = new gear
            // g' = N' / wheelRPM = f * N / wheelRPM = f * g
            for (int i = 0; i < newGearRatios.Length; i++)
                newGearRatios[i] *= neededEfficiencyForOldTorque;
            
            return true;
        }

        public void InitializeScene(Transform root, PlayerInitializationInfo playerInfo)
        {
            GameObject instantiatedPlayer = null;
            var carProperties = instantiatedPlayer.GetComponent<CarProperties>();
            var dataModel = carProperties.DataModel;

            var spec = dataModel._spec.info;
            spec.engine = playerInfo.engineInfo;
            spec.transmission = playerInfo.transmissionInfo;
        }
    }
}
