using System;
using UnityEngine;
using UnityEngine.Events;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    // 2
    public struct ParticipantsDriverInfo
    {
        // 3
        public DriverInfo[] infos;
        public int playerCount;
        public int botCount;

        public readonly Span<DriverInfo> Players => infos.AsSpan(0, playerCount);
        public readonly Span<DriverInfo> Bots => infos.AsSpan(playerCount, botCount);
        public readonly ref DriverInfo this[int index] => ref infos[index];
        public readonly int Count => infos.Length;
    }

    public struct ParticipantsTrackInfo
    {
        // 2, 3
        public RoadPoint[] points;
        public RoadPoint[] checkpoints;
    }

    public struct Participants
    {
        public ParticipantsDriverInfo driver;
        public ParticipantsTrackInfo track;

        public readonly int Count => driver.infos.Length;
    }

    public struct TrackRaceInfo
    {
        // 1
        public IStaticTrack track;
        public float visualWidth;
        public float actualWidth;
    }

    // TODO: this being a value type would be nice?
    public class RaceDataModel
    {
        public Participants participants;
        public Transform mapTransform;
        public Transform trackTransform;
        public TrackRaceInfo trackInfo;

        public IStaticTrack Track => trackInfo.track;
    }

    public static class RaceDataModelHelper
    {
        
        // NOTE:
        // I in the end decided to go with a method,
        // because this method is pretty much about the invariance of the data.
        // I was really tempted to go with an extension method here, but a member
        // method actually makes sense in this case. 
        public static void SetParticipants(RaceDataModel dataModel, ReadOnlySpan<DriverInfo> players, ReadOnlySpan<DriverInfo> bots)
        {
            int participantCount = players.Length + bots.Length;

            {
                ref var driver = ref dataModel.participants.driver;
                Array.Resize(ref driver.infos, participantCount);
                
                driver.playerCount = players.Length;
                players.CopyTo(driver.Players);
                
                driver.botCount = bots.Length;
                bots.CopyTo(driver.Bots);
            }

            {
                ref var track = ref dataModel.participants.track;
                Array.Resize(ref track.points, participantCount); 
                Array.Resize(ref track.checkpoints, participantCount); 
            }
        }

        public static void PlaceParticipants(RaceDataModel dataModel)
        {
            // For now, only position in a grid
            var placementData = CarPlacement.GetGridPlacementData(dataModel.participants.driver, dataModel.trackInfo);
            
            ref var participants = ref dataModel.participants;
            
            for (int i = 0; i < participants.Count; i++)
            {
                var (pos, rot) = CarPlacement.GetPositionAndRotation(placementData, i);

                ref readonly var driverInfo = ref participants.driver.infos[i];
                var t = driverInfo.transform;

                // Visual
                CarDataModelHelper.ResetPositionAndRotationOfBackOfCar(
                    t, driverInfo.carProperties, pos, rot);

                SynchronizeTrackLocationToWorldPositionOfParticipant(dataModel, t, i);
            }
        }

        private static void SynchronizeTrackLocationToWorldPositionOfParticipant(
            RaceDataModel dataModel, Transform transform, int participantIndex)
        {
            ref var location = ref dataModel.participants.track.points[participantIndex];
            location = dataModel.Track.UpdateRoadPoint(location, transform.position);
        }
    }

    public class RaceProperties : MonoBehaviour
    {
        private RaceDataModel _dataModel;
        public RaceDataModel DataModel => _dataModel;

        public UnityEvent<ParticipantUpdatedEventInfo> OnParticipantUpdated;

        void Awake()
        {
        }

        public void Initialize(RaceDataModel dataModel)
        {
            _dataModel = dataModel;

            {
                assert(dataModel.mapTransform != null);
                assert(dataModel.trackTransform != null);
                assert(dataModel.participants.driver.infos is not null);
                assert(dataModel.participants.track.checkpoints is not null);
                assert(dataModel.participants.track.points is not null);
                assert(dataModel.trackInfo.track is not null);
            }
        }

        public void TriggerParticipantUpdated(int participantIndex, ParticipantUpdateResult result)
        {
            OnParticipantUpdated.Invoke(new ParticipantUpdatedEventInfo
            {
                index = participantIndex,
                result = result,
                raceProperties = this,
            });
        }
    }


    public struct ParticipantUpdatedEventInfo
    {
        public int index;
        public ParticipantUpdateResult result;
        public RaceProperties raceProperties;

        public readonly RaceDataModel DataModel => raceProperties.DataModel;
        public readonly ref DriverInfo Driver => ref DataModel.participants.driver[index];
    }
}