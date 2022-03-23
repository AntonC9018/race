using System;
using Race.Gameplay.Generated;
using UnityEngine;

using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public enum ParticipantUpdateResult
    {
        Ok = 0,
        Invalid = 1,
        ReachedEnd = 2,
        EliminatedBit = 16,
        UpsideDown = EliminatedBit | 1,
        OutsideTrack = EliminatedBit | 2,
    }

    /// <summary>
    /// Dispatches events on the RaceProperties when participants' points get updated.
    /// </summary>
    public class RaceParticipantsUpdateManager : MonoBehaviour, IInitialize<RaceProperties>
    {
        private RaceProperties _raceProperties;
        private ParticipantUpdateResult[] _updateResultsTemporary;
        private ref Participants Participants => ref _raceProperties.DataModel.participants;

        public void Initialize(RaceProperties properties)
        {
            _raceProperties = properties;

            ref var participants = ref properties.DataModel.participants;
            Array.Resize(ref _updateResultsTemporary, participants.Count);
        }

        public void Update()
        {
            for (int i = 0; i < Participants.Count; i++)
            {
                ref var participant = ref Participants.driver[i];

                bool isRespawning = participant.carProperties.DataModel.IsDrivingDisabled();
                if (isRespawning)
                    continue;

                _updateResultsTemporary[i] = UpdatePosition(i, participant.transform);
            }
            _raceProperties.TriggerParticipantUpdated(_updateResultsTemporary);
        }

        private static bool IsUpsideDown(IStaticTrack track, RoadPoint point, Transform participantTransform)
        {
            Vector3 roadNormal = track.GetRoadNormal(point);
            float upAmount = Vector3.Dot(participantTransform.up, roadNormal);
            const float requiredUpAmount = 0.2f;
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

            // Probably should take into account the length of the car somehow?
            // So the track should have a function for back and a function for front?
            if (Mathf.Approximately(track.GetTotalProgress(point), 1.0f))
                return ParticipantUpdateResult.ReachedEnd;

            return ParticipantUpdateResult.Ok;
        }
    }
}