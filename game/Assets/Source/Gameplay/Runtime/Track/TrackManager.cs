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

    public static class TrackHelper
    {
        public static (IStaticTrack track, float width) CreateFromQuad(Transform quad)
        {
            var transform = quad.transform;
            var center = transform.position;
            var scale = transform.localRotation * transform.localScale;
            var length = scale.z;
            var width = scale.x;

            // hack: does not handle slopes
            var halfLengthVector = new Vector3(0, 0, length / 2);

            var startPoint = center - halfLengthVector;
            var endPoint = center + halfLengthVector;

            const float visualVSFunctionRoadFactor = 1.2f;
            return (new StraightTrack(startPoint, endPoint, width * visualVSFunctionRoadFactor), width);
        }
    }

    public class TrackManager
    {
        // Currently assumes that the road is a 1x1 quad.
        private LowLevelTrackManager _underlyingManager;
        private DriverInfo[] _participants;
        private float[] _participantDeathTimes;

        // TODO: Must not place participants immediately! That's just confusing. Refactor!
        public void Initialize(DriverInfo[] participants, IStaticTrack track, ICarPlacementStrategy placementStrategy)
        {
            assert(participants is not null);
            assert(track is not null);
            assert(placementStrategy is not null);
            
            _participants = participants;
            _participantDeathTimes = new float[participants.Length];

            _underlyingManager = new LowLevelTrackManager();
            _underlyingManager.Reset(track, participants.Length);

            for (int i = 0; i < participants.Length; i++)
            {
                var (pos, rot) = placementStrategy.PlaceCar(i);
                ref readonly var participant = ref participants[i];
                var t = participant.transform;
                CarDataModelHelper.ResetPositionAndRotationOfBackOfCar(
                    t, participant.carProperties, pos, rot);
                _underlyingManager.UpdatePosition(i, t);
            }
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

                    if (_participantDeathTimes[i] + respawnTimeout < Time.time)
                    {
                        _underlyingManager.ReturnToCheckpoint(i);
                        {
                            var (position, r) = _underlyingManager.GetPositionAndRotation(participantIndex: i);
                            CarDataModelHelper.ResetPositionAndRotationOfBackOfCar(
                                participant.transform, participant.carProperties, position, r);
                        }
                        CarDataModelHelper.RestartDisabledDriving(participant.carProperties);
                    }
                }
                // Update
                else
                {
                    var updateResult = _underlyingManager.UpdatePosition(i, participant.transform);
                    if ((updateResult & LowLevelTrackManager.UpdateResult.EliminatedBit) != 0)
                    {
                        CarDataModelHelper.StopCar(participant.transform, participant.carProperties);
                        _participantDeathTimes[i] = Time.time;
                    }
                }
            }
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