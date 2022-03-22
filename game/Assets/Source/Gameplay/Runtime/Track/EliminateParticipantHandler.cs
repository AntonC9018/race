using System;
using UnityEngine;

namespace Race.Gameplay
{
    public class EliminateParticipantHandler : MonoBehaviour, IInitialize<RaceProperties>
    {
        // For now, just put it here
        /// <summary>
        /// The delay is used for respawning participants when they are eliminated.
        /// </summary>
        [SerializeField] private SameDurationDelay _delay;

        public void Initialize(RaceProperties value)
        {
            value.OnParticipantUpdated.AddListener(OnParticipantUpdated);
        }

        public void OnParticipantUpdated(ParticipantUpdatedEventInfo info)
        {
            if (info.result == ParticipantUpdateResult.Ok)
            {
                ref var t = ref info.DataModel.participants.track;
                
                ref var checkpoint = ref t.checkpoints[info.index];
                var point = t.points[info.index];

                if (point > checkpoint)
                        checkpoint = point;
            }

            else if ((info.result & ParticipantUpdateResult.EliminatedBit) != 0)
            {
                CarDataModelHelper.StopCar(info.Driver.transform, info.Driver.carProperties);
                _delay.Delay(GetRespawnParticipantAction(info.index, info.raceProperties));
            }
        }

        private static Action GetRespawnParticipantAction(int index, RaceProperties properties)
        {
            return () =>
            {
                var model = properties.DataModel;
                ref var participants = ref model.participants;
                ref var driver = ref participants.driver.infos[index];
                ref var t = ref participants.track;
                ref var point = ref t.points[index];

                point = t.checkpoints[index];

                {
                    var track = model.Track;
                    var position = track.GetRoadMiddlePosition(point);
                    var rotation = track.GetRegularRotation(point);
                    CarDataModelHelper.ResetPositionAndRotationOfBackOfCar(
                        driver.transform, driver.carProperties.DataModel, position, rotation);
                }

                Debug.Log("returing car to checkpoint");
                CarDataModelHelper.RestartDisabledDriving(driver.carProperties);
            };
        }
    }
}