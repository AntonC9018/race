using Race.Gameplay.Generated;
using UnityEngine;

using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public enum ParticipantUpdateResult
    {
        Ok = 0,
        Invalid = 1,
        EliminatedBit = 16,
        UpsideDown = EliminatedBit | 1,
        OutsideTrack = EliminatedBit | 2,
    }

    /// <summary>
    /// Dispatches events on the RaceProperties when participants' points get updated.
    /// </summary>
    public class RaceManager : MonoBehaviour, IInitialize<RaceProperties>
    {
        private RaceProperties _raceProperties;
        private ref Participants Participants => ref _raceProperties.DataModel.participants;

        public void Initialize(RaceProperties properties)
        {
            _raceProperties = properties;

            ref var participants = ref properties.DataModel.participants.track;
            var dataModel = properties.DataModel;
        }

        public void Update()
        {
            ref var participants = ref _raceProperties.DataModel.participants.driver.infos;

            for (int i = 0; i < Participants.Count; i++)
            {
                ref var participant = ref Participants.driver[i];

                bool isRespawning = participant.carProperties.DataModel.DrivingState.flags.Has(CarDrivingState.Flags.Disabled);
                if (isRespawning)
                    continue;

                var updateResult = UpdatePosition(i, participant.transform);

                // Swallow the invalids. They mean the listeners did not disable the participant.
                if (updateResult == ParticipantUpdateResult.Invalid)
                    continue;

                // An idea is to do a readonly array, and dispatch the whole thing at once.
                _raceProperties.TriggerParticipantUpdated(i, updateResult);
            }
        }

        private static bool IsUpsideDown(IStaticTrack track, RoadPoint point, Transform participantTransform)
        {
            Vector3 roadNormal = track.GetRoadNormal(point);
            float upAmount = Vector3.Dot(participantTransform.up, roadNormal);
            const float requiredUpAmount = -0.2f;
            return upAmount < requiredUpAmount;
        }
        
        private ParticipantUpdateResult UpdatePosition(int participantIndex, Transform participantTransform)
        {
            ref var point = ref Participants.track.points[participantIndex];
            var track = _raceProperties.DataModel.Track;

            if (point.IsOutsideTrack)
                return ParticipantUpdateResult.Invalid;

            point = track.UpdateRoadPoint(point, participantTransform.position);
            if (point.IsOutsideTrack)
                return ParticipantUpdateResult.OutsideTrack;

            if (IsUpsideDown(track, point, participantTransform))
                return ParticipantUpdateResult.UpsideDown;

            return ParticipantUpdateResult.Ok;
        }
    }
}