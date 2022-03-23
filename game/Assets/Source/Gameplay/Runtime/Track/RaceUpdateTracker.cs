using System;
using UnityEngine;

using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    // There should also be an event for this.
    // A single thing should handle whatever happens to the race as a result,
    // and while all the other things could update ui and stuff.
    // But I don't know for certain yet.
    public interface IOnRaceEnded
    {
        void OnRaceEnded(int winnerIndex, RaceProperties raceProperties);
    }

    // It's not clear yet how much decoupling I want here.
    // I guess this works OK for now.
    public class RaceUpdateTracker : MonoBehaviour
    {
        /// <summary>
        /// The delay is used for respawning participants when they are eliminated.
        /// </summary>
        private IDelay _delay;
        private IOnRaceEnded _onRaceEnded;

        // For now, initialize this particular class manually.
        // But this can be decoupled with proper dependency injection.
        public void Initialize(RaceProperties value, IDelay delay, IOnRaceEnded onRaceEnded)
        {
            value.OnParticipantsUpdated.AddListener(OnParticipantsUpdated);
            _delay = delay;
            _onRaceEnded = onRaceEnded;
        }

        public void OnParticipantsUpdated(ParticipantUpdatedEventInfo info)
        {
            for (int i = 0; i < info.results.Count; i++)
            {
                var result = info.results[i];

                switch (result)
                {
                    case ParticipantUpdateResult.Ok:
                    {
                        ref var t = ref info.DataModel.participants.track;
                    
                        ref var checkpoint = ref t.checkpoints[i];
                        var point = t.points[i];

                        if (point > checkpoint)
                            checkpoint = point;
                        break;
                    }

                    case ParticipantUpdateResult.ReachedEnd:
                    {
                        // do something??
                        // It's not clear who should be responsible for this yet.
                        // I guess the RaceProperties should have a callback for OnRaceEnd().

                        // Could decouple this a bit more with another class that would handle these updates,
                        // but I guess for now let's just call the handler.
                        _onRaceEnded.OnRaceEnded(i, info.raceProperties);

                        return;
                    }

                    case ParticipantUpdateResult.Invalid:
                    {
                        // Since we always take control and take it back, this should never happen.
                        // (unless we delegate the ReachedEnd to someone else).
                        assert(false, "Invalid result happened?");
                        break;
                    }

                    // else if ((result & ParticipantUpdateResult.EliminatedBit) != 0)
                    case ParticipantUpdateResult.UpsideDown:
                    case ParticipantUpdateResult.OutsideTrack:
                    {
                        ref var driver = ref info.DataModel.participants.driver[i];
                        CarDataModelHelper.StopCar(driver.transform, driver.carProperties);
                        _delay.Delay(GetRespawnParticipantAction(i, info.raceProperties));

                        break;
                    }

                    default:
                    {
                        assert(false, "Must update this switch when more conditions are added");
                        break;
                    }
                }
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

                CarDataModelHelper.RestartDisabledDriving(driver.carProperties);
            };
        }
    }
}