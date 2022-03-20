using System;
using System.Collections.Generic;
using Kari.Plugins.Terminal;
using Race.Gameplay.Generated;
using UnityEngine;

using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public interface IPosition
    {
        Vector3 Position { get; }
    }

    public struct ParticipantInfo
    {
        public readonly Transform transform;
        public readonly CarProperties carProperties;
        public float deathTime;

        public ParticipantInfo(Transform transform, CarProperties carProperties)
        {
            this.transform = transform;
            this.carProperties = carProperties;
            this.deathTime = 0;
        }
    }

    public class TrackManager : MonoBehaviour
    {
        private LowLevelTrackManager _underlyingManager;
        private ParticipantInfo[] _participants;

        public void Initialize(Transform playerTransform, CarProperties playerCarProperties)
        {
            _participants = new[] { new ParticipantInfo(playerTransform, playerCarProperties), };

            var track = MakeTrack();
            IStaticTrack MakeTrack()
            {
                var transform = this.transform;
                var center = transform.position;
                var scale = transform.localRotation * transform.localScale;
                var length = scale.z;
                var width = scale.x;

                // hack: does not handle slopes
                var halfLengthVector = new Vector3(0, 0, length / 2);

                var startPoint = center - halfLengthVector;
                var endPoint = center + halfLengthVector;

                const float visualVSFunctionRoadFactor = 1.2f;
                return new StraightTrack(startPoint, endPoint, width * visualVSFunctionRoadFactor);
            }

            _underlyingManager = new LowLevelTrackManager();
            _underlyingManager.Reset(track, 1);

            for (int i = 0; i < _participants.Length; i++)
                ActivateParticipant(i);
        }

        private void ActivateParticipant(int i)
        {
            var (position, r) = _underlyingManager.GetPositionAndRotation(participantIndex: i);
            ref readonly var participant = ref _participants[i];
            CarDataModelHelper.ResetPositionAndRotationOfBackOfCar(participant.transform, participant.carProperties, position, r);
        }

        void FixedUpdate()
        {
            for (int i = 0; i < _participants.Length; i++)
            {
                ref var participant = ref _participants[i];

                bool isRespawning = participant.carProperties.DataModel.DrivingState.flags.Has(CarDrivingState.Flags.Disabled);
                if (isRespawning)
                {
                    const float respawnTimeout = 1.0f;

                    if (participant.deathTime + respawnTimeout < Time.time)
                    {
                        _underlyingManager.ReturnToCheckpoint(i);
                        ActivateParticipant(i);
                        CarDataModelHelper.RestartDisabledDriving(participant.carProperties);
                    }
                }
                // Update
                else
                {
                    var updateResult = _underlyingManager.UpdatePosition(i, participant.transform);
                    if ((updateResult & LowLevelTrackManager.UpdateResult.EliminatedBit) != 0)
                    {
                        Debug.Log(updateResult);
                        CarDataModelHelper.StopCar(participant.transform, participant.carProperties);
                        participant.deathTime = Time.time;
                    }
                }
            }
        }


        [Command(Name = "flip", Help = "Flips a car upside down.")]
        public static void FlipOver(
            [Argument("Which participant to flip over")] int participantIndex = 0)
        {
            var trackManager = GameObject.FindObjectOfType<TrackManager>();
            if (trackManager == null)
            {
                Debug.LogError("The track manager could not be found");
                return;
            }

            if (participantIndex < 0 || participantIndex >= trackManager._participants.Length)
            {
                Debug.Log($"The participant index {participantIndex} was outside the bound of the participant array");
                return;
            }

            var t = trackManager._participants[participantIndex].transform;
            t.rotation = Quaternion.AngleAxis(180, Vector3.forward);
            t.position += Vector3.up * 3;
        }
    }

    public class LowLevelTrackManager
    {
        internal RoadPoint[] _participantPositions;
        internal RoadPoint[] _participantCheckpoints;
        internal IStaticTrack _track;

        public void Reset(IStaticTrack track, int count)
        {
            Array.Resize(ref _participantPositions, count);
            Array.Resize(ref _participantCheckpoints, count);
            _track = track;

            {
                var p = RoadPoint.CreateStartOf(track.StartingSegment);
                assert(p.IsInsideTrack);

                for (int i = 0; i < count; i++)
                {
                    _participantCheckpoints[i] = p;
                    _participantPositions[i] = p;
                }
            }
        }

        public Vector3 GetCheckpointPosition(int participantIndex)
        {
            return _track.GetRoadMiddlePosition(_participantCheckpoints[participantIndex]);
        }

        private bool IsUpsideDown(int participantIndex, Transform participantTransform)
        {
            var location = _participantPositions[participantIndex];
            Vector3 roadNormal = _track.GetRoadNormal(location);
            float upAmount = Vector3.Dot(participantTransform.up, roadNormal);
            const float requiredUpAmount = -0.2f;
            return upAmount < requiredUpAmount;
        }


        public enum UpdateResult
        {
            Ok = 0,
            EliminatedBit = 16,
            UpsideDown = EliminatedBit | 1,
            OutsideTrack = EliminatedBit | 2,
        }

        public UpdateResult UpdatePosition(int participantIndex, Transform participantTransform)
        {
            ref var location = ref _participantPositions[participantIndex];
            if (location.IsOutsideTrack)
                return UpdateResult.OutsideTrack;

            location = _track.UpdateRoadPoint(location, participantTransform.position);
            if (location.IsOutsideTrack)
                return UpdateResult.OutsideTrack;

            bool isUpsideDown = IsUpsideDown(participantIndex, participantTransform);
            if (!isUpsideDown)
            {
                // TODO: maybe snap
                _participantCheckpoints[participantIndex] = location;
                return UpdateResult.Ok;
            }
                
            return UpdateResult.UpsideDown;
        }

        public (Vector3 position, Quaternion rotation) GetPositionAndRotation(int participantIndex)
        {
            var location = _participantPositions[participantIndex];
            var position = _track.GetRoadMiddlePosition(location);
            var rotation = _track.GetRegularRotation(location);
            return (position, rotation);
        }

        public void ReturnToCheckpoint(int participantIndex)
        {
            _participantPositions[participantIndex] = _participantCheckpoints[participantIndex];
        }
    }
}